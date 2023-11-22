using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ExtendedDictionary
{
    public static class ReflectionHelper
    {
        /// <summary>
        /// Gets types that can be assigned to the base type 
        /// </summary>
        /// <param name="baseType"></param>
        /// <returns>Enumerable with all types assignable to the base type</returns>
        public static IEnumerable<Type> GetAssignableTypes(this Type baseType)
        {
            // Get all type across all domains
            var allTypes =
                from assembly in AppDomain.CurrentDomain.GetAssemblies()
                from type in assembly.GetTypes()
                select type;

            var derivedTypes = from type in allTypes
                where type.IsSubclassOf(baseType)
                select type;

            // Add base type, as it is assignable to itself
            derivedTypes = derivedTypes.Prepend(baseType);

            return derivedTypes;
        }

        /// <summary>
        /// Creates default value for runtime type that is not know at compile time
        /// </summary>
        /// <param name="type">Type for which to create default value</param>
        /// <returns>Default value of a type</returns>
        /// <remarks>In case of classes return value will always be null</remarks>
        public static object CreateDefaultValue(this Type type)
        {
            return type.IsValueType | type.GetConstructor(new Type[]{}) != null
                ? Activator.CreateInstance(type)
                : type == typeof(string) ? "" : null;
        }

        /// <summary>
        /// Checks if value can be assigned to type that is not know at compile time and if it is not creates default value for it
        /// </summary>
        /// <param name="type">Target type</param>
        /// <param name="value">Value that we try to assign to type</param>
        /// <returns>Unchanged value if it can be assigned, otherwise default value of the type</returns>
        /// TODO Better handling of primitive types (e.g. int value would not be assigned to long)
        public static object SetOrDefaultValue(this Type type, object value)
        {
            return type.IsInstanceOfType(value) ? value : type.CreateDefaultValue();
        }
        
        

        /// <summary>
        /// Checks if the object is a Unity Object that is missing
        /// </summary>
        /// <param name="obj">Object to be checked</param>
        /// <remarks>Always returns false for non-unity objects</remarks>
        /// <example>Reference to a deleted instance of ScriptableObject would return true</example>
        public static bool UnityObjectIsMissing(this object obj)
        {
            // Unity object is missing when its overrode Equals method says it is null and instance id is non-zero
            if (obj is UnityEngine.Object unityObject)
                return obj.Equals(null) && unityObject.GetInstanceID() != 0;

            return false;
        }

        /// <summary>
        /// Swaps all fields between objects while preserving their original references
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>Types with the attributes</returns>
        /// <exception cref="ArgumentException">Thrown when left and right objects are of different types</exception>
        public static void Swap(object left, object right)
        {
            var type = left.GetType();

            if (right.GetType() != type)
                throw new ArgumentException("Arguments are of different types!");

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
                field.Swap(left, right);

            // Go over base types and swap private fields
            while ((type = type.BaseType) != null)
            {
                fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

                // Filter out protected fields that were already iterated over as NonPublic fields in the base type
                fields = fields.Where(field => field.IsPrivate).ToArray();

                foreach (var field in fields)
                    field.Swap(left, right);
            }
        }

        /// <summary>
        /// Swaps specific fields between objects
        /// </summary>
        /// <param name="field"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>Types with the attributes</returns>
        public static void Swap(this FieldInfo field, object left, object right)
        {
            var leftValue = field.GetValue(left);
            var rightValue = field.GetValue(right);

            field.SetValue(right, leftValue);
            field.SetValue(left, rightValue);
        }
    }
}