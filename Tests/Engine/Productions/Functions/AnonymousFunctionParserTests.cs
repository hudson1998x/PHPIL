using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Productions;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;

namespace PHPIL.Tests.Engine;

public class AnonymousFunctionParserTests : BaseTest
{
    // Helper to parse an anonymous function
    private SyntaxNode ParseAnonFunc(string source)
    {
        var tokens = Lexer.ParseSpan(source.AsSpan())
            .Where(t => t.Kind is not (TokenKind.Whitespace or TokenKind.NewLine))
            .ToArray();

        var ctx = new ParserContext(tokens.AsSpan(), source.AsSpan());

        var pattern = new AnonymousFunctionPattern();
        if (!pattern.TryMatch(ref ctx, out var node) || node == null)
            throw new Exception("Failed to parse anonymous function: " + source);

        return node;
    }

    [PHPILTest]
    public void Parses_EmptyAnonymousFunction()
    {
        var source = "function() {}";
        var node = ParseAnonFunc(source);
        AssertEqual(typeof(AnonymousFunctionNode), node.GetType());

        var anon = (AnonymousFunctionNode)node;
        AssertEqual(0, anon.Params.Count);
        AssertEqual(0, anon.UseCaptures.Count);
        AssertEqual(false, anon.ReturnType.HasValue);
        AssertEqual(typeof(BlockNode), anon.Body!.GetType());
    }

    [PHPILTest]
    public void Parses_AnonymousFunction_WithParameters()
    {
        var source = "function($a, $b) {}";
        var node = ParseAnonFunc(source);
        var anon = (AnonymousFunctionNode)node;

        var paramSpan = "$a, $b".AsSpan();

        AssertEqual(2, anon.Params.Count);
    }

    [PHPILTest]
    public void Parses_AnonymousFunction_WithUseCapture()
    {
        var source = "function($x) use ($y, $z) {}";
        var node = ParseAnonFunc(source);
        var anon = (AnonymousFunctionNode)node;

        var captureSpan = "($y, $z)".AsSpan();

        AssertEqual(1, anon.Params.Count);
        AssertEqual(2, anon.UseCaptures.Count);
    }

    [PHPILTest]
    public void Parses_AnonymousFunction_WithReturnType()
    {
        var source = "function(): int {}";
        var node = ParseAnonFunc(source);
        var anon = (AnonymousFunctionNode)node;

        var returnSpan = ": int {}".AsSpan();

        AssertEqual(true, anon.ReturnType.HasValue);
        AssertEqual("int", anon.ReturnType!.Value.TextValue(in returnSpan));
    }

    [PHPILTest]
    public void Parses_FullAnonymousFunction()
    {
        var source = "function($a, $b) use ($x) : string { return $a + $b; }";
        var node = ParseAnonFunc(source);
        var anon = (AnonymousFunctionNode)node;

        var returnSpan = ": string { return $a + $b; }".AsSpan();

        AssertEqual(2, anon.Params.Count);
        AssertEqual(1, anon.UseCaptures.Count);

        AssertEqual(true, anon.ReturnType.HasValue);
        AssertEqual("string", anon.ReturnType!.Value.TextValue(in returnSpan));

        AssertEqual(typeof(BlockNode), anon.Body!.GetType());
    }

    [PHPILTest]
    public void Parses_AnonymousFunction_ByRefUseCapture()
    {
        var source = "function() use (&$x, $y) {}";
        var node = ParseAnonFunc(source);
        var anon = (AnonymousFunctionNode)node;

        AssertEqual(0, anon.Params.Count);
        AssertEqual(2, anon.UseCaptures.Count);
    }
}