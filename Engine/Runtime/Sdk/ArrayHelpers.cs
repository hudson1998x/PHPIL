using System.Collections.Generic;
using System.Linq;

namespace PHPIL.Engine.Runtime.Sdk;

public static class ArrayHelpers
{
    public static void Append(Dictionary<object, object> dict, object value)
    {
        dict[dict.Count] = value;
    }

    public static Dictionary<object, object> Merge(Dictionary<object, object> target, Dictionary<object, object> source)
    {
        if (source == null) return target;
        
        // Find the highest int key in target to continue numbering
        int nextKey = target.Keys.OfType<int>().DefaultIfEmpty(-1).Max() + 1;
        
        // Iterate over a copy to avoid modification issues if target == source
        foreach (var kvp in source.ToList())
        {
            if (kvp.Key is int intKey && intKey >= 0)
            {
                // Re-index numeric keys to continue from target's sequence
                target[nextKey + intKey] = kvp.Value;
            }
            else
            {
                // Non-numeric keys just get copied
                target[kvp.Key] = kvp.Value;
            }
        }
        return target;
    }
}