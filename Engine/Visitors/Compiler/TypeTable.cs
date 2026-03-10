using PHPIL.Engine.Visitors.SemanticAnalysis;

namespace PHPIL.Engine.Visitors;

public static class TypeTable
{
    public static readonly Dictionary<string, Type> _types = [];

    static TypeTable()
    {
        // register core types. 
        _types["void"]   = typeof(void);
        _types["bool"]   = typeof(bool);
        _types["int"]    = typeof(int);
        _types["float"]  = typeof(double);
        _types["object"] = typeof(object);
        _types["mixed"]  = typeof(object);
        _types["array"]  = typeof(Dictionary<string, object>);
        _types["string"] = typeof(string);
    }

    public static Type GetPrimitive(AnalysedType type)
    {
        return _types[type.ToString().ToLower()];
    }
}