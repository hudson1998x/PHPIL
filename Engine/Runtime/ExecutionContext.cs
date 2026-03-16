using System.Collections.Concurrent;
using System.Text;

namespace PHPIL.Engine.Runtime;

/// <summary>
/// Encapsulates all request-scoped execution state for a single PHP script execution.
/// Thread-safe for use with AsyncLocal&lt;ExecutionContext&gt; in high-concurrency scenarios (Kestrel).
/// </summary>
public class ExecutionContext
{
    /// <summary>
    /// Per-execution superglobals: $_GET, $_POST, $_COOKIE, $_SERVER, $_REQUEST, $_FILES, $_ENV, $_SESSION.
    /// Each request gets isolated copies.
    /// </summary>
    private readonly Dictionary<string, object?> _superglobals = new();

    /// <summary>
    /// Per-execution output buffer. All print/echo/var_dump output goes here.
    /// </summary>
    private readonly MemoryStream _outputMemory;
    private readonly StreamWriter _outputStream;

    /// <summary>
    /// Tracks which files have been require_once'd in this execution context.
    /// Maps normalized file path → true.
    /// </summary>
    private readonly HashSet<string> _requiredFiles = new();

    /// <summary>
    /// Creates a new execution context with isolated state.
    /// </summary>
    public ExecutionContext()
    {
        // Initialize superglobals as empty dictionaries
        _superglobals["$_GET"] = new Dictionary<object, object>();
        _superglobals["$_POST"] = new Dictionary<object, object>();
        _superglobals["$_COOKIE"] = new Dictionary<object, object>();
        _superglobals["$_SERVER"] = new Dictionary<object, object>();
        _superglobals["$_REQUEST"] = new Dictionary<object, object>();
        _superglobals["$_FILES"] = new Dictionary<object, object>();
        _superglobals["$_ENV"] = new Dictionary<object, object>();
        _superglobals["$_SESSION"] = new Dictionary<object, object>();

        // Initialize output buffer
        _outputMemory = new MemoryStream();
        _outputStream = new StreamWriter(_outputMemory, Encoding.UTF8, bufferSize: 4096, leaveOpen: true);
    }

    /// <summary>
    /// Gets a superglobal by name (e.g., "$_GET", "$_POST").
    /// </summary>
    public object? GetSuperglobal(string name)
    {
        if (_superglobals.TryGetValue(name, out var value))
            return value;
        return null;
    }

    /// <summary>
    /// Sets or replaces a superglobal by name.
    /// </summary>
    public void SetSuperglobal(string name, object? value)
    {
        _superglobals[name] = value;
    }

    /// <summary>
    /// Populates $_GET from query parameters.
    /// </summary>
    public void PopulateGet(Dictionary<object, object> get)
    {
        _superglobals["$_GET"] = get;
    }

    /// <summary>
    /// Populates $_POST from request body.
    /// </summary>
    public void PopulatePost(Dictionary<object, object> post)
    {
        _superglobals["$_POST"] = post;
    }

    /// <summary>
    /// Populates $_COOKIE from Cookie header.
    /// </summary>
    public void PopulateCookie(Dictionary<object, object> cookie)
    {
        _superglobals["$_COOKIE"] = cookie;
    }

    /// <summary>
    /// Populates $_SERVER from HTTP request metadata.
    /// </summary>
    public void PopulateServer(Dictionary<object, object> server)
    {
        _superglobals["$_SERVER"] = server;
    }

    /// <summary>
    /// Populates $_REQUEST from combined GET/POST/COOKIE.
    /// </summary>
    public void PopulateRequest(Dictionary<object, object> request)
    {
        _superglobals["$_REQUEST"] = request;
    }

    /// <summary>
    /// Populates $_FILES from uploaded files.
    /// </summary>
    public void PopulateFiles(Dictionary<object, object> files)
    {
        _superglobals["$_FILES"] = files;
    }

    /// <summary>
    /// Populates $_ENV from environment variables.
    /// </summary>
    public void PopulateEnv(Dictionary<object, object> env)
    {
        _superglobals["$_ENV"] = env;
    }

    /// <summary>
    /// Populates $_SESSION from session data.
    /// </summary>
    public void PopulateSession(Dictionary<object, object> session)
    {
        _superglobals["$_SESSION"] = session;
    }

    /// <summary>
    /// Gets the output stream for this execution context.
    /// Used by print(), var_dump(), etc.
    /// </summary>
    public StreamWriter OutputStream => _outputStream;

    /// <summary>
    /// Gets the accumulated output as a string and clears the buffer.
    /// </summary>
    public string GetAndClearOutput()
    {
        _outputStream.Flush();
        
        _outputMemory.Position = 0;
        using var reader = new StreamReader(_outputMemory, Encoding.UTF8, false, leaveOpen: true);
        var result = reader.ReadToEnd();

        _outputMemory.Position = 0;
        _outputMemory.SetLength(0);

        return result;
    }

    /// <summary>
    /// Marks a file as required once in this execution context.
    /// Returns true if this is the first time the file has been required in this context.
    /// </summary>
    public bool MarkFileRequired(string filePath)
    {
        var normalizedPath = Path.GetFullPath(filePath);
        return _requiredFiles.Add(normalizedPath);
    }

    /// <summary>
    /// Checks if a file has been required once in this execution context.
    /// </summary>
    public bool IsFileRequired(string filePath)
    {
        var normalizedPath = Path.GetFullPath(filePath);
        return _requiredFiles.Contains(normalizedPath);
    }

    /// <summary>
    /// Disposes resources held by this execution context.
    /// Call this after request handling is complete.
    /// </summary>
    public void Dispose()
    {
        _outputStream?.Dispose();
        _outputMemory?.Dispose();
    }
}
