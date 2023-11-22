using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExtendedDictionary.Editor.UI;

namespace ExtendedDictionary.Editor
{
    /// <summary>
    /// Adapter for validating and applying edits to the target dictionary made with <see cref="DictionaryPropertyDrawer"/>
    /// </summary>
    /// <remarks>
    /// Stores current state of the property fields as pseudo-dictionary
    /// If it is in invalid state, only the invalid entries are not applied, entries that have warnings will still be applied to the target dictionary
    /// <see cref="ErrorsAndWarnings"/> for errors and warnings that are checked when validating the dictionary
    /// </remarks>
    /// <example>
    /// If there is a duplicate key, the first one encountered will only receive a warning, but will be still applied to the target dictionary
    /// Consecutive duplicate keys will receive an error and will not be added to the target dictionary
    /// </example>
    internal class DictionaryPrototype
    {
        public class KeyValuePair
        {
            public object Key;
            public object Value;

            public Type KeyFieldType;
            public Type ValueFieldType;

            public Issue KeyError;
            public Issue ValueError;
        }
        
        public class Issue
        {
            public enum IssueLevel
            {
                Warning,
                Error
            }

            public string Message;
            public IssueLevel Level;
        }
        

        internal readonly Type KeyGenericType;
        internal readonly Type ValueGenericType;

        private Type _newKeyType;
        private Type _newValueType;

        internal readonly List<KeyValuePair> KeyValuePairs;

        private readonly IDictionary _targetDictionary;


        public DictionaryPrototype(IDictionary dictionary)
        {
            _targetDictionary = dictionary;

            KeyGenericType = _targetDictionary.Keys.GetType().GetGenericArguments()[0];
            ValueGenericType = _targetDictionary.Values.GetType().GetGenericArguments()[1];

            KeyValuePairs = new List<KeyValuePair>(_targetDictionary.Count);
            UpdateFromTarget();

            // Update the target right away to check for missing object warnings
            UpdateTarget();
        }

        public KeyValuePair this[int i] => KeyValuePairs[i];

        public bool UpdateTarget()
        {
            _targetDictionary.Clear();

            foreach (var kvp in KeyValuePairs)
            {
                kvp.KeyError = null;
                kvp.ValueError = null;

                if (kvp.Key.UnityObjectIsMissing())
                    kvp.KeyError = ErrorsAndWarnings.MissingKeyObject;

                if (kvp.Value.UnityObjectIsMissing())
                    kvp.ValueError = ErrorsAndWarnings.MissingValueObject;

                if (kvp.Key == null)
                {
                    kvp.KeyError = ErrorsAndWarnings.NullKey;
                    continue;
                }

                if (_targetDictionary.Contains(kvp.Key))
                {
                    var originalKvp = KeyValuePairs.First(entry => Equals(entry.Key, kvp.Key));

                    originalKvp.KeyError = ErrorsAndWarnings.DuplicatedKeyOriginal;
                    kvp.KeyError = ErrorsAndWarnings.DuplicatedKey;
                    continue;
                }

                _targetDictionary.Add(kvp.Key, kvp.Value);
            }

            // Set types that will be used as defaults when creating new dictionary entry
            if (KeyValuePairs.Count > 0)
            {
                _newKeyType = KeyValuePairs.Last().KeyFieldType;
                _newValueType = KeyValuePairs.Last().ValueFieldType;

                if (_newKeyType == typeof(object))
                    _newKeyType = typeof(UnityEngine.Object);
                if (_newValueType == typeof(object))
                    _newValueType = typeof(UnityEngine.Object);
            }

            return true;
        }

        /// <summary>
        /// Creates a new entry
        /// </summary>
        /// <param name="index">If is non negative, the new entry is created in place (replaces default created kvp)</param>
        public void CreateEntry(int index = -1)
        {
            var entry = new KeyValuePair
            {
                Key = _newKeyType.CreateDefaultValue(),
                KeyFieldType = _newKeyType,
                Value = _newValueType.CreateDefaultValue(),
                ValueFieldType = _newValueType
            };

            if (index >= 0)
                KeyValuePairs[index] = entry;
            else
                KeyValuePairs.Add(entry);
        }

        public void RemoveEntry(int index) => KeyValuePairs.RemoveAt(index);

        /// <summary>
        /// Updates the prototype with the data from the target dictionary
        /// </summary>
        public void UpdateFromTarget()
        {
            var targetKeys = _targetDictionary.Keys.Cast<object>().ToList();

            foreach (var kvp in KeyValuePairs.Where(kvp => targetKeys.Contains(kvp.Key)))
            {
                kvp.Value = _targetDictionary[kvp.Key];
                targetKeys.Remove(kvp.Key);
            }

            foreach (var key in targetKeys)
            {
                var value = _targetDictionary[key];

                KeyValuePairs.Add(new KeyValuePair
                {
                    Key = key,
                    Value = value,
                    KeyFieldType = key!.GetType(),
                    ValueFieldType = value == null ? ValueGenericType : value.GetType()
                });
            }
        }

        private static class ErrorsAndWarnings
        {
            public static readonly Issue MissingKeyObject = new Issue
            {
                Message = "Missing Key Object!",
                Level = Issue.IssueLevel.Warning
            };

            public static readonly Issue MissingValueObject = new Issue
            {
                Message = "Missing Value Object!",
                Level = Issue.IssueLevel.Warning
            };

            public static readonly Issue NullKey = new Issue
            {
                Message = "Key can not be null! Entry will NOT be saved!",
                Level = Issue.IssueLevel.Error
            };

            // The first key that has duplicates can be still applied
            public static readonly Issue DuplicatedKeyOriginal = new Issue
            {
                Message = "Key is duplicated! Entry will be saved!",
                Level = Issue.IssueLevel.Warning
            };

            public static readonly Issue DuplicatedKey = new Issue
            {
                Message = "Key is duplicated! Entry will NOT be saved!",
                Level = Issue.IssueLevel.Error
            };
        }
    }
}