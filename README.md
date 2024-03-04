# Unity-Extended-Dictionary
Unity Package that provides extended Dictionary class with support for polymorphic serialization and editing in the inspector.

> [!NOTE]
> Requires Unity 2021.3 or newer.

![Inspector Dictionary](https://github.com/ikold/Unity-Extended-Dictionary/assets/18567485/b2b26715-4140-4ad5-b3aa-eb1bc73c0611)

### Setup
In Unity Package Manager select `Add Package from git URL...` and add following URL
```sh
https://github.com/ikold/Unity-Extended-Dictionary.git
```

To use serialized dictionary and have it show up for editing in the inspector use class `SDictionary<TKey, TValue>` the same way as you would use C# `Dictionary<TKey, TValue>`. It inherits from it and has access to the same methods.

```C#
using ExtendedDictionary;
using UnityEngine;

public class CustomMonoBehaviour : MonoBehaviour
{
    public SDictionary<object, object> Parameters = new SDictionary<object, object>
    {
        { "row 1", 1 },
        { 2.2, "string" },
        { 3, null }
    };
}
```

Features:
- Polymorphic serialization (can be used with Unity Objects)
- Prefab support
- Inspector:
  - Type selector for keys and values
  - Visual data validation
  - Support of Unity Object references
  - Adding and removing dictionary entries
