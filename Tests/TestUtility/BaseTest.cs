using PHPIL.Engine.CodeLexer;

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
}