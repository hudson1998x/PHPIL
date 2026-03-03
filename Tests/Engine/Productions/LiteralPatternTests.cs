using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Tests.Engine;

public class LiteralPatternTests : BaseTest
{
    private LiteralNode ParseLiteral(string source)
    {
        var tokens = Lexer.ParseSpan(source.AsSpan())
            .Where(t => t.Kind is not (TokenKind.Whitespace or TokenKind.NewLine))
            .ToArray();

        var ctx = new ParserContext(tokens.AsSpan(), source.AsSpan());

        var pattern = new LiteralPattern();
        if (!pattern.TryMatch(ref ctx, out var node) || node is not LiteralNode literal)
            throw new Exception("Failed to parse literal: " + source);

        return literal;
    }

    [PHPILTest]
    public void Parses_IntegerLiteral()
    {
        var source = "123";
        var span = source.AsSpan();

        var node = ParseLiteral(source);
        AssertEqual(TokenKind.IntLiteral, node.Token.Kind);
        AssertEqual("123", node.Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_NegativeIntegerLiteral()
    {
        var source = "-456";
        var span = source.AsSpan();

        var node = ParseLiteral(source);
        AssertEqual(TokenKind.IntLiteral, node.Token.Kind);
        AssertEqual("-456", node.Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_FloatLiteral()
    {
        var source = "3.1415";
        var span = source.AsSpan();

        var node = ParseLiteral(source);
        AssertEqual(TokenKind.FloatLiteral, node.Token.Kind);
        AssertEqual("3.1415", node.Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_NegativeFloatLiteral()
    {
        var source = "-0.25";
        var span = source.AsSpan();

        var node = ParseLiteral(source);
        AssertEqual(TokenKind.FloatLiteral, node.Token.Kind);
        AssertEqual("-0.25", node.Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_StringLiteral()
    {
        var source = "'hello world'";
        var span = source.AsSpan();

        var node = ParseLiteral(source);
        AssertEqual(TokenKind.StringLiteral, node.Token.Kind);
        AssertEqual("'hello world'", node.Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_EmptyStringLiteral()
    {
        var source = "''";
        var span = source.AsSpan();

        var node = ParseLiteral(source);
        AssertEqual(TokenKind.StringLiteral, node.Token.Kind);
        AssertEqual("''", node.Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_BooleanTrueLiteral()
    {
        var source = "true";
        var span = source.AsSpan();

        var node = ParseLiteral(source);
        AssertEqual(TokenKind.TrueLiteral, node.Token.Kind);
        AssertEqual("true", node.Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_BooleanFalseLiteral()
    {
        var source = "false";
        var span = source.AsSpan();

        var node = ParseLiteral(source);
        AssertEqual(TokenKind.FalseLiteral, node.Token.Kind);
        AssertEqual("false", node.Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_NullLiteral()
    {
        var source = "null";
        var span = source.AsSpan();

        var node = ParseLiteral(source);
        AssertEqual(TokenKind.NullLiteral, node.Token.Kind);
        AssertEqual("null", node.Token.TextValue(in span));
    }
}