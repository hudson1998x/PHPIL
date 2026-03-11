using PHPIL.Engine.Runtime;

namespace PHPIL.Tests.Engine.Execution.Arrays;

public class ArrayExecutionTests : BaseTest
{
    private string Execute(string source)
    {
        var span = source.AsSpan();
        Runtime.Execute(in span, "tests");
        return Runtime.GetExecutionResult();
    }

    [PHPILTest]
    public void Array_BasicCreate()
    {
        var result = Execute("<?php $arr = [1, 2, 3]; print('ok');");
        AssertEqual("ok", result);
    }

    [PHPILTest]
    public void Array_Empty()
    {
        var result = Execute("<?php $arr = []; print('ok');");
        AssertEqual("ok", result);
    }

    [PHPILTest]
    public void Array_Associative()
    {
        var result = Execute("<?php $arr = ['a' => 1, 'b' => 2]; print('ok');");
        AssertEqual("ok", result);
    }

    [PHPILTest]
    public void Array_MixedKeys()
    {
        var result = Execute("<?php $arr = [0 => 'a', 'b' => 'c', 1 => 'd']; print('ok');");
        AssertEqual("ok", result);
    }

    [PHPILTest]
    public void Array_VariableKey()
    {
        var result = Execute("<?php $key = 'x'; $arr = [$key => 1]; print('ok');");
        AssertEqual("ok", result);
    }

    [PHPILTest]
    public void Array_Nested()
    {
        var result = Execute("<?php $arr = [1, [2, 3], 4]; print('ok');");
        AssertEqual("ok", result);
    }

    [PHPILTest]
    public void Array_Accessor_GetSet()
    {
        var result = Execute("<?php $arr = []; $arr['test'] = 'success'; print($arr['test']);");
        AssertEqual("success", result);
    }

    [PHPILTest]
    public void Array_Accessor_Append()
    {
        var result = Execute("<?php $arr = []; $arr[] = 1; print($arr[0]);");
        AssertEqual("1", result);
    }
}
