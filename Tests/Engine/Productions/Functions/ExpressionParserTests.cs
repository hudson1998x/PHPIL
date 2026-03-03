using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.Productions;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions.Patterns;

namespace PHPIL.Tests.Engine;

public class ExpressionParserTests : BaseTest
{
    private SyntaxNode ParseExpr(string source)
    {
        var tokens = Lexer.ParseSpan(source.AsSpan())
            .Where(t => t.Kind is not (TokenKind.Whitespace or TokenKind.NewLine))
            .ToArray();
        var context = new ParserContext(tokens.AsSpan(), source.AsSpan());

        if (new InnerExpressionPattern().TryMatch(ref context, out var node))
            return node!;
        
        throw new Exception("Failed to parse expression: " + source);
    }

    [PHPILTest]
    public void Parse_PrefixIncrement()
    {
        var node = ParseExpr("++$x");
        AssertEqual(typeof(PrefixExpressionNode), node.GetType());

        var prefix = (PrefixExpressionNode)node;
        AssertEqual(TokenKind.Increment, prefix.Operator.Kind);
        AssertEqual(typeof(VariableNode), prefix.Operand.GetType());
    }

    [PHPILTest]
    public void Parse_PrefixDecrement()
    {
        var node = ParseExpr("--$y");
        AssertEqual(typeof(PrefixExpressionNode), node.GetType());

        var prefix = (PrefixExpressionNode)node;
        AssertEqual(TokenKind.Decrement, prefix.Operator.Kind);
        AssertEqual(typeof(VariableNode), prefix.Operand.GetType());
    }

    [PHPILTest]
    public void Parse_PostfixIncrement()
    {
        var node = ParseExpr("$i++");
        AssertEqual(typeof(PostfixExpressionNode), node.GetType());

        var postfix = (PostfixExpressionNode)node;
        AssertEqual(TokenKind.Increment, postfix.Operator.Kind);
        AssertEqual(typeof(VariableNode), postfix.Operand.GetType());
    }

    [PHPILTest]
    public void Parse_PostfixDecrement()
    {
        var node = ParseExpr("$i--");
        AssertEqual(typeof(PostfixExpressionNode), node.GetType());

        var postfix = (PostfixExpressionNode)node;
        AssertEqual(TokenKind.Decrement, postfix.Operator.Kind);
        AssertEqual(typeof(VariableNode), postfix.Operand.GetType());
    }

    [PHPILTest]
    public void Parse_UnaryPlus()
    {
        var node = ParseExpr("+$a");
        AssertEqual(typeof(PrefixExpressionNode), node.GetType());

        var prefix = (PrefixExpressionNode)node;
        AssertEqual(TokenKind.Add, prefix.Operator.Kind);
        AssertEqual(typeof(VariableNode), prefix.Operand.GetType());
    }

    [PHPILTest]
    public void Parse_UnaryMinus()
    {
        var node = ParseExpr("-$b");
        AssertEqual(typeof(PrefixExpressionNode), node.GetType());

        var prefix = (PrefixExpressionNode)node;
        AssertEqual(TokenKind.Subtract, prefix.Operator.Kind);
        AssertEqual(typeof(VariableNode), prefix.Operand.GetType());
    }

    [PHPILTest]
    public void Parse_BinaryPrecedence()
    {
        var node = ParseExpr("$a + $b * $c");

        // Should parse as: $a + ($b * $c)
        AssertEqual(typeof(BinaryOpNode), node.GetType());
        var plus = (BinaryOpNode)node;
        AssertEqual(TokenKind.Add, plus.Operator);

        AssertEqual(typeof(VariableNode), plus.Left!.GetType());

        var mult = plus.Right!;
        AssertEqual(typeof(BinaryOpNode), mult.GetType());
        var multiply = (BinaryOpNode)mult;
        AssertEqual(TokenKind.Multiply, multiply.Operator);
        AssertEqual(typeof(VariableNode), multiply.Left!.GetType());
        AssertEqual(typeof(VariableNode), multiply.Right!.GetType());
    }

    [PHPILTest]
    public void Parse_MixedPrefixPostfix()
    {
        var node = ParseExpr("++$i * $j++");

        // Should parse as: (++$i) * ($j++)
        AssertEqual(typeof(BinaryOpNode), node.GetType());
        var mul = (BinaryOpNode)node;
        AssertEqual(TokenKind.Multiply, mul.Operator);

        AssertEqual(typeof(PrefixExpressionNode), mul.Left!.GetType());
        AssertEqual(TokenKind.Increment, ((PrefixExpressionNode)mul.Left).Operator.Kind);

        AssertEqual(typeof(PostfixExpressionNode), mul.Right!.GetType());
        AssertEqual(TokenKind.Increment, ((PostfixExpressionNode)mul.Right).Operator.Kind);
    }
}