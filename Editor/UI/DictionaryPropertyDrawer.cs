using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ExtendedDictionary.Editor.UI
{
    [CustomPropertyDrawer(typeof(SDictionary<,>), true)]
    public class DictionaryPropertyDrawer : PropertyDrawer
    {
        private VisualElement _rootVisualElement;
        private VisualTreeAsset _listEntryTemplate;

        private DictionaryPrototype _dictionaryPrototype;
        private DictionaryFieldAttribute _fieldAttribute;
        private SerializedProperty _property;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            _rootVisualElement = new VisualElement();
            _property = property;

            var visualTree = Resources.Load<VisualTreeAsset>("DictionaryPropertyDrawerLayout");
            _listEntryTemplate = Resources.Load<VisualTreeAsset>("KeyValuePairListEntry");

            VisualElement labelFromUxml = visualTree.Instantiate();
            _rootVisualElement.Add(labelFromUxml);

            BindData(property);

            return _rootVisualElement;
        }

        private void BindData(SerializedProperty property)
        {
            if (_dictionaryPrototype == null)
                Setup(property);

            var list = _rootVisualElement.Q<ListView>("dictionary");

            list.headerTitle = property.displayName;

            list.itemsSource = _dictionaryPrototype.KeyValuePairs;

            list.reorderable = !_fieldAttribute.DrawKeysAsLabels;
            list.showAddRemoveFooter = !_fieldAttribute.DrawKeysAsLabels;

            list.makeItem += () => _listEntryTemplate.Instantiate();

            list.bindItem += (element, i) =>
            {
                var kvp = _dictionaryPrototype[i];


                var keyTypeSelector = element.Q<TypeSelector>("key-type-selector");
                keyTypeSelector.SelectedType = kvp.KeyFieldType;
                keyTypeSelector.BaseType = _dictionaryPrototype.KeyGenericType;

                keyTypeSelector.TypeChangeCallback = type =>
                {
                    kvp.KeyFieldType = type;
                    kvp.Key = type.SetOrDefaultValue(kvp.Value);
                    ApplyEdit();
                };


                var keyField = element.Q<GenericField>("key-field");

                keyField.Bind(keyTypeSelector.SelectedType, kvp.Key, value =>
                {
                    kvp.Key = value;
                    ApplyEdit();
                });

                // TODO Create a better way to draw key labels
                keyTypeSelector.SetEnabled(!_fieldAttribute.DrawKeysAsLabels);
                keyField.SetEnabled(!_fieldAttribute.DrawKeysAsLabels);

                SetFieldError(keyField, kvp.KeyError);


                var valueTypeSelector = element.Q<TypeSelector>("value-type-selector");
                valueTypeSelector.SelectedType = kvp.ValueFieldType;
                valueTypeSelector.BaseType = _dictionaryPrototype.ValueGenericType;

                valueTypeSelector.TypeChangeCallback = type =>
                {
                    kvp.ValueFieldType = type;
                    kvp.Value = type.SetOrDefaultValue(kvp.Value);
                    ApplyEdit();
                };


                var valueField = element.Q<GenericField>("value-field");

                valueField.Bind(valueTypeSelector.SelectedType, kvp.Value, value =>
                {
                    kvp.Value = value;
                    ApplyEdit();
                });

                SetFieldError(valueField, kvp.ValueError);
            };

            list.itemsAdded += indices =>
            {
                // ListView uses default constructor when adding new items, so we want to replace it with properly created entry
                foreach (var index in indices)
                    _dictionaryPrototype.CreateEntry(index);

                ApplyEdit();
            };

            list.itemsRemoved += indices =>
            {
                // We have to delay call to apply edit because itemsRemoved is called before ListView removes items from the dictionary
                // We can not remove items ourselves, as ListView would still attempt to remove them after this callback returns
                EditorApplication.delayCall += ApplyEdit;
            };

            list.itemIndexChanged += (_, _) => { ApplyEdit(); };

            // Create a hidden field bound to SerializationBundle data to monitor for changes to the target dictionary
            var dataProperty = property.FindPropertyRelative("bundle.base64Data");
            var stringField = new PropertyField(dataProperty);
            stringField.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            _rootVisualElement.Add(stringField);

            stringField.RegisterValueChangeCallback(value =>
            {
                _dictionaryPrototype.UpdateFromTarget();
                list.itemsSource = _dictionaryPrototype.KeyValuePairs;
                list.RefreshItems();
            });
        }

        private void ClearFieldError(VisualElement field)
        {
            field.RemoveFromClassList("warning");
            field.RemoveFromClassList("error");
            field.tooltip = "";
        }

        private void SetFieldError(VisualElement field, DictionaryPrototype.Issue error)
        {
            ClearFieldError(field);

            if (error == null)
                return;

            switch (error.Level)
            {
                case DictionaryPrototype.Issue.IssueLevel.Warning:
                    field.AddToClassList("warning");
                    break;
                case DictionaryPrototype.Issue.IssueLevel.Error:
                    field.AddToClassList("error");
                    break;
            }

            field.tooltip = error.Message;
        }

        private void ApplyEdit()
        {
            _dictionaryPrototype.UpdateTarget();
            EditorUtility.SetDirty(_property.serializedObject.targetObject);
            _rootVisualElement.Q<ListView>("dictionary").RefreshItems();
        }

        private void Setup(SerializedProperty property)
        {
            var targetObject = property.serializedObject.targetObject;

            var dictionaryFieldInfo = targetObject.GetType().GetField(property.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            _fieldAttribute = dictionaryFieldInfo!.GetCustomAttribute<DictionaryFieldAttribute>() ?? new DictionaryFieldAttribute();

            var dictionary = dictionaryFieldInfo!.GetValue(targetObject) as IDictionary;

            _dictionaryPrototype = new DictionaryPrototype(dictionary);
        }
    }
}