using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Tests.Engine;

public class WhileExpressionParserTests : BaseTest
{
    // Helper to parse a while loop
    private WhileNode ParseWhile(string source)
    {
        var tokens = Lexer.ParseSpan(source.AsSpan())
            .Where(t => t.Kind is not (TokenKind.Whitespace or TokenKind.NewLine))
            .ToArray();

        var ctx = new ParserContext(tokens.AsSpan(), source.AsSpan());

        var pattern = new WhileExpressionPattern();
        if (!pattern.TryMatch(ref ctx, out var node) || node is not WhileNode whileNode)
            throw new Exception("Failed to parse while loop: " + source);

        return whileNode;
    }

    [PHPILTest]
    public void Parses_SimpleWhileLoop()
    {
        var source = "while($i < 10) {}";
        var span = source.AsSpan();

        var node = ParseWhile(source);

        // Condition
        AssertEqual(typeof(BinaryOpNode), node.Expression!.GetType());
        var cond = (BinaryOpNode)node.Expression;
        AssertEqual("$i", ((VariableNode)cond.Left).Token.TextValue(in span));
        AssertEqual("10", ((LiteralNode)cond.Right).Token.TextValue(in span));

        // Body
        AssertEqual(typeof(BlockNode), node.Body!.GetType());
    }

    [PHPILTest]
    public void Parses_WhileLoop_WithFunctionCallCondition()
    {
        var source = "while(hasNext($iter)) {}";
        var span = source.AsSpan();

        var node = ParseWhile(source);

        AssertEqual(typeof(FunctionCallNode), node.Expression!.GetType());
        var call = (FunctionCallNode)node.Expression;
        AssertEqual("hasNext", ((IdentifierNode)call.Callee!).Token.TextValue(in span));
        AssertEqual(1, call.Args.Count);
        AssertEqual("$iter", ((VariableNode)call.Args[0]).Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_NestedWhileLoops()
    {
        var source = @"
        while($i < 10) {
            while($j < 5) {}
        }";
        var span = source.AsSpan();

        var outer = ParseWhile(source);

        // Outer loop
        AssertEqual(typeof(BinaryOpNode), outer.Expression!.GetType());
        AssertEqual(typeof(BlockNode), outer.Body!.GetType());

        // Inner loop
        var innerStmt = outer.Body!.Statements[0];
        AssertEqual(typeof(WhileNode), innerStmt.GetType());
        var inner = (WhileNode)innerStmt;
        AssertEqual(typeof(BinaryOpNode), inner.Expression!.GetType());
        AssertEqual(typeof(BlockNode), inner.Body!.GetType());
    }

    [PHPILTest]
    public void Parses_WhileLoop_WithComplexExpression()
    {
        var source = "while($a + $b < $c * 2) {}";
        var span = source.AsSpan();

        var node = ParseWhile(source);

        // Top-level should be BinaryOpNode for '<'
        AssertEqual(typeof(BinaryOpNode), node.Expression!.GetType());
        var cond = (BinaryOpNode)node.Expression;

        // Left side: $a + $b
        AssertEqual(typeof(BinaryOpNode), cond.Left.GetType());
        var left = (BinaryOpNode)cond.Left;
        AssertEqual("$a", ((VariableNode)left.Left).Token.TextValue(in span));
        AssertEqual("$b", ((VariableNode)left.Right).Token.TextValue(in span));

        // Right side: $c * 2
        AssertEqual(typeof(BinaryOpNode), cond.Right.GetType());
        var right = (BinaryOpNode)cond.Right;
        AssertEqual("$c", ((VariableNode)right.Left).Token.TextValue(in span));
        AssertEqual("2", ((LiteralNode)right.Right).Token.TextValue(in span));
    }
}