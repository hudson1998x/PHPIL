using System.Collections.Concurrent;

namespace PHPIL.Engine.Runtime;

/// <summary>
/// Legacy global state accessor, now delegates to per-ExecutionContext superglobals.
/// Kept for backward compatibility; prefer using ExecutionContext directly in new code.
/// </summary>
public static class GlobalState
{
    private static readonly ConcurrentDictionary<string, object?> _superglobals = new();

    static GlobalState()
    {
        // Initialize standard superglobals as empty dictionaries (PHP arrays)
        // These are used as fallback when no ExecutionContext is active
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
        var context = Runtime.CurrentContext;
        if (context != null)
            return context.GetSuperglobal(name);

        // Fallback to legacy behavior
        if (_superglobals.TryGetValue(name, out var value))
            return value;
        return null;
    }

    public static void SetSuperglobal(string name, object? value)
    {
        var context = Runtime.CurrentContext;
        if (context != null)
        {
            context.SetSuperglobal(name, value);
            return;
        }

        // Fallback to legacy behavior
        _superglobals[name] = value;
    }

    public static void ClearSuperglobals()
    {
        var context = Runtime.CurrentContext;
        if (context != null)
        {
            // Clear all superglobals in the current context
            context.SetSuperglobal("$_GET", new Dictionary<object, object>());
            context.SetSuperglobal("$_POST", new Dictionary<object, object>());
            context.SetSuperglobal("$_COOKIE", new Dictionary<object, object>());
            context.SetSuperglobal("$_SERVER", new Dictionary<object, object>());
            context.SetSuperglobal("$_REQUEST", new Dictionary<object, object>());
            context.SetSuperglobal("$_FILES", new Dictionary<object, object>());
            context.SetSuperglobal("$_ENV", new Dictionary<object, object>());
            context.SetSuperglobal("$_SESSION", new Dictionary<object, object>());
            return;
        }

        // Fallback to legacy behavior
        foreach (var key in _superglobals.Keys)
        {
            _superglobals[key] = new Dictionary<object, object>();
        }
    }

    // Helper methods for HTTP context population
    public static void PopulateGet(Dictionary<object, object> get)
    {
        var context = Runtime.CurrentContext;
        if (context != null)
        {
            context.PopulateGet(get);
            return;
        }
        _superglobals["$_GET"] = get;
    }

    public static void PopulatePost(Dictionary<object, object> post)
    {
        var context = Runtime.CurrentContext;
        if (context != null)
        {
            context.PopulatePost(post);
            return;
        }
        _superglobals["$_POST"] = post;
    }

    public static void PopulateCookie(Dictionary<object, object> cookie)
    {
        var context = Runtime.CurrentContext;
        if (context != null)
        {
            context.PopulateCookie(cookie);
            return;
        }
        _superglobals["$_COOKIE"] = cookie;
    }

    public static void PopulateServer(Dictionary<object, object> server)
    {
        var context = Runtime.CurrentContext;
        if (context != null)
        {
            context.PopulateServer(server);
            return;
        }
        _superglobals["$_SERVER"] = server;
    }

    public static void PopulateRequest(Dictionary<object, object> request)
    {
        var context = Runtime.CurrentContext;
        if (context != null)
        {
            context.PopulateRequest(request);
            return;
        }
        _superglobals["$_REQUEST"] = request;
    }

    public static void PopulateFiles(Dictionary<object, object> files)
    {
        var context = Runtime.CurrentContext;
        if (context != null)
        {
            context.PopulateFiles(files);
            return;
        }
        _superglobals["$_FILES"] = files;
    }

    public static void PopulateEnv(Dictionary<object, object> env)
    {
        var context = Runtime.CurrentContext;
        if (context != null)
        {
            context.PopulateEnv(env);
            return;
        }
        _superglobals["$_ENV"] = env;
    }

    public static void PopulateSession(Dictionary<object, object> session)
    {
        var context = Runtime.CurrentContext;
        if (context != null)
        {
            context.PopulateSession(session);
            return;
        }
        _superglobals["$_SESSION"] = session;
    }
}
