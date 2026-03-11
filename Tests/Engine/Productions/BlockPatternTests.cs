using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Tests.Engine; // Assuming BaseTest exists

namespace PHPIL.Tests.Engine;

public class BlockPatternTests : BaseTest
{
    private BlockNode ParseBlock(string source)
    {
        var tokens = Lexer.ParseSpan(source.AsSpan())
            .Where(t => t.Kind is not (TokenKind.Whitespace or TokenKind.NewLine))
            .ToArray();

        var ctx = new ParserContext(tokens.AsSpan(), source.AsSpan());

        var pattern = new BlockPattern();
        if (!pattern.TryMatch(ref ctx, out var node) || node is not BlockNode block)
            throw new Exception("Failed to parse block: " + source);

        return block;
    }

    [PHPILTest]
    public void Parses_EmptyBlock()
    {
        var source = "{}";
        var block = ParseBlock(source);
        AssertEqual(0, block.Statements.Count);
    }

    [PHPILTest]
    public void Parses_Block_WithSingleAssignment()
    {
        var source = "{$x = 42;}";
        var span = source.AsSpan();
        var block = ParseBlock(source);

        AssertEqual(1, block.Statements.Count);
        var assign = block.Statements[0] as BinaryOpNode;
        AssertNotNull(assign);
        AssertEqual("$x", ((VariableNode)assign!.Left!).Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_Block_WithMultipleStatements()
    {
        var source = "{$a = 1; $b = 2;}";
        var span = source.AsSpan();
        var block = ParseBlock(source);

        AssertEqual(2, block.Statements.Count);

        var first = (BinaryOpNode)block.Statements[0];
        AssertEqual("$a", ((VariableNode)first.Left!).Token.TextValue(in span));

        var second = (BinaryOpNode)block.Statements[1];
        AssertEqual("$b", ((VariableNode)second.Left!).Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_NestedBlock()
    {
        var source = "{$x = 1; {$y = 2;}; $z = 3;}";
        var span = source.AsSpan();
        var block = ParseBlock(source);

        AssertEqual(3, block.Statements.Count); 
    }

    [PHPILTest]
    public void Parses_Block_WithFunctionCall()
    {
        var source = "{print($x);}";
        var span = source.AsSpan();
        var block = ParseBlock(source);

        AssertEqual(1, block.Statements.Count);
        var call = block.Statements[0] as FunctionCallNode;
        AssertNotNull(call);
        AssertEqual("print", ((IdentifierNode)call!.Callee!).Token.TextValue(in span));
        AssertEqual(1, call.Args.Count);
        AssertEqual("$x", ((VariableNode)call.Args[0]).Token.TextValue(in span));
    }
}