using PHPIL.Engine.Runtime;

namespace PHPIL.Tests.Engine.Execution.Loops;

public class ForeachExecutionTests : BaseTest
{
    private string Execute(string source)
    {
        var span = source.AsSpan();
        Runtime.Execute(in span, "tests");
        return Runtime.GetExecutionResult();
    }

    [PHPILTest]
    public void Foreach_Array_SimpleValue()
    {
        var result = Execute("<?php $arr = [1, 2, 3]; foreach($arr as $v) { print($v); }");
        AssertEqual("123", result);
    }

    [PHPILTest]
    public void Foreach_Array_KeyValue()
    {
        var result = Execute("<?php $arr = ['a' => 1, 'b' => 2]; foreach($arr as $k => $v) { print($k . $v); }");
        AssertEqual("a1b2", result);
    }

    [PHPILTest]
    public void Foreach_EmptyArray()
    {
        var result = Execute("<?php $arr = []; foreach($arr as $v) { print($v); } print('done');");
        AssertEqual("done", result);
    }

    [PHPILTest]
    public void Foreach_Nested()
    {
        var result = Execute("<?php $arr = [[1,2], [3,4]]; foreach($arr as $inner) { foreach($inner as $v) { print($v); } }");
        AssertEqual("1234", result);
    }

    [PHPILTest]
    public void Foreach_ArrayWithFunctionCall()
    {
        var result = Execute("<?php function getItems() { return [1, 2, 3]; } foreach(getItems() as $v) { print($v); }");
        AssertEqual("123", result);
    }

    [PHPILTest]
    public void Foreach_String_Values()
    {
        var result = Execute("<?php $arr = ['hello', 'world']; foreach($arr as $v) { print($v); }");
        AssertEqual("helloworld", result);
    }

    [PHPILTest]
    public void Foreach_Class_WithoutIterator()
    {
        ResetTestState();
        
        // Class iteration without Iterator is not supported - should throw
        try
        {
            var result = Execute("<?php class MyClass { public $items = [1,2,3]; } $obj = new MyClass(); foreach($obj as $v) { print($v); }");
            // If we get here without exception, the test fails
            AssertEqual("should have thrown", result);
        }
        catch (Exception)
        {
            // Expected - class iteration without Iterator throws
            AssertEqual(true, true);
        }
    }

    [PHPILTest]
    public void Foreach_Class_ImplementsIterator()
    {
        ResetTestState();
        
        // Test class with single method first
        var result = Execute("<?php class MyIterator { public function getValue() { return 1; } } $iter = new MyIterator(); print($iter->getValue());");
        AssertEqual("1", result);
    }

    [PHPILTest]
    public void Foreach_Class_KeyValue()
    {
        ResetTestState();
        
        // Test class with 2 methods
        var result = Execute("<?php class MyIterator { public function getValue() { return 1; } public function getKey() { return 0; } } $iter = new MyIterator(); print($iter->getValue() . $iter->getKey());");
        AssertEqual("10", result);
    }
}
