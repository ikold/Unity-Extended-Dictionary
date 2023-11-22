using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace ExtendedDictionary
{
    /// <summary>
    /// Dictionary with support for serialization, including polymorphism and Unity references
    /// </summary>
    /// <typeparam name="TKey">Key type</typeparam>
    /// <typeparam name="TValue">Value type</typeparam>
    /// <remarks>Wrapper class for base <see cref="Dictionary{TKey,TValue}"/> that extends it to support serialization</remarks>
    [Serializable]
    public class SDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        // Handles serialization
        [SerializeField]
        private SerializationBundle bundle;

        // All constructors available in the base dictionary
        // Serialization bundle needs reference to the dictionary
        public SDictionary() => bundle = new SerializationBundle(this);

        public SDictionary(int capacity)
            : base(capacity) => bundle = new SerializationBundle(this);

        public SDictionary(IEqualityComparer<TKey> comparer)
            : base(comparer) => bundle = new SerializationBundle(this);

        public SDictionary(int capacity, IEqualityComparer<TKey> comparer)
            : base(capacity, comparer) => bundle = new SerializationBundle(this);

        public SDictionary(IDictionary<TKey, TValue> dictionary)
            : base(dictionary) => bundle = new SerializationBundle(this);

        public SDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
            : base(dictionary, comparer) => bundle = new SerializationBundle(this);

        // Passes deserialization data to the base dictionary and sets serialization bundle from context
        protected SDictionary(SerializationInfo info, StreamingContext context) : base(info, context) => bundle = context.Context as SerializationBundle;
    }
}