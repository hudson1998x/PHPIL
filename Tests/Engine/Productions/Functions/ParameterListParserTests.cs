using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Tests.Engine;

public class ParameterListParserTests : BaseTest
{
    // Helper to parse a parameter list
    private ParameterListNode ParseParameters(string source)
    {
        var tokens = Lexer.ParseSpan(source.AsSpan())
            .Where(t => t.Kind is not (TokenKind.Whitespace or TokenKind.NewLine))
            .ToArray();

        var ctx = new ParserContext(tokens.AsSpan(), source.AsSpan());

        var pattern = new ParameterListPattern();
        if (!pattern.TryMatch(ref ctx, out var node) || node is not ParameterListNode paramList)
            throw new Exception("Failed to parse parameter list: " + source);

        return paramList;
    }

    [PHPILTest]
    public void Parses_EmptyParameterList()
    {
        var source = "()";

        var list = ParseParameters(source);
        AssertEqual(0, list.Parameters.Count);
    }

    [PHPILTest]
    public void Parses_SingleParameter_NoTypeNoDefault()
    {
        var source = "($a)";
        var span = source.AsSpan();

        var list = ParseParameters(source);
        AssertEqual(1, list.Parameters.Count);

        var param = list.Parameters[0];
        AssertEqual("$a", param.Name.TextValue(in span));
        AssertEqual(false, param.TypeHint.HasValue);
        AssertEqual(null, param.DefaultValue);
    }

    [PHPILTest]
    public void Parses_MultipleParameters_NoTypeNoDefault()
    {
        var source = "($x, $y, $z)";
        var span = source.AsSpan();

        var list = ParseParameters(source);
        AssertEqual(3, list.Parameters.Count);

        AssertEqual("$x", list.Parameters[0].Name.TextValue(in span));
        AssertEqual("$y", list.Parameters[1].Name.TextValue(in span));
        AssertEqual("$z", list.Parameters[2].Name.TextValue(in span));
    }

    [PHPILTest]
    public void Parses_Parameter_WithTypeHint()
    {
        var source = "(int $a, string $b)";
        var span = source.AsSpan();

        var list = ParseParameters(source);
        AssertEqual(2, list.Parameters.Count);

        var p0 = list.Parameters[0];
        AssertEqual("$a", p0.Name.TextValue(in span));
        AssertEqual(true, p0.TypeHint.HasValue);
        AssertEqual("int", p0.TypeHint!.Value.TextValue(in span));
        AssertEqual(null, p0.DefaultValue);

        var p1 = list.Parameters[1];
        AssertEqual("$b", p1.Name.TextValue(in span));
        AssertEqual(true, p1.TypeHint.HasValue);
        AssertEqual("string", p1.TypeHint!.Value.TextValue(in span));
        AssertEqual(null, p1.DefaultValue);
    }

    [PHPILTest]
    public void Parses_Parameter_WithDefaultValue()
    {
        var source = "($a = 1, $b = 'hello')";
        var span = source.AsSpan();

        var list = ParseParameters(source);
        AssertEqual(2, list.Parameters.Count);

        var p0 = list.Parameters[0];
        AssertEqual("$a", p0.Name.TextValue(in span));
        AssertEqual(false, p0.TypeHint.HasValue);
        AssertEqual(typeof(LiteralNode), p0.DefaultValue!.GetType());

        var p1 = list.Parameters[1];
        AssertEqual("$b", p1.Name.TextValue(in span));
        AssertEqual(false, p1.TypeHint.HasValue);
        AssertEqual(typeof(LiteralNode), p1.DefaultValue!.GetType());
    }

    [PHPILTest]
    public void Parses_Parameter_WithTypeHintAndDefault()
    {
        var source = "(float $x = 0.5, bool $y = true)";
        var span = source.AsSpan();

        var list = ParseParameters(source);
        AssertEqual(2, list.Parameters.Count);

        var p0 = list.Parameters[0];
        AssertEqual("$x", p0.Name.TextValue(in span));
        AssertEqual(true, p0.TypeHint.HasValue);
        AssertEqual("float", p0.TypeHint!.Value.TextValue(in span));
        AssertEqual(typeof(LiteralNode), p0.DefaultValue!.GetType());

        var p1 = list.Parameters[1];
        AssertEqual("$y", p1.Name.TextValue(in span));
        AssertEqual(true, p1.TypeHint.HasValue);
        AssertEqual("bool", p1.TypeHint!.Value.TextValue(in span));
        AssertEqual(typeof(LiteralNode), p1.DefaultValue!.GetType());
    }

    [PHPILTest]
    public void Parses_ParameterList_WithTrailingComma()
    {
        var source = "($a, $b,)";
        var span = source.AsSpan();

        var list = ParseParameters(source);
        AssertEqual(2, list.Parameters.Count);

        AssertEqual("$a", list.Parameters[0].Name.TextValue(in span));
        AssertEqual("$b", list.Parameters[1].Name.TextValue(in span));
    }
}