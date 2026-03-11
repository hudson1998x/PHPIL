using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure.Loops;

namespace PHPIL.Tests.Engine;

public class ForeachExpressionParserTests : BaseTest
{
    private ForeachNode ParseForeach(string source)
    {
        var tokens = Lexer.ParseSpan(source.AsSpan())
            .Where(t => t.Kind is not (TokenKind.Whitespace or TokenKind.NewLine))
            .ToArray();

        var ctx = new ParserContext(tokens.AsSpan(), source.AsSpan());

        var pattern = new ForeachExpressionPattern();
        if (!pattern.TryMatch(ref ctx, out var node) || node is not ForeachNode foreachNode)
            throw new Exception("Failed to parse foreach: " + source);

        return foreachNode;
    }

    [PHPILTest]
    public void Parses_SimpleForeach()
    {
        var source = "foreach($items as $item) {}";
        var span = source.AsSpan();

        var node = ParseForeach(source);

        AssertEqual(typeof(VariableNode), node.Iterable!.GetType());
        AssertEqual("$items", ((VariableNode)node.Iterable!).Token.TextValue(in span));

        AssertEqual(null, node.Key);

        AssertEqual("$item", node.Value!.Token.TextValue(in span));
        AssertEqual(typeof(BlockNode), node.Body!.GetType());
    }

    [PHPILTest]
    public void Parses_Foreach_WithKey()
    {
        var source = "foreach($map as $key => $val) {}";
        var span = source.AsSpan();

        var node = ParseForeach(source);

        AssertEqual("$map", ((VariableNode)node.Iterable!).Token.TextValue(in span));
        AssertEqual("$key", node.Key!.Token.TextValue(in span));
        AssertEqual("$val", node.Value!.Token.TextValue(in span));
        AssertEqual(typeof(BlockNode), node.Body!.GetType());
    }

    [PHPILTest]
    public void Parses_Foreach_Nested()
    {
        var source = @"
        foreach($maps as $m) {
            foreach($m as $k => $v) {}
        }";
        var span = source.AsSpan();

        var outer = ParseForeach(source);

        AssertEqual("$maps", ((VariableNode)outer.Iterable!).Token.TextValue(in span));
        AssertEqual(null, outer.Key);
        AssertEqual("$m", outer.Value!.Token.TextValue(in span));
        AssertEqual(typeof(BlockNode), outer.Body!.GetType());

        // Inner foreach in outer body
        var innerStmt = outer.Body!.Statements[0];
        AssertEqual(typeof(ForeachNode), innerStmt.GetType());

        var inner = (ForeachNode)innerStmt;
        AssertEqual("$m", ((VariableNode)inner.Iterable!).Token.TextValue(in span));
        AssertEqual("$k", inner.Key!.Token.TextValue(in span));
        AssertEqual("$v", inner.Value!.Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_Foreach_WithFunctionCallIterable()
    {
        var source = "foreach(getItems() as $i) {}";
        var span = source.AsSpan();

        var node = ParseForeach(source);

        if (node.Iterable is FunctionCallNode call)
        {
            AssertEqual("getItems", GetQualifiedName(call.Callee, in span));
        }
        else
        {
            throw new Exception("Iterable is not a FunctionCallNode");
        }

        AssertEqual(null, node.Key);
        AssertEqual("$i", node.Value!.Token.TextValue(in span));
    }

}