using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.Productions.Patterns;
using PHPIL.Engine.SyntaxTree;

namespace PHPIL.Tests.Engine;

public class FunctionDeclarationParserTests : BaseTest
{
    // Helper to parse a function declaration
    private FunctionNode ParseFunction(string source)
    {
        var tokens = Lexer.ParseSpan(source.AsSpan())
            .Where(t => t.Kind is not (TokenKind.Whitespace or TokenKind.NewLine))
            .ToArray();

        var ctx = new ParserContext(tokens.AsSpan(), source.AsSpan());

        var pattern = new FunctionDeclarationPattern();
        if (!pattern.TryMatch(ref ctx, out var node) || node is not FunctionNode fn)
            throw new Exception("Failed to parse function: " + source);

        return fn;
    }

    [PHPILTest]
    public void Parses_EmptyFunction_NoParams_NoReturn()
    {
        var source = "function foo() {}";
        var span = source.AsSpan();

        var fn = ParseFunction(source);
        AssertEqual("foo", fn.Name.TextValue(in span));
        AssertEqual(0, fn.Params.Count);
        AssertEqual(false, fn.ReturnType.HasValue);
        AssertEqual(typeof(BlockNode), fn.Body!.GetType());
    }

    [PHPILTest]
    public void Parses_Function_WithParameters()
    {
        var source = "function bar($a, $b) {}";
        var span = source.AsSpan();

        var fn = ParseFunction(source);
        AssertEqual("bar", fn.Name.TextValue(in span));
        AssertEqual(2, fn.Params.Count);

        AssertEqual("$a", fn.Params[0].Name.TextValue(in span));
        AssertEqual("$b", fn.Params[1].Name.TextValue(in span));
        AssertEqual(false, fn.ReturnType.HasValue);
        AssertEqual(typeof(BlockNode), fn.Body!.GetType());
    }

    [PHPILTest]
    public void Parses_Function_WithReturnType()
    {
        var source = "function baz(): int {}";
        var span = source.AsSpan();

        var fn = ParseFunction(source);
        AssertEqual("baz", fn.Name.TextValue(in span));
        AssertEqual(0, fn.Params.Count);

        AssertEqual(true, fn.ReturnType.HasValue);
        AssertEqual("int", fn.ReturnType!.Value.TextValue(in span));
        AssertEqual(typeof(BlockNode), fn.Body!.GetType());
    }

    [PHPILTest]
    public void Parses_Function_WithParametersAndReturnType()
    {
        var source = "function sum($x, $y): float { return $x + $y; }";
        var span = source.AsSpan();

        var fn = ParseFunction(source);
        AssertEqual("sum", fn.Name.TextValue(in span));
        AssertEqual(2, fn.Params.Count);
        AssertEqual("$x", fn.Params[0].Name.TextValue(in span));
        AssertEqual("$y", fn.Params[1].Name.TextValue(in span));

        AssertEqual(true, fn.ReturnType.HasValue);
        AssertEqual("float", fn.ReturnType!.Value.TextValue(in span));
        AssertEqual(typeof(BlockNode), fn.Body!.GetType());
    }

    [PHPILTest]
    public void Parses_Function_WithNoParametersAndReturnType()
    {
        var source = "function getTime(): string {}";
        var span = source.AsSpan();

        var fn = ParseFunction(source);
        AssertEqual("getTime", fn.Name.TextValue(in span));
        AssertEqual(0, fn.Params.Count);

        AssertEqual(true, fn.ReturnType.HasValue);
        AssertEqual("string", fn.ReturnType!.Value.TextValue(in span));
        AssertEqual(typeof(BlockNode), fn.Body!.GetType());
    }
}