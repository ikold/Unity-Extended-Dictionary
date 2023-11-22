using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ExtendedDictionary.Editor.UI
{
    /// <summary>
    /// Field that manages underlying field based on the bound type that can be rebound to a different type at runtime
    /// </summary>
    public class GenericField : VisualElement
    {
        private static readonly MethodInfo BindFieldMethodInfo;
        private static readonly Dictionary<Type, Func<BindableElement>> FieldsLookup;
        private Type _bindType;
        private object _bindValue;
        private Action<object> _changeCallback = _ => {};
        private MulticastDelegate _changeEvent;
        private BindableElement _underlyingField;

        static GenericField()
        {
            BindFieldMethodInfo = typeof(GenericField).GetMethod(nameof(BindField), BindingFlags.Instance | BindingFlags.NonPublic);

            FieldsLookup = new Dictionary<Type, Func<BindableElement>>
            {
                [typeof(Object)] = () => new ObjectField(),
                [typeof(string)] = () => new TextField(),
                [typeof(int)] = () => new IntegerField(),
                [typeof(long)] = () => new LongField(),
                [typeof(float)] = () => new FloatField(),
                [typeof(double)] = () => new DoubleField(),
                [typeof(bool)] = () => new Toggle(),
            };
        }

        public static void AddFieldType(Type type, Func<BindableElement> fieldCreator)
        {
            FieldsLookup[type] = fieldCreator;
        }

        public GenericField()
        {
            Bind(typeof(string), "");
        }

        public GenericField(Type type, object value, Action<object> setCallback)
        {
            Bind(type, value, setCallback);
        }

        public void Bind(Type type, object value, Action<object> changeCallback)
        {
            _changeCallback = changeCallback;
            Bind(type, value);
        }

        /// <summary>
        /// Binds new value while keeping the old change callback
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public void Bind(Type type, object value)
        {
            // Treat all Unity objects as base Unity Object
            if (typeof(Object).IsAssignableFrom(type))
                type = typeof(Object);

            // Even if the underlying field stays the same we add it back later
            if (_underlyingField != null)
                Remove(_underlyingField);

            if (type != _bindType)
                _underlyingField = CreateField(type);
        
            _bindType = type;
            _bindValue = value;

            BindFieldMethodInfo
                .MakeGenericMethod(_bindType)
                .Invoke(this, new[] { _bindValue, _changeCallback });

            Add(_underlyingField);
        }

        private static BindableElement CreateField(Type type)
        {
            return FieldsLookup.TryGetValue(type, out var fieldCreator) ? fieldCreator() : new Label();
        }

        private void BindField<T>(T value, Action<object> setCallback)
        {
            switch (_underlyingField)
            {
                case BaseField<T> baseField:
                    baseField.SetValueWithoutNotify(value);
                    break;
                case Label label:
                    label.text = value?.ToString();
                    break;
            }

            _underlyingField.UnregisterCallback(_changeEvent as EventCallback<ChangeEvent<T>>);

            _changeEvent = new EventCallback<ChangeEvent<T>>(change => setCallback(change.newValue));

            _underlyingField.RegisterCallback(_changeEvent as EventCallback<ChangeEvent<T>>);
        }

        public new class UxmlFactory : UxmlFactory<GenericField> {}
    }
}