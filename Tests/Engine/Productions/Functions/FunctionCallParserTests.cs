using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Productions;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;

namespace PHPIL.Tests.Engine;

public class FunctionCallParserTests : BaseTest
{
    // Helper to parse a function call
    private FunctionCallNode ParseFunctionCall(string source)
    {
        var tokens = Lexer.ParseSpan(source.AsSpan())
            .Where(t => t.Kind is not (TokenKind.Whitespace or TokenKind.NewLine))
            .ToArray();

        var ctx = new ParserContext(tokens.AsSpan(), source.AsSpan());

        var pattern = new FunctionCallPattern();
        if (!pattern.TryMatch(ref ctx, out var node) || node is not FunctionCallNode callNode)
            throw new Exception("Failed to parse function call: " + source);

        return callNode;
    }

    [PHPILTest]
    public void Parses_FunctionCall_NoArguments()
    {
        var source = "print()";
        var span = source.AsSpan();

        var node = ParseFunctionCall(source);

        AssertEqual("print", ((IdentifierNode)node.Callee!).Token.TextValue(in span));
        AssertEqual(0, node.Args.Count);
    }

    [PHPILTest]
    public void Parses_FunctionCall_SingleArgument()
    {
        var source = "print($x)";
        var span = source.AsSpan();

        var node = ParseFunctionCall(source);

        AssertEqual("print", ((IdentifierNode)node.Callee!).Token.TextValue(in span));
        AssertEqual(1, node.Args.Count);
        AssertEqual("$x", ((VariableNode)node.Args[0]).Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_FunctionCall_MultipleArguments()
    {
        var source = "sum($a, $b, 123)";
        var span = source.AsSpan();

        var node = ParseFunctionCall(source);

        AssertEqual("sum", ((IdentifierNode)node.Callee!).Token.TextValue(in span));
        AssertEqual(3, node.Args.Count);

        AssertEqual("$a", ((VariableNode)node.Args[0]).Token.TextValue(in span));
        AssertEqual("$b", ((VariableNode)node.Args[1]).Token.TextValue(in span));
        AssertEqual("123", ((LiteralNode)node.Args[2]).Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_FunctionCall_NestedCall()
    {
        var source = "foo(bar($x), $y)";
        var span = source.AsSpan();

        var node = ParseFunctionCall(source);

        AssertEqual("foo", ((IdentifierNode)node.Callee!).Token.TextValue(in span));
        AssertEqual(2, node.Args.Count);

        var nested = (FunctionCallNode)node.Args[0];
        AssertEqual("bar", ((IdentifierNode)nested.Callee!).Token.TextValue(in span));
        AssertEqual(1, nested.Args.Count);
        AssertEqual("$x", ((VariableNode)nested.Args[0]).Token.TextValue(in span));

        AssertEqual("$y", ((VariableNode)node.Args[1]).Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_FunctionCall_ExpressionArguments()
    {
        var source = "max($a + $b, $c * 2)";
        var span = source.AsSpan();

        var node = ParseFunctionCall(source);

        AssertEqual("max", ((IdentifierNode)node.Callee!).Token.TextValue(in span));
        AssertEqual(2, node.Args.Count);

        // First argument is a BinaryOpNode: $a + $b
        AssertEqual(typeof(BinaryOpNode), node.Args[0].GetType());
        var bin1 = (BinaryOpNode)node.Args[0];
        AssertEqual("$a", ((VariableNode)bin1.Left).Token.TextValue(in span));
        AssertEqual("$b", ((VariableNode)bin1.Right).Token.TextValue(in span));

        // Second argument is a BinaryOpNode: $c * 2
        AssertEqual(typeof(BinaryOpNode), node.Args[1].GetType());
        var bin2 = (BinaryOpNode)node.Args[1];
        AssertEqual("$c", ((VariableNode)bin2.Left).Token.TextValue(in span));
        AssertEqual("2", ((LiteralNode)bin2.Right).Token.TextValue(in span));
    }
}