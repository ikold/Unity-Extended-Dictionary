using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ExtendedDictionary.Editor.UI
{
    public class TypeSelector : Button
    {
        private Type _selectedType;

        public Type SelectedType
        {
            get => _selectedType;
            set => SetType(value);
        }

        public Action<Type> TypeChangeCallback { get; set; } = _ => {};
        public Type BaseType { get; set; } = typeof(object);
        public bool HideIncompatibleTypes { get; set; }

        private static readonly Dictionary<Type, TypeDescriptor> TypeDescriptors;

        public struct TypeDescriptor
        {
            public Type Type;
            public string DisplayName;
            public Texture2D Icon;

            public TypeDescriptor(Type type, string displayName, Texture2D icon)
            {
                Type = type;
                DisplayName = displayName;
                Icon = icon;
            }
        }

        static TypeSelector()
        {
            var objectFieldIcon = Resources.Load<Texture2D>("ObjectFieldIcon");
            var textFieldIcon = Resources.Load<Texture2D>("TextFieldIcon");
            var numberFieldIcon = Resources.Load<Texture2D>("NumberFieldIcon");
            var floatingNumberFieldIcon = Resources.Load<Texture2D>("FloatingNumberFieldIcon");
            var checkboxIcon = Resources.Load<Texture2D>("CheckboxIcon");

            TypeDescriptors = new Dictionary<Type, TypeDescriptor>
            {
                { typeof(Object), new TypeDescriptor(typeof(Object), "Unity Object", objectFieldIcon) },
                { typeof(string), new TypeDescriptor(typeof(string), "String", textFieldIcon) },
                { typeof(int), new TypeDescriptor(typeof(int), "Integer", numberFieldIcon) },
                { typeof(long), new TypeDescriptor(typeof(long), "Long Integer", numberFieldIcon) },
                { typeof(float), new TypeDescriptor(typeof(float), "Float", floatingNumberFieldIcon) },
                { typeof(double), new TypeDescriptor(typeof(double), "Double Float", floatingNumberFieldIcon) },
                { typeof(bool), new TypeDescriptor(typeof(bool), "Bool", checkboxIcon) }
            };
        }

        public TypeSelector()
        {
            clicked += () =>
            {
                var menu = new GenericDropdownMenu();

                foreach (var descriptor in TypeDescriptors.Values)
                {
                    var isCompatible = BaseType.IsAssignableFrom(descriptor.Type);

                    // We only want to allow for the types compatible with the underlying base type to be selectable
                    switch (isCompatible)
                    {
                        case false when HideIncompatibleTypes:
                            continue;
                        case true:
                            menu.AddItem(descriptor.DisplayName, SelectedType == descriptor.Type, () => SetType(descriptor.Type));
                            break;
                        default:
                            menu.AddDisabledItem(descriptor.DisplayName, false);
                            break;
                    }
                }

                menu.DropDown(worldBound, this);
            };
        }

        public void AddType(Type type, TypeDescriptor descriptor)
        {
            TypeDescriptors[type] = descriptor;
        }

        public void SetType(Type type)
        {
            if (typeof(Object).IsAssignableFrom(type) || type == typeof(object))
                type = typeof(Object);

            if (_selectedType == type)
                return;

            _selectedType = type;

            style.backgroundImage = new StyleBackground(TypeDescriptors[type].Icon);
        
            TypeChangeCallback(type);
        }

        public new class UxmlFactory : UxmlFactory<TypeSelector, UxmlTraits> {}

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlBoolAttributeDescription _hideIncompatibleTypes = new UxmlBoolAttributeDescription { name = "Hide incompatible types" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var selector = (TypeSelector)ve;
                selector.HideIncompatibleTypes = _hideIncompatibleTypes.GetValueFromBag(bag, cc);
            }
        }
    }
}