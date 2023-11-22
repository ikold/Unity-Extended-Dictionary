using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExtendedDictionary;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ExtendedDictionary.Tests
{
    public class SerializationFunctionality
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return null;
        }

        [UnityTest]
        public IEnumerator DictionarySerialization()
        {
            var baseDictionary = new Dictionary<string, int>
            {
                { "key 1", 1 },
                { "key 2", 2 },
                { "key 3", 3 },
            };

            var dictionary = new SDictionary<string, int>(baseDictionary);

            dictionary.AssertDictionariesEqual(baseDictionary);


            var bundle = dictionary.GetSerializationBundle();
            bundle.ForceSerialization();

            dictionary.Clear();
            bundle.ForceDeserialization();

            dictionary.AssertDictionariesEqual(baseDictionary);

            yield return null;
        }

        [UnityTest]
        public IEnumerator UnityReferenceSerialization()
        {
            var scriptableObject1 = ScriptableObject.CreateInstance<UnityObject>();
            var scriptableObject2 = ScriptableObject.CreateInstance<UnityObject>();
            var scriptableObject3 = ScriptableObject.CreateInstance<UnityObject>();

            var baseDictionary = new Dictionary<string, UnityObject>
            {
                { "key 1", scriptableObject1 },
                { "key 2", scriptableObject2 },
                { "key 3", scriptableObject3 },
            };

            var dictionary = new SDictionary<string, UnityObject>(baseDictionary);

            dictionary.AssertDictionariesEqual(baseDictionary);


            var bundle = dictionary.GetSerializationBundle();
            bundle.ForceSerialization();

            dictionary.Clear();
            bundle.ForceDeserialization();

            dictionary.AssertDictionariesEqual(baseDictionary);

            bundle.ForceSerialization();

            dictionary.Clear();
            Object.DestroyImmediate(scriptableObject1);
            Object.DestroyImmediate(scriptableObject2);

            yield return null;

            bundle.ForceDeserialization();

            dictionary.AssertDictionariesEqual(baseDictionary);

            yield return null;
        }

        [UnityTest]
        public IEnumerator UnityReferenceKeySerialization()
        {
            var scriptableObject1 = ScriptableObject.CreateInstance<UnityObject>();
            var scriptableObject2 = ScriptableObject.CreateInstance<UnityObject>();
            var scriptableObject3 = ScriptableObject.CreateInstance<UnityObject>();

            var baseDictionary = new Dictionary<UnityObject, string>
            {
                { scriptableObject1, "value 1" },
                { scriptableObject2, "value 2" },
                { scriptableObject3, "value 3" },
            };

            var dictionary = new SDictionary<UnityObject, string>(baseDictionary);

            dictionary.AssertDictionariesEqual(baseDictionary);


            var bundle = dictionary.GetSerializationBundle();
            bundle.ForceSerialization();

            dictionary.Clear();
            Object.DestroyImmediate(scriptableObject1);
            Object.DestroyImmediate(scriptableObject2);

            yield return null;

            bundle.ForceDeserialization();

            dictionary.AssertDictionariesEqual(baseDictionary);

            yield return null;
        }
    }

    internal class UnityObject : ScriptableObject, IComparable
    {
        public int CompareTo(object obj)
        {
            return GetInstanceID().CompareTo(((UnityObject)obj).GetInstanceID());
        }
    }


    internal static class SerializationTestUtility
    {
        internal static SerializationBundle GetSerializationBundle(this object obj)
        {
            var fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var bundleField = fields.First(field => field.FieldType == typeof(SerializationBundle));
            return bundleField.GetValue(obj) as SerializationBundle;
        }

        private static readonly MethodInfo SerializationMethod = typeof(SerializationBundle).GetMethod("Serialize", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo DeserializationMethod = typeof(SerializationBundle).GetMethod("Deserialize", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static void ForceSerialization(this SerializationBundle bundle)
        {
            SerializationMethod.Invoke(bundle, null);
        }

        internal static void ForceDeserialization(this SerializationBundle bundle)
        {
            DeserializationMethod.Invoke(bundle, null);
        }

        internal static void AssertDictionariesEqual<TKey, TValue>(this Dictionary<TKey, TValue> left, Dictionary<TKey, TValue> right)
        {
            Assert.IsTrue(left.OrderBy(kvp => kvp.Key)
                .SequenceEqual(right.OrderBy(kvp => kvp.Key)));
        }
    }
}