using System.Collections.Generic;
using System.Linq;

namespace PHPIL.Engine.Runtime.Sdk;

public static class ArrayHelpers
{
    public static void Append(Dictionary<object, object> dict, object value)
    {
        int nextKey = 0;
        
        var intKeys = dict.Keys.OfType<int>().ToList();
        if (intKeys.Any())
        {
            nextKey = intKeys.Max() + 1;
        }

        dict[nextKey] = value;
    }
}
