using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Tests.Engine;

public class ReturnPatternTests : BaseTest
{
    private ReturnNode ParseReturn(string source)
    {
        var tokens = Lexer.ParseSpan(source.AsSpan())
            .Where(t => t.Kind is not (TokenKind.Whitespace or TokenKind.NewLine))
            .ToArray();

        var ctx = new ParserContext(tokens.AsSpan(), source.AsSpan());

        var pattern = new ReturnPattern();
        if (!pattern.TryMatch(ref ctx, out var node) || node is not ReturnNode returnNode)
            throw new Exception("Failed to parse return statement: " + source);

        return returnNode;
    }

    [PHPILTest]
    public void Parses_ReturnWithoutExpression()
    {
        var source = "return;";

        var node = ParseReturn(source);
        AssertEqual(null, node.Expression);
    }

    [PHPILTest]
    public void Parses_ReturnWithVariable()
    {
        var source = "return $x;";
        var span = source.AsSpan();

        var node = ParseReturn(source);
        AssertEqual(typeof(VariableNode), node.Expression!.GetType());
        var varNode = (VariableNode)node.Expression;
        AssertEqual("$x", varNode.Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_ReturnWithLiteral()
    {
        var source = "return 123;";
        var span = source.AsSpan();

        var node = ParseReturn(source);
        AssertEqual(typeof(LiteralNode), node.Expression!.GetType());
        var litNode = (LiteralNode)node.Expression;
        AssertEqual("123", litNode.Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_ReturnWithBinaryExpression()
    {
        var source = "return $a + $b;";
        var span = source.AsSpan();

        var node = ParseReturn(source);
        AssertEqual(typeof(BinaryOpNode), node.Expression!.GetType());
        var bin = (BinaryOpNode)node.Expression;

        AssertEqual("$a", ((VariableNode)bin.Left!).Token.TextValue(in span));
        AssertEqual("$b", ((VariableNode)bin.Right!).Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_ReturnWithoutSemicolonAtEnd()
    {
        var source = "return 42";
        var span = source.AsSpan();

        var node = ParseReturn(source);
        AssertEqual(typeof(LiteralNode), node.Expression!.GetType());
        var litNode = (LiteralNode)node.Expression;
        AssertEqual("42", litNode.Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_ReturnWithFunctionCall()
    {
        var source = "return foo($x, $y);";
        var span = source.AsSpan();

        var node = ParseReturn(source);
        AssertEqual(typeof(FunctionCallNode), node.Expression!.GetType());

        var call = (FunctionCallNode)node.Expression!;
        AssertEqual("foo", GetQualifiedName(call.Callee, in span));
        AssertEqual(2, call.Args.Count);

        AssertEqual("$x", ((VariableNode)call.Args[0]).Token.TextValue(in span));
        AssertEqual("$y", ((VariableNode)call.Args[1]).Token.TextValue(in span));
    }
}