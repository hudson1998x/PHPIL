namespace PHPIL.Engine.Runtime.Sdk;

public class Enumerable
{
    private class PhpArrayEnumerator
    {
        private Dictionary<object, object>.Enumerator _enumerator;
        
        public PhpArrayEnumerator(Dictionary<object, object> array)
        {
            _enumerator = array.GetEnumerator();
        }
        
        public bool MoveNext() => _enumerator.MoveNext();
        
        public object Current => _enumerator.Current.Value;
        
        public object Key => _enumerator.Current.Key;
    }
    
    public static object GetEnumerator(object? iterable)
    {
        if (iterable == null) return new PhpArrayEnumerator(new Dictionary<object, object>());
        
        if (iterable is Dictionary<object, object> dict)
        {
            return new PhpArrayEnumerator(dict);
        }
        
        if (iterable is System.Collections.IEnumerable enumerable && iterable is not string)
        {
            return enumerable.GetEnumerator();
        }
        
        var type = iterable.GetType();
        
        // Use RuntimeHelpers to try calling the methods
        try {
            var validResult = RuntimeHelpers.CallMethod(iterable, "valid", Array.Empty<object>());
            var currentResult = RuntimeHelpers.CallMethod(iterable, "current", Array.Empty<object>());
            return new IteratorEnumerator(iterable);
        }
        catch { }
        
        
        throw new Exception($"Cannot iterate over {iterable.GetType().Name}");
    }
    
    private class IteratorEnumerator
    {
        private readonly object _iterator;
        private bool _first = true;
        
        public IteratorEnumerator(object iterator)
        {
            _iterator = iterator;
        }
        
        public bool MoveNext()
        {
            if (_first)
            {
                _first = false;
                return (bool)RuntimeHelpers.CallMethod(_iterator, "valid", Array.Empty<object>())!;
            }
            
            // Try to call next(), but don't fail if it doesn't exist
            try { RuntimeHelpers.CallMethod(_iterator, "next", Array.Empty<object>()); } catch { }
            return (bool)RuntimeHelpers.CallMethod(_iterator, "valid", Array.Empty<object>())!;
        }
        
        public object Current => RuntimeHelpers.CallMethod(_iterator, "current", Array.Empty<object>()) ?? "";
        
        public object Key 
        {
            get 
            {
                try { return RuntimeHelpers.CallMethod(_iterator, "key", Array.Empty<object>()) ?? 0; }
                catch { return 0; }
            }
        }
    }
    
    public static Dictionary<object, object> ToDictionary(object? iterable)
    {
        if (iterable == null) return new Dictionary<object, object>();
        if (iterable is Dictionary<object, object> dict) return dict;
        
        throw new Exception($"Cannot iterate over {iterable.GetType().Name}");
    }
    
    public static bool MoveNext(object? enumerator)
    {
        if (enumerator is PhpArrayEnumerator ae) return ae.MoveNext();
        if (enumerator is IteratorEnumerator ie) return ie.MoveNext();
        if (enumerator is System.Collections.IEnumerator netEnumerator) return netEnumerator.MoveNext();
        throw new Exception("Invalid enumerator");
    }
    
    public static object GetCurrent(object? enumerator)
    {
        if (enumerator is PhpArrayEnumerator ae) return ae.Current;
        if (enumerator is IteratorEnumerator ie) return ie.Current;
        if (enumerator is System.Collections.IEnumerator netEnumerator) return netEnumerator.Current ?? "";
        throw new Exception("Invalid enumerator");
    }
    
    public static object GetKey(object? enumerator)
    {
        if (enumerator is PhpArrayEnumerator ae) return ae.Key;
        if (enumerator is IteratorEnumerator ie) return ie.Key;
        if (enumerator is System.Collections.IEnumerator netEnumerator) 
        {
            // For standard .NET enumerators, we don't have a key
            // Return 0 or throw? PHP foreach with key needs key.
            // For simple foreach ($arr as $val), key is not used.
            // We'll return 0 for now.
            return 0; 
        }
        throw new Exception("Invalid enumerator");
    }
}
