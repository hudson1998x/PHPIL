using System.Text;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Exceptions;
using PHPIL.Engine.Productions;
using PHPIL.Engine.Runtime.Sdk;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;
using PHPIL.Engine.Visitors.SemanticAnalysis;

namespace PHPIL.Engine.Runtime;

public static class Runtime
{
    /// <summary>
    /// Global list of autoloaders, registered once and available to all executions.
    /// Guarded by lock due to List<T> not being thread-safe.
    /// </summary>
    private static readonly List<Delegate> _autoloaders = new();
    private static readonly object _autoloaderLock = new();

    /// <summary>
    /// Per-execution context, stored as AsyncLocal so each async task/request gets its own isolated context.
    /// This enables millions of concurrent requests with Kestrel without cross-contamination.
    /// </summary>
    private static readonly AsyncLocal<ExecutionContext?> _currentContext = new();

    static Runtime()
    {
        SdkInitializer.Init();
    }

    /// <summary>
    /// Gets the current execution context for the async task.
    /// Returns null if no context has been set (e.g., during non-request execution).
    /// </summary>
    public static ExecutionContext? CurrentContext => _currentContext.Value;

    /// <summary>
    /// Registers a global autoloader, available to all future executions.
    /// Thread-safe via lock.
    /// </summary>
    public static void RegisterAutoloader(Delegate autoloader)
    {
        lock (_autoloaderLock)
        {
            _autoloaders.Add(autoloader);
        }
    }

    /// <summary>
    /// Attempts to autoload a class using registered autoloaders.
    /// </summary>
    public static bool Autoload(string className)
    {
        lock (_autoloaderLock)
        {
            foreach (var autoloader in _autoloaders)
            {
                autoloader.DynamicInvoke(className);
                if (TypeTable.GetType(className)?.RuntimeType != null) return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Attempts to autoload a function using registered autoloaders.
    /// </summary>
    public static bool AutoloadFunction(string functionName)
    {
        lock (_autoloaderLock)
        {
            foreach (var autoloader in _autoloaders)
            {
                autoloader.DynamicInvoke(functionName);
                if (FunctionTable.GetFunction(functionName) != null) return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Executes a PHP file by path, using the OpCache for parsed ASTs.
    /// Requires an active ExecutionContext; use within a request handler.
    /// </summary>
    public static void ExecuteFile(string filePath)
    {
        var path = Path.GetFullPath(filePath);
        try
        {
            // Use OpCache to get or parse the AST
            var ast = AstCache.GetOrParse(path);
            
            // Read the full source again (needed for visitor passes)
            var source = File.ReadAllText(path).AsSpan();
            
            Execute(in source, path, ast);
        }
        catch (FunctionNotDefinedException functionNotDefinedException)
        {
            FatalError(functionNotDefinedException);
        }
    }

    private static void FatalError(Exception exception)
    {
        Console.Clear();
        Console.WriteLine($"Fatal Error: {exception.Message}");
    }

    /// <summary>
    /// Executes PHP code directly from a string (memory).
    /// For files, use ExecuteFile() to benefit from OpCache.
    /// </summary>
    public static void Execute(in ReadOnlySpan<char> fileContent, string fileName = "vm:0", SyntaxNode? precompiledAst = null)
    {
        SyntaxNode? ast = precompiledAst;
        
        if (ast == null)
        {
            var tokens = Lexer.ParseSpan(fileContent);
            ast = Parser.Parse(in tokens, in fileContent);
        }
        
        var visitors = new Visitor(
            new SemanticVisitor()
        );

        ast?.Accept(visitors, in fileContent);
        
        var compiler = new Compiler();
        compiler.WithFileName(fileName);
        ast?.Accept(compiler, in fileContent);

        try
        {
            compiler.Execute();
        }
        catch (DieException)
        {
        }
    }

    /// <summary>
    /// Gets the accumulated output from the current execution context and clears the buffer.
    /// </summary>
    public static string GetExecutionResult()
    {
        var context = _currentContext.Value;
        if (context == null)
        {
            // Fallback to legacy behavior if no context set
            SdkInitializer.StdoutStream.Flush();
            var stream = SdkInitializer.StdoutMemory;
            stream.Position = 0;
            using var reader = new StreamReader(stream, Encoding.UTF8, false, leaveOpen: true);
            var result = reader.ReadToEnd();
            stream.Position = 0;
            stream.SetLength(0);
            return result;
        }

        return context.GetAndClearOutput();
    }

    /// <summary>
    /// Sets the execution context for the current async task.
    /// Call this at the start of each HTTP request handler.
    /// </summary>
    public static void SetContext(ExecutionContext context)
    {
        _currentContext.Value = context;
    }

    /// <summary>
    /// Clears the execution context for the current async task.
    /// Call this at the end of each HTTP request handler.
    /// </summary>
    public static void ClearContext()
    {
        _currentContext.Value = null;
    }

    public static bool CoerceToBool(object? obj)
    {
        if (obj == null) return false;
        if (obj is bool b) return b;
        if (obj is int i) return i != 0;
        if (obj is double d) return d != 0.0;
        if (obj is string s) return s != "" && s != "0";
        if (obj is System.Collections.IDictionary dict) return dict.Count > 0;
        return true;
    }

    public static bool StrictEquals(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (a.GetType() != b.GetType()) return false;
        return a.Equals(b);
    }

    public static object NullCoalesce(object? left, object? right)
    {
        return left ?? (right ?? "");
    }
}