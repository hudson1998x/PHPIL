using PHPIL.Engine.Runtime;

namespace PHPIL.Tests.Engine.Execution.Functions;

public class FunctionCallsExecutionTests : BaseTest
{
    private string Execute(string source)
    {
        var span = source.AsSpan();
        Runtime.Execute(in span, "tests");
        return Runtime.GetExecutionResult();
    }

    [PHPILTest]
    public void Print_NumberLiteral()
    {
        AssertEqual("123", Execute("<?php print(123);"));
    }

    [PHPILTest]
    public void Print_EmptyString()
    {
        AssertEqual("", Execute("<?php print('');"));
    }

    [PHPILTest]
    public void Print_MultipleCalls()
    {
        AssertEqual("abc", Execute("<?php print('a'); print('b'); print('c');"));
    }

    [PHPILTest]
    public void Print_ConcatenatedStrings()
    {
        AssertEqual("hello world", Execute("<?php print('hello' . ' ' . 'world');"));
    }

    [PHPILTest]
    public void Print_VariableConcatenation()
    {
        AssertEqual("foobar", Execute("<?php $a = 'foo'; $b = 'bar'; print($a . $b);"));
    }

    [PHPILTest]
    public void Print_VariableAndNumber()
    {
        AssertEqual("value=10", Execute("<?php $x = 10; print('value=' . $x);"));
    }

    [PHPILTest]
    public void Print_MultipleConcats()
    {
        AssertEqual("a1b2", Execute("<?php print('a' . 1 . 'b' . 2);"));
    }

    [PHPILTest]
    public void Print_ExpressionArgument()
    {
        AssertEqual("hello world", Execute("<?php $a = 'hello'; print($a . ' world');"));
    }

    [PHPILTest]
    public void Print_NestedConcatExpressions()
    {
        AssertEqual("abc", Execute("<?php $a = 'a'; $b = 'b'; print(($a . $b) . 'c');"));
    }

    [PHPILTest]
    public void Print_VariableOnly()
    {
        AssertEqual("hello", Execute("<?php $msg = 'hello'; print($msg);"));
    }

    [PHPILTest]
    public void Print_ExpressionEvaluation()
    {
        AssertEqual("hi there", Execute("<?php $a = 'hi'; print($a . ' there');"));
    }

    [PHPILTest]
    public void Print_AfterVariableChange()
    {
        AssertEqual("b", Execute("<?php $x = 'a'; $x = 'b'; print($x);"));
    }

    [PHPILTest]
    public void Print_ConcatNumbers()
    {
        AssertEqual("a12", Execute("<?php\nprint(\"a\" . 1 . 2);"));
    }
    
    [PHPILTest]
    public void Print_AssignmentInsideConcat()
    {
        AssertEqual("xy", Execute("<?php $a = 'x'; print($a . ($a = 'y'));"));
    }
}