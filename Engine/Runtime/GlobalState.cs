using System.Collections.Concurrent;

namespace PHPIL.Engine.Runtime;

public static class GlobalState
{
    private static readonly ConcurrentDictionary<string, object?> _superglobals = new();

    static GlobalState()
    {
        // Initialize standard superglobals as empty dictionaries (PHP arrays)
        _superglobals["$_GET"] = new Dictionary<object, object>();
        _superglobals["$_POST"] = new Dictionary<object, object>();
        _superglobals["$_COOKIE"] = new Dictionary<object, object>();
        _superglobals["$_SERVER"] = new Dictionary<object, object>();
        _superglobals["$_REQUEST"] = new Dictionary<object, object>();
        _superglobals["$_FILES"] = new Dictionary<object, object>();
        _superglobals["$_ENV"] = new Dictionary<object, object>();
        _superglobals["$_SESSION"] = new Dictionary<object, object>();
    }

    public static object? GetSuperglobal(string name)
    {
        if (_superglobals.TryGetValue(name, out var value))
            return value;
        return null;
    }

    public static void SetSuperglobal(string name, object? value)
    {
        _superglobals[name] = value;
    }

    public static void ClearSuperglobals()
    {
        foreach (var key in _superglobals.Keys)
        {
            _superglobals[key] = new Dictionary<object, object>();
        }
    }

    // Helper methods for HTTP context population
    public static void PopulateGet(Dictionary<object, object> get)
    {
        _superglobals["$_GET"] = get;
    }

    public static void PopulatePost(Dictionary<object, object> post)
    {
        _superglobals["$_POST"] = post;
    }

    public static void PopulateCookie(Dictionary<object, object> cookie)
    {
        _superglobals["$_COOKIE"] = cookie;
    }

    public static void PopulateServer(Dictionary<object, object> server)
    {
        _superglobals["$_SERVER"] = server;
    }

    public static void PopulateRequest(Dictionary<object, object> request)
    {
        _superglobals["$_REQUEST"] = request;
    }

    public static void PopulateFiles(Dictionary<object, object> files)
    {
        _superglobals["$_FILES"] = files;
    }

    public static void PopulateEnv(Dictionary<object, object> env)
    {
        _superglobals["$_ENV"] = env;
    }

    public static void PopulateSession(Dictionary<object, object> session)
    {
        _superglobals["$_SESSION"] = session;
    }
}
