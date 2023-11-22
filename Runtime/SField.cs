using System;
using UnityEngine;

namespace ExtendedDictionary
{
    /// <summary>
    /// Wrapper class that enables polymorphic serialization of types that do not support it
    /// </summary>
    /// <typeparam name="T">Type of the underlying object to serialize</typeparam>
    [Serializable]
    public class SField<T>
    {
        // Handles serialization
        [SerializeField]
        private SerializationBundle bundle;

        /// <summary>
        /// Underlying object reference
        /// </summary>
        public T reference;

        public SField() => bundle = new SerializationBundle(this);

        // Implicit converter to and from the underlying reference
        public static implicit operator T(SField<T> field) => field.reference;
        public static implicit operator SField<T>(T reference) => new SField<T> { reference = reference };

        
        public override string ToString() => reference.ToString();
    }
}