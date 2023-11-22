using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ExtendedDictionary
{
    /// <summary>
    /// Allows serialization of mixed general types, polymorphism and references to Unity objects
    /// </summary>
    /// <seealso cref="SDictionary{TKey,TValue}"/>
    /// <seealso cref="SField{T}"/>
    [Serializable]
    public class SerializationBundle : ISerializationCallbackReceiver
    {
        /// <summary>
        /// Creates automatically managed SerializationBundle
        /// </summary>
        /// <param name="target">Object to be serialized</param>
        public SerializationBundle(object target) => _target = target;

        /// <summary>
        /// Object to be serialized
        /// </summary>
        private readonly object _target;

        /// <summary>
        /// Store references to Unity Objects that will be serialized by Unity serialization
        /// </summary>
        [SerializeField]
        private List<Object> unityObjects = new List<Object>();

        /// <summary>
        /// Serialized data saved as a base 64 string for Unity to serialized
        /// </summary>
        /// <remarks>
        /// Data could not be stored as byte[], due to unit applying changes from prefabs per index basis
        /// </remarks>
        [SerializeField]
        private string base64Data;

        /// <summary>
        /// Formatter that is responsible for (de)serialization of the object
        /// </summary>
        /// <remarks>
        /// Is Thread Static in case multiple objects will be serialized at the same time, in which case we want to preserve serialization context that contains reference to specific SerializationBundle
        /// </remarks>
        [ThreadStatic]
        private static BinaryFormatter _formatter;

        /// <summary>
        /// Property for creating a formatter singleton per thread basis
        /// </summary>
        /// <remarks>
        /// As Thread Static fields are only initialized once, this is required for it to work on each thread
        /// </remarks>
        private static BinaryFormatter Formatter => _formatter ??= new BinaryFormatter(new BundleSurrogateSelector(), new StreamingContext());

        /// <summary>
        /// Serializes target object into <see cref="base64Data"/> and <see cref="unityObjects"/>
        /// </summary>
        private void Serialize()
        {
            if (_target == null)
                return;
            
            // Clear old Unity references before serialization
            unityObjects.Clear();

            using var stream = new MemoryStream();

            // Set reference to this SerializationBundle as context of the serialization
            Formatter.Context = new StreamingContext(StreamingContextStates.All, this);

            // Serialize target into stream
            Formatter.Serialize(stream, _target);

            // Save data as base 64 string for Unity to serialize
            base64Data = Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length);
        }

        /// <summary>
        /// Deserializes data and inserts it into the <see cref="_target"/>
        /// </summary>
        private void Deserialize()
        {
            if (string.IsNullOrEmpty(base64Data))
                return;
            
            using var stream = new MemoryStream(Convert.FromBase64String(base64Data));

            // Set reference to this SerializationBundle as context of the serialization
            Formatter.Context = new StreamingContext(StreamingContextStates.All, this);

            // Returns deserialized object with Unity objects in place
            var deserializedObject = Formatter.Deserialize(stream);

            ReflectionHelper.Swap(_target, deserializedObject);
        }

        /// <summary>
        /// Adds reference to Unity Object to <see cref="unityObjects"/> list that will be serialized separately and returns its index in it
        /// </summary>
        /// <param name="obj">Unity Object</param>
        /// <returns>Index to Unity Object in <see cref="unityObjects"/> list</returns>
        /// <remarks>
        /// If object is already in the list it will just return its index
        /// </remarks>
        private int AddUnityReference(Object obj)
        {
            // Check if object is in the list and return its index if it is
            var index = unityObjects.IndexOf(obj);

            if (index >= 0)
                return index;

            // Add object to the list and return its index
            index = unityObjects.Count;
            unityObjects.Add(obj);

            return index;
        }

        /// <summary>
        /// Returns object from <see cref="unityObjects"/> at the index
        /// </summary>
        /// <param name="index"></param>
        private Object GetUnityReference(int index)
        {
            return unityObjects[index];
        }

        // Callbacks that are called by Unity serialization
        void ISerializationCallbackReceiver.OnBeforeSerialize() => Serialize();
        void ISerializationCallbackReceiver.OnAfterDeserialize() => Deserialize();

        /// <summary>
        /// Custom surrogate selector used by <see cref="SerializationBundle.Formatter"/>
        /// </summary>
        private class BundleSurrogateSelector : ISurrogateSelector
        {
            /// <summary>
            /// All Unity types that serialization will save as reference
            /// </summary>
            private static readonly HashSet<Type> UnityTypes = new HashSet<Type>(typeof(Object).GetAssignableTypes());

            public ISerializationSurrogate GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector selector)
            {
                selector = this;

                if (type == typeof(SerializationBundle))
                    return new BundleSerializationSurrogate();

                if (UnityTypes.Contains(type))
                    return new UnityReferenceSerializationSurrogate();

                // Use default serialization if it is not SerializationBundle or Unity Object
                return null;
            }

            /// <summary>
            /// Surrogate that handles replacing Unity Objects with temporal IDs in the serialized data, and puts them back during deserialization
            /// </summary>
            private class UnityReferenceSerializationSurrogate : ISerializationSurrogate
            {
                public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
                {
                    // Retrieve SerializationBundle for the context, as it is used for managing Unity references
                    var bundle = (SerializationBundle)context.Context;

                    // Save Unity Object in the bundle
                    var index = bundle.AddUnityReference(obj as Object);

                    // Save index to the object in the serialization info
                    // URI stands for "Unity Reference Index"
                    info.AddValue("URI", index);
                }

                public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
                {
                    // Retrieve SerializationBundle for the context, as it is used for managing Unity references
                    var bundle = (SerializationBundle)context.Context;

                    // Retrieve Unity Object by its index in unityObjects
                    var index = info.GetInt32("URI");

                    return bundle.GetUnityReference(index);
                }
            }

            /// <summary>
            /// Surrogate for <see cref="SerializationBundle"/> to prevent it from being serialized
            /// </summary>
            private class BundleSerializationSurrogate : ISerializationSurrogate
            {
                public void GetObjectData(object obj, SerializationInfo info, StreamingContext context) {}
                public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector) => context.Context;
            }

            // Not used in the implementation
            void ISurrogateSelector.ChainSelector(ISurrogateSelector selector) => throw new NotImplementedException();
            ISurrogateSelector ISurrogateSelector.GetNextSelector() => throw new NotImplementedException();
        }
    }
}