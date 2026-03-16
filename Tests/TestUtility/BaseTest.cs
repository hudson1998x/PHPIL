using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Visitors;

namespace PHPIL.Tests;

public abstract class BaseTest
{
    protected void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"Expected: {expected}, Got: {actual}");
    }

    protected void AssertEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
    {
        var expList = expected.ToList();
        var actList = actual.ToList();

        if (expList.Count != actList.Count || !expList.SequenceEqual(actList))
        {
            throw new Exception($"Collection mismatch.\n       Expected: [{string.Join(", ", expList)}]\n       Actual:   [{string.Join(", ", actList)}]");
        }
    }

    protected List<TokenKind> GetKinds(string source, bool ignoreTrivia = true)
    {
        var tokens = Lexer.ParseSpan(source.AsSpan(), true).ToList();
        return ignoreTrivia 
            ? tokens.Where(t => t.Kind is not (TokenKind.Whitespace or TokenKind.NewLine)).Select(t => t.Kind).ToList() 
            : tokens.Select(t => t.Kind).ToList();
    }
    
    protected void AssertNotNull(object? obj, string? message = null)
    {
        if (obj is null)
            throw new Exception(message ?? "Expected object to be not null, but it was null.");
    }

    protected string GetQualifiedName(ExpressionNode? node, in ReadOnlySpan<char> span)
    {
        if (node is PHPIL.Engine.SyntaxTree.Structure.QualifiedNameNode qn)
        {
            var parts = new List<string>();
            foreach (var part in qn.Parts)
                parts.Add(part.TextValue(in span));
            return string.Join("\\", parts);
        }
        if (node is IdentifierNode id)
        {
            return id.Token.TextValue(in span);
        }
        return "";
    }
    
    protected void ResetTestState()
    {
        TypeTable.Clear();
        Compiler.ResetModule();
    }

    protected void AssertTrue(bool condition, string? message = null)
    {
        if (!condition)
            throw new Exception(message ?? "Expected condition to be true, but it was false.");
    }

    protected void AssertFalse(bool condition, string? message = null)
    {
        if (condition)
            throw new Exception(message ?? "Expected condition to be false, but it was true.");
    }

    protected void AssertNull(object? obj, string? message = null)
    {
        if (obj is not null)
            throw new Exception(message ?? $"Expected object to be null, but it was {obj}.");
    }
}