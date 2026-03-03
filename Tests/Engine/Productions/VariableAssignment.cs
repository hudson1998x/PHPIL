using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Tests.Engine;

public class VariableAssignmentParserTests : BaseTest
{
    private BinaryOpNode ParseAssignment(string source)
    {
        var tokens = Lexer.ParseSpan(source.AsSpan())
            .Where(t => t.Kind is not (TokenKind.Whitespace or TokenKind.NewLine))
            .ToArray();

        var ctx = new ParserContext(tokens.AsSpan(), source.AsSpan());

        var pattern = new VariableAssignmentPattern();
        if (!pattern.TryMatch(ref ctx, out var node) || node is not BinaryOpNode assignNode)
            throw new Exception("Failed to parse assignment: " + source);

        return assignNode;
    }

    [PHPILTest]
    public void Parses_SimpleAssignment()
    {
        var source = "$x = 42;";
        var span = source.AsSpan();

        var node = ParseAssignment(source);

        AssertEqual(TokenKind.AssignEquals, node.Operator);

        var left = (VariableNode)node.Left!;
        AssertEqual("$x", left.Token.TextValue(in span));

        var right = (LiteralNode)node.Right!;
        AssertEqual("42", right.Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_Assignment_WithExpression()
    {
        var source = "$y = $a + $b;";
        var span = source.AsSpan();

        var node = ParseAssignment(source);
        var left = (VariableNode)node.Left!;
        AssertEqual("$y", left.Token.TextValue(in span));

        var bin = (BinaryOpNode)node.Right!;
        AssertEqual("$a", ((VariableNode)bin.Left).Token.TextValue(in span));
        AssertEqual("$b", ((VariableNode)bin.Right).Token.TextValue(in span));
        AssertEqual(TokenKind.Add, bin.Operator);
    }

    [PHPILTest]
    public void Parses_Assignment_WithArrayLiteral()
    {
        var source = "$arr = [1, 2, 3];";
        var span = source.AsSpan();

        var node = ParseAssignment(source);
        var left = (VariableNode)node.Left!;
        AssertEqual("$arr", left.Token.TextValue(in span));

        AssertEqual("ArrayLiteralNode", node.Right!.GetType().Name);
    }

    [PHPILTest]
    public void Parses_NestedExpressionAssignment()
    {
        var source = "$z = ($x * 2) + ($y / 3);";
        var span = source.AsSpan();

        var node = ParseAssignment(source);
        var left = (VariableNode)node.Left!;
        AssertEqual("$z", left.Token.TextValue(in span));

        var top = (BinaryOpNode)node.Right!;
        var leftMul = (BinaryOpNode)top.Left;
        AssertEqual("$x", ((VariableNode)leftMul.Left).Token.TextValue(in span));
        AssertEqual("2", ((LiteralNode)leftMul.Right).Token.TextValue(in span));
        AssertEqual(TokenKind.Multiply, leftMul.Operator);

        var rightDiv = (BinaryOpNode)top.Right;
        AssertEqual("$y", ((VariableNode)rightDiv.Left).Token.TextValue(in span));
        AssertEqual("3", ((LiteralNode)rightDiv.Right).Token.TextValue(in span));
        AssertEqual(TokenKind.DivideBy, rightDiv.Operator);

        AssertEqual(TokenKind.Add, top.Operator);
    }

    [PHPILTest]
    public void Parses_Assignment_WithAnonymousFunction()
    {
        var source = "$fn = function($a, $b) { return $a + $b; };";
        var span = source.AsSpan();

        var node = ParseAssignment(source);
        var left = (VariableNode)node.Left!;
        AssertEqual("$fn", left.Token.TextValue(in span));

        var anon = (AnonymousFunctionNode)node.Right!;
        AssertEqual(2, anon.Params.Count);
        AssertEqual("$a", anon.Params[0].Name.TextValue(in span));
        AssertEqual("$b", anon.Params[1].Name.TextValue(in span));
        AssertEqual(1, anon.Body!.Statements.Count);
    }

    [PHPILTest]
    public void Parses_Assignment_ArrayWithExpressions()
    {
        var source = "$arr = [1, $x + $y, 3];";
        var span = source.AsSpan();

        var node = ParseAssignment(source);
        var left = (VariableNode)node.Left!;
        AssertEqual("$arr", left.Token.TextValue(in span));

        var arr = node.Right!;
        AssertEqual("ArrayLiteralNode", arr.GetType().Name);
        // Additional internal checks can be added if you traverse ArrayLiteralNode.Elements
    }

    [PHPILTest]
    public void Parses_Assignment_AnonymousFunctionReturningArray()
    {
        var source = "$getValues = function() { return [1, 2, 3]; };";
        var span = source.AsSpan();

        var node = ParseAssignment(source);
        var left = (VariableNode)node.Left!;
        AssertEqual("$getValues", left.Token.TextValue(in span));

        var anon = (AnonymousFunctionNode)node.Right!;
        AssertEqual(0, anon.Params.Count);
        AssertEqual(1, anon.Body!.Statements.Count);

        var ret = (ReturnNode)anon.Body.Statements[0];
        AssertEqual("ArrayLiteralNode", ret.Expression!.GetType().Name);
    }
}