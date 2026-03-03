using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Tests.Engine;

public class ArgumentListParserTests : BaseTest
{
    // Helper to parse an argument list
    private ArgumentListNode ParseArgumentList(string source)
    {
        var tokens = Lexer.ParseSpan(source.AsSpan())
            .Where(t => t.Kind is not (TokenKind.Whitespace or TokenKind.NewLine))
            .ToArray();

        var ctx = new ParserContext(tokens.AsSpan(), source.AsSpan());

        var pattern = new ArgumentListPattern();
        if (!pattern.TryMatch(ref ctx, out var node) || node is not ArgumentListNode argsNode)
            throw new Exception("Failed to parse argument list: " + source);

        return argsNode;
    }

    [PHPILTest]
    public void Parses_EmptyArgumentList()
    {
        var source = "()";
        var span = source.AsSpan();

        var node = ParseArgumentList(source);
        AssertEqual(0, node.Arguments.Count);
    }

    [PHPILTest]
    public void Parses_SingleArgument()
    {
        var source = "($x)";
        var span = source.AsSpan();

        var node = ParseArgumentList(source);
        AssertEqual(1, node.Arguments.Count);
        AssertEqual("$x", ((VariableNode)node.Arguments[0]).Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_MultipleArguments()
    {
        var source = "($a, $b, 123)";
        var span = source.AsSpan();

        var node = ParseArgumentList(source);
        AssertEqual(3, node.Arguments.Count);

        AssertEqual("$a", ((VariableNode)node.Arguments[0]).Token.TextValue(in span));
        AssertEqual("$b", ((VariableNode)node.Arguments[1]).Token.TextValue(in span));
        AssertEqual("123", ((LiteralNode)node.Arguments[2]).Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_TrailingComma()
    {
        var source = "($x,)";
        var span = source.AsSpan();

        var node = ParseArgumentList(source);
        AssertEqual(1, node.Arguments.Count);
        AssertEqual("$x", ((VariableNode)node.Arguments[0]).Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_NestedFunctionCalls()
    {
        var source = "(foo($a), bar($b, $c))";
        var span = source.AsSpan();

        var node = ParseArgumentList(source);
        AssertEqual(2, node.Arguments.Count);

        var arg1 = (FunctionCallNode)node.Arguments[0];
        AssertEqual("foo", ((IdentifierNode)arg1.Callee!).Token.TextValue(in span));
        AssertEqual(1, arg1.Args.Count);
        AssertEqual("$a", ((VariableNode)arg1.Args[0]).Token.TextValue(in span));

        var arg2 = (FunctionCallNode)node.Arguments[1];
        AssertEqual("bar", ((IdentifierNode)arg2.Callee!).Token.TextValue(in span));
        AssertEqual(2, arg2.Args.Count);
        AssertEqual("$b", ((VariableNode)arg2.Args[0]).Token.TextValue(in span));
        AssertEqual("$c", ((VariableNode)arg2.Args[1]).Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_ExpressionArguments()
    {
        var source = "($a + $b, $c * 2)";
        var span = source.AsSpan();

        var node = ParseArgumentList(source);
        AssertEqual(2, node.Arguments.Count);

        var bin1 = (BinaryOpNode)node.Arguments[0];
        AssertEqual("$a", ((VariableNode)bin1.Left).Token.TextValue(in span));
        AssertEqual("$b", ((VariableNode)bin1.Right).Token.TextValue(in span));

        var bin2 = (BinaryOpNode)node.Arguments[1];
        AssertEqual("$c", ((VariableNode)bin2.Left).Token.TextValue(in span));
        AssertEqual("2", ((LiteralNode)bin2.Right).Token.TextValue(in span));
    }
}