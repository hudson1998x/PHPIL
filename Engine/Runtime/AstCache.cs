using System.Security.Cryptography;
using System.Text;
using PHPIL.Engine.Productions;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Engine.Runtime;

/// <summary>
/// Immutable, thread-safe cache for parsed PHP ASTs.
/// Validates cached ASTs by comparing file content hash; automatically invalidates on file change.
/// </summary>
public static class AstCache
{
    private class CachedAst
    {
        public string FileHash { get; set; } = "";
        public SyntaxNode? Ast { get; set; }
    }

    /// <summary>
    /// Global cache mapping file path (normalized) → (content hash, parsed AST).
    /// Thread-safe: uses ConcurrentDictionary.
    /// </summary>
    private static readonly Dictionary<string, CachedAst> _cache = new();
    private static readonly object _lockObj = new();

    /// <summary>
    /// Computes a SHA256 hash of file content for cache validation.
    /// </summary>
    private static string ComputeFileHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var hashBytes = sha256.ComputeHash(fileStream);
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Gets or parses a PHP file AST.
    /// If the file is in cache and hash matches, returns cached AST.
    /// Otherwise, parses file and updates cache.
    /// </summary>
    public static SyntaxNode? GetOrParse(string filePath)
    {
        var normalizedPath = Path.GetFullPath(filePath);

        // Compute current file hash
        string currentHash;
        try
        {
            currentHash = ComputeFileHash(normalizedPath);
        }
        catch
        {
            // File doesn't exist or is unreadable - let caller handle
            throw;
        }

        lock (_lockObj)
        {
            // Check if in cache with valid hash
            if (_cache.TryGetValue(normalizedPath, out var cached))
            {
                if (cached.FileHash == currentHash)
                {
                    // Cache hit!
                    return cached.Ast;
                }
            }

            // Cache miss or hash mismatch - parse and cache
            var source = File.ReadAllText(normalizedPath).AsSpan();
            var tokens = CodeLexer.Lexer.ParseSpan(source);
            var ast = Productions.Parser.Parse(in tokens, in source);

            _cache[normalizedPath] = new CachedAst
            {
                FileHash = currentHash,
                Ast = ast
            };

            return ast;
        }
    }

    /// <summary>
    /// Clears the entire cache. Use for testing or when you want to force re-parse all files.
    /// </summary>
    public static void Clear()
    {
        lock (_lockObj)
        {
            _cache.Clear();
        }
    }

    /// <summary>
    /// Gets cache statistics (for debugging/monitoring).
    /// </summary>
    public static (int CachedFiles, int TotalEntries) GetStats()
    {
        lock (_lockObj)
        {
            return (_cache.Count, _cache.Count);
        }
    }
}
