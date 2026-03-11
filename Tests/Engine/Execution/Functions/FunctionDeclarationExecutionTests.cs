using PHPIL.Engine.Runtime;

namespace PHPIL.Tests.Engine.Execution.Functions;

public class FunctionDeclarationExecutionTests : BaseTest
{
    private string Execute(string source)
    {
        var span = source.AsSpan();
        Runtime.Execute(in span, "tests");
        return Runtime.GetExecutionResult();
    }

    [PHPILTest]
    public void Function_Void_NoArgs()
    {
        AssertEqual("hello", Execute("<?php function test() { print('hello'); } test();"));
    }

    [PHPILTest]
    public void Function_WithArgs()
    {
        AssertEqual("12", Execute("<?php function test($a, $b) { print($a . $b); } test(1, 2);"));
    }

    [PHPILTest]
    public void Function_WithReturn()
    {
        AssertEqual("3", Execute("<?php function add($a, $b) { return $a + $b; } print(add(1, 2));"));
    }

    [PHPILTest]
    public void Function_LocalVariableScope()
    {
        // Tests that locals within a function don't bleed out or use global context
        AssertEqual("5", Execute("<?php function test() { $a = 5; return $a; } print(test());"));
    }

    [PHPILTest]
    public void Function_Recursive()
    {
        AssertEqual("120", Execute("<?php function factorial($n) { if ($n == 0) { return 1; } return $n * factorial($n - 1); } print(factorial(5));"));
    }

    [PHPILTest]
    public void Function_MultipleCalls()
    {
        AssertEqual("1020", Execute("<?php function printDouble($n) { print($n * 2); } printDouble(5); printDouble(10);"));
    }
}
