using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Tests.Engine;

public class ForExpressionParserTests : BaseTest
{
    // Helper to parse a 'for' loop
    private For ParseFor(string source)
    {
        var tokens = Lexer.ParseSpan(source.AsSpan())
            .Where(t => t.Kind is not (TokenKind.Whitespace or TokenKind.NewLine))
            .ToArray();

        var ctx = new ParserContext(tokens.AsSpan(), source.AsSpan());

        var pattern = new ForExpressionPattern();
        if (!pattern.TryMatch(ref ctx, out var node) || node is not For forNode)
            throw new Exception("Failed to parse for loop: " + source);

        return forNode;
    }

    [PHPILTest]
    public void Parses_SimpleForLoop()
    {
        var source = "for($i = 0; $i < 10; $i++) {}";
        var span = source.AsSpan();

        var node = ParseFor(source);

        // Init
        AssertEqual(typeof(BinaryOpNode), node.Init!.GetType());
        var init = (BinaryOpNode)node.Init;
        AssertEqual("$i", ((VariableNode)init.Left!).Token.TextValue(in span));
        AssertEqual("0", ((LiteralNode)init.Right!).Token.TextValue(in span));

        // Condition
        AssertEqual(typeof(BinaryOpNode), node.Condition!.GetType());
        var cond = (BinaryOpNode)node.Condition;
        AssertEqual("$i", ((VariableNode)cond.Left!).Token.TextValue(in span));
        AssertEqual("10", ((LiteralNode)cond.Right!).Token.TextValue(in span));

        // Increment
        AssertEqual(typeof(PostfixExpressionNode), node.Increment!.GetType());
        var incr = (PostfixExpressionNode)node.Increment;
        AssertEqual("$i", ((VariableNode)incr.Operand).Token.TextValue(in span));

        // Body
        AssertEqual(typeof(BlockNode), node.Body!.GetType());
    }

    [PHPILTest]
    public void Parses_ForLoop_WithoutInit()
    {
        var source = "for(; $i < 5; $i++) {}";

        var node = ParseFor(source);
        AssertEqual(null, node.Init);

        AssertEqual(typeof(BinaryOpNode), node.Condition!.GetType());
        AssertEqual(typeof(PostfixExpressionNode), node.Increment!.GetType());
    }

    [PHPILTest]
    public void Parses_ForLoop_WithoutCondition()
    {
        var source = "for($i = 0;; $i++) {}";
        var node = ParseFor(source);

        AssertEqual(typeof(BinaryOpNode), node.Init!.GetType());
        AssertEqual(null, node.Condition);

        AssertEqual(typeof(PostfixExpressionNode), node.Increment!.GetType());
    }

    [PHPILTest]
    public void Parses_ForLoop_WithoutIncrement()
    {
        var source = "for($i = 0; $i < 5;) {}";

        var node = ParseFor(source);

        AssertEqual(typeof(BinaryOpNode), node.Init!.GetType());
        AssertEqual(typeof(BinaryOpNode), node.Condition!.GetType());
        AssertEqual(null, node.Increment);
    }

    [PHPILTest]
    public void Parses_EmptyForLoop()
    {
        var source = "for(;;) {}";
        var node = ParseFor(source);

        AssertEqual(null, node.Init);
        AssertEqual(null, node.Condition);
        AssertEqual(null, node.Increment);
        AssertEqual(typeof(BlockNode), node.Body!.GetType());
    }

    [PHPILTest]
    public void Parses_NestedForLoops()
    {
        var source = @"
        for($i = 0; $i < 10; $i++) {
            for($j = 0; $j < 5; $j++) {}
        }";

        var outer = ParseFor(source);

        // Outer loop checks
        AssertEqual(typeof(BinaryOpNode), outer.Init!.GetType());
        AssertEqual(typeof(BinaryOpNode), outer.Condition!.GetType());
        AssertEqual(typeof(PostfixExpressionNode), outer.Increment!.GetType());
        AssertEqual(typeof(BlockNode), outer.Body!.GetType());

        // Inner loop
        var innerStmt = (outer.Body! as BlockNode)!.Statements[0];
        AssertEqual(typeof(For), innerStmt.GetType());

        var inner = (For)innerStmt;
        AssertEqual(typeof(BinaryOpNode), inner.Init!.GetType());
        AssertEqual(typeof(BinaryOpNode), inner.Condition!.GetType());
        AssertEqual(typeof(PostfixExpressionNode), inner.Increment!.GetType());
    }

    [PHPILTest]
    public void Parses_ForLoop_WithFunctionCallIncrement()
    {
        var source = "for($i = 0; $i < 10; next($i)) {}";
        var span = source.AsSpan();

        var node = ParseFor(source);

        AssertEqual(typeof(BinaryOpNode), node.Init!.GetType());
        AssertEqual(typeof(BinaryOpNode), node.Condition!.GetType());

        AssertEqual(typeof(FunctionCallNode), node.Increment!.GetType());
        var incrCall = (FunctionCallNode)node.Increment!;
        AssertEqual("next", GetQualifiedName(incrCall.Callee, in span));
        AssertEqual(1, incrCall.Args.Count);
        AssertEqual("$i", ((VariableNode)incrCall.Args[0]).Token.TextValue(in span));
    }
}