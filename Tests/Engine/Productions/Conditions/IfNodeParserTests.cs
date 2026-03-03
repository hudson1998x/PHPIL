using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;

namespace PHPIL.Tests.Engine;

public class IfElseParserTests : BaseTest
{
    // Helper: parse source into BlockNode
    private BlockNode ParseBlock(string source)
    {
        var tokens = Lexer.ParseSpan(source.AsSpan()).ToArray();
        var ast = Parser.Parse(in tokens, source.AsSpan());
        if (ast is not BlockNode block)
            throw new Exception("Expected root to be BlockNode");
        return block;
    }

    // Helper: assert statement is a function call
    private void AssertFunctionCall(SyntaxNode node)
    {
        if (node is not FunctionCallNode)
            throw new Exception($"Expected FunctionCallNode, got {node.GetType().Name}");
    }

    [PHPILTest]
    public void Parse_SimpleIf()
    {
        var source = "if ($x > 0) { print($x); }";
        var block = ParseBlock(source);

        var ifNode = (IfNode)block.Statements[0];
        AssertEqual(0, ifNode.ElseIfs.Count);
        AssertEqual(null, ifNode.ElseNode);
        AssertEqual(true, ifNode.Body != null);
        AssertFunctionCall(ifNode.Body!.Statements[0]);
    }

    [PHPILTest]
    public void Parse_IfElse()
    {
        var source = @"
            if ($x > 0) { print(""pos""); }
            else { print(""nonpos""); }
        ";
        var block = ParseBlock(source);
        var ifNode = (IfNode)block.Statements[0];

        // Body
        AssertEqual(true, ifNode.Body != null);
        AssertFunctionCall(ifNode.Body!.Statements[0]);

        // Else
        var elseNode = (ElseNode)ifNode.ElseNode!;
        AssertEqual(true, elseNode.Body != null);
        AssertFunctionCall(elseNode.Body!.Statements[0]);
    }

    [PHPILTest]
    public void Parse_IfElseIfElse()
    {
        var source = @"
            if ($x > 10) { print(""gt10""); }
            else if ($x > 5) { print(""gt5""); }
            else { print(""le5""); }
        ";
        var block = ParseBlock(source);
        var ifNode = (IfNode)block.Statements[0];

        AssertEqual(1, ifNode.ElseIfs.Count);
        var elseifNode = ifNode.ElseIfs[0];
        AssertEqual(true, elseifNode.Body != null);
        AssertFunctionCall(elseifNode.Body!.Statements[0]);

        var finalElse = (ElseNode)ifNode.ElseNode!;
        AssertFunctionCall(finalElse.Body!.Statements[0]);
    }

    [PHPILTest]
    public void Parse_IfElseIfElseIfElse()
    {
        var source = @"
            if ($x == 1) { print(""1""); }
            else if ($x == 2) { print(""2""); }
            else if ($x == 3) { print(""3""); }
            else { print(""other""); }
        ";
        var block = ParseBlock(source);
        var ifNode = (IfNode)block.Statements[0];

        AssertEqual(2, ifNode.ElseIfs.Count);

        foreach (var elseifNode in ifNode.ElseIfs)
        {
            AssertEqual(true, elseifNode.Body != null);
            AssertFunctionCall(elseifNode.Body!.Statements[0]);
        }

        var finalElse = (ElseNode)ifNode.ElseNode!;
        AssertFunctionCall(finalElse.Body!.Statements[0]);
    }

    [PHPILTest]
    public void Parse_IfWithFunctionCallsInBody()
    {
        var source = @"
            if ($x > 0) {
                doSomething($x);
                doSomethingElse();
            }
        ";
        var block = ParseBlock(source);
        var ifNode = (IfNode)block.Statements[0];

        var body = ifNode.Body!;
        AssertEqual(2, body.Statements.Count);
        foreach (var stmt in body.Statements)
            AssertFunctionCall(stmt);
    }
}