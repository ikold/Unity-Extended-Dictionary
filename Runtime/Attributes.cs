using System;

namespace ExtendedDictionary
{
    public class DictionaryFieldAttribute : Attribute
    {
        /// <summary>
        /// Draws key value pairs in similar way as if keys were a fields in a object
        /// </summary>
        /// <remarks>
        /// Only values of the existing keys will be editable and adding or removing entries will not be available in the inspector
        /// </remarks>
        public bool DrawKeysAsLabels { get; set; }
    }
}