using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;
using PHPIL.Engine.SyntaxTree.Structure;

namespace PHPIL.Tests.Engine;

public class ArrayLiteralPatternTests : BaseTest
{
    private ArrayLiteralNode ParseArray(string source)
    {
        var tokens = Lexer.ParseSpan(source.AsSpan())
            .Where(t => t.Kind is not (TokenKind.Whitespace or TokenKind.NewLine))
            .ToArray();

        var ctx = new ParserContext(tokens.AsSpan(), source.AsSpan());

        var pattern = new ArrayLiteralPattern();
        if (!pattern.TryMatch(ref ctx, out var node) || node is not ArrayLiteralNode array)
            throw new Exception("Failed to parse array: " + source);

        return array;
    }

    [PHPILTest]
    public void Parses_EmptyShortArray()
    {
        var source = "[]";
        var array = ParseArray(source);
        AssertEqual(0, array.Items.Count);
    }

    [PHPILTest]
    public void Parses_EmptyLongArray()
    {
        var source = "array()";
        var array = ParseArray(source);
        AssertEqual(0, array.Items.Count);
    }

    [PHPILTest]
    public void Parses_ShortArray_WithValues()
    {
        var source = "[1, 2, 3]";
        var span = source.AsSpan();
        var array = ParseArray(source);

        AssertEqual(3, array.Items.Count);
        AssertEqual("1", ((LiteralNode)array.Items[0].Value).Token.TextValue(in span));
        AssertEqual("2", ((LiteralNode)array.Items[1].Value).Token.TextValue(in span));
        AssertEqual("3", ((LiteralNode)array.Items[2].Value).Token.TextValue(in span));
        AssertEqual(null, array.Items[0].Key);
    }

    [PHPILTest]
    public void Parses_LongArray_WithKeys()
    {
        var source = "array('a' => 1, 'b' => 2)";
        var span = source.AsSpan();
        var array = ParseArray(source);

        AssertEqual(2, array.Items.Count);
        AssertEqual("'a'", ((LiteralNode)array.Items[0].Key!).Token.TextValue(in span));
        AssertEqual("1", ((LiteralNode)array.Items[0].Value).Token.TextValue(in span));

        AssertEqual("'b'", ((LiteralNode)array.Items[1].Key!).Token.TextValue(in span));
        AssertEqual("2", ((LiteralNode)array.Items[1].Value).Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_Array_MixedKeysAndValues()
    {
        var source = "[10, 'x' => 42, 20]";
        var span = source.AsSpan();
        var array = ParseArray(source);

        AssertEqual(3, array.Items.Count);
        AssertEqual(null, array.Items[0].Key);
        AssertEqual("10", ((LiteralNode)array.Items[0].Value).Token.TextValue(in span));

        AssertEqual("'x'", ((LiteralNode)array.Items[1].Key!).Token.TextValue(in span));
        AssertEqual("42", ((LiteralNode)array.Items[1].Value).Token.TextValue(in span));

        AssertEqual(null, array.Items[2].Key);
        AssertEqual("20", ((LiteralNode)array.Items[2].Value).Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_NestedArray()
    {
        var source = "[1, [2, 3], 4]";
        var span = source.AsSpan();
        var array = ParseArray(source);

        AssertEqual(3, array.Items.Count);

        AssertEqual("1", ((LiteralNode)array.Items[0].Value).Token.TextValue(in span));

        var nested = (ArrayLiteralNode)array.Items[1].Value;
        AssertEqual(2, nested.Items.Count);
        AssertEqual("2", ((LiteralNode)nested.Items[0].Value).Token.TextValue(in span));
        AssertEqual("3", ((LiteralNode)nested.Items[1].Value).Token.TextValue(in span));

        AssertEqual("4", ((LiteralNode)array.Items[2].Value).Token.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_Array_WithTrailingComma()
    {
        var source = "[1, 2, 3,]";
        var span = source.AsSpan();
        var array = ParseArray(source);

        AssertEqual(3, array.Items.Count);
        AssertEqual("1", ((LiteralNode)array.Items[0].Value).Token.TextValue(in span));
        AssertEqual("2", ((LiteralNode)array.Items[1].Value).Token.TextValue(in span));
        AssertEqual("3", ((LiteralNode)array.Items[2].Value).Token.TextValue(in span));
    }
}