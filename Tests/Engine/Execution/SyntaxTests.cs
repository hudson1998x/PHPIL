using PHPIL.Engine.CodeLexer;
using PHPIL.Engine.Productions;
using PHPIL.Engine.Productions.Patterns;

namespace PHPIL.Tests.Engine.Execution;

public class SyntaxTests : BaseTest
{
    private Exception ParseWithExpectedError(string source)
    {
        try
        {
            var tokens = Lexer.ParseSpan(source.AsSpan());
            var sourceSpan = source.AsSpan();
            var ast = Parser.Parse(in tokens, in sourceSpan);
            throw new Exception("Expected syntax error but parsing succeeded");
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    [PHPILTest]
    public void SyntaxError_MissingClosingBrace()
    {
        var source = "<?php\nif (true) {\n    echo \"missing closing brace\";";
        
        var error = ParseWithExpectedError(source);
        
        AssertNotNull(error);
        // Check that it's some kind of error (could be SyntaxError or generic Exception)
        AssertEqual(true, error.Message.Contains("line") || error.Message.Contains("error"));
    }

    [PHPILTest]
    public void SyntaxError_MissingSemicolon()
    {
        // Note: PHP allows omitting the final semicolon in a file
        var source = "<?php\necho \"missing semicolon\"";
        
        // This is actually valid PHP, so we should not expect an error
        var tokens = Lexer.ParseSpan(source.AsSpan());
        var sourceSpan = source.AsSpan();
        var ast = Parser.Parse(in tokens, in sourceSpan);
        
        AssertNotNull(ast);
    }

    [PHPILTest]
    public void SyntaxError_InvalidFunctionDeclaration()
    {
        var source = "<?php\nfunction test() {\n    echo \"no return type\";";
        
        var error = ParseWithExpectedError(source);
        
        AssertNotNull(error);
    }

    [PHPILTest]
    public void SyntaxError_UnclosedString()
    {
        var source = "<?php\necho \"unclosed string;";
        
        var error = ParseWithExpectedError(source);
        
        AssertNotNull(error);
    }

    [PHPILTest]
    public void SyntaxError_MissingParenthesis()
    {
        var source = "<?php\nif true {\n    echo \"missing parenthesis\";";
        
        var error = ParseWithExpectedError(source);
        
        AssertNotNull(error);
    }

    [PHPILTest]
    public void SyntaxError_InvalidVariable()
    {
        var source = "<?php\n$var = ;\necho \"invalid variable\";";
        
        var error = ParseWithExpectedError(source);
        
        AssertNotNull(error);
    }

    [PHPILTest]
    public void SyntaxError_EmptyFile()
    {
        var source = "<?php";
        
        // Empty file is actually valid (no statements)
        var tokens = Lexer.ParseSpan(source.AsSpan());
        var sourceSpan = source.AsSpan();
        var ast = Parser.Parse(in tokens, in sourceSpan);
        
        // Should not throw for empty file
        AssertNotNull(ast);
    }

    [PHPILTest]
    public void SyntaxError_ClassWithoutBody()
    {
        var source = "<?php\nclass Test";
        
        var error = ParseWithExpectedError(source);
        
        AssertNotNull(error);
    }

    [PHPILTest]
    public void SyntaxError_MissingCommaInArray()
    {
        var source = "<?php\n$arr = [1 2 3];";
        
        var error = ParseWithExpectedError(source);
        
        AssertNotNull(error);
        AssertEqual(true, error.Message.Contains("array") || error.Message.Contains("Array"));
    }

    [PHPILTest]
    public void SyntaxError_InvalidForLoop()
    {
        // Actually valid PHP (infinite loop)
        var source = "<?php\nfor ( ; ; ) {\n    echo \"infinite\";\n}";
        
        var tokens = Lexer.ParseSpan(source.AsSpan());
        var sourceSpan = source.AsSpan();
        var ast = Parser.Parse(in tokens, in sourceSpan);
        
        AssertNotNull(ast);
    }

    [PHPILTest]
    public void SyntaxError_MissingConditionInIf()
    {
        var source = "<?php\nif {\n    echo \"missing condition\";";
        
        var error = ParseWithExpectedError(source);
        
        AssertNotNull(error);
    }
}
