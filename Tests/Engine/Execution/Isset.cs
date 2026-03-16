using PHPIL.Engine.Runtime;

namespace PHPIL.Tests.Engine.Execution;

public class IssetExecutionTests : BaseTest
{
    private string Execute(string source)
    {
        var span = source.AsSpan();
        Runtime.Execute(in span, "tests");
        return Runtime.GetExecutionResult();
    }

    [PHPILTest]
    public void Isset_With_Set_Variable()
    {
        ResetTestState();
        var result = Execute("<?php $x = 5; if (isset($x)) { print('true'); }");
        AssertEqual("true", result);
    }

    [PHPILTest]
    public void Isset_With_Undefined_Variable()
    {
        ResetTestState();
        var result = Execute("<?php if (isset($undefined)) { print('true'); } else { print('false'); }");
        AssertEqual("false", result);
    }

    [PHPILTest]
    public void Isset_With_Null_Value()
    {
        ResetTestState();
        var result = Execute("<?php $x = null; if (isset($x)) { print('true'); } else { print('false'); }");
        AssertEqual("false", result);
    }

    [PHPILTest]
    public void Isset_With_Array_Element_Exists()
    {
        ResetTestState();
        var result = Execute("<?php $arr = array(1, 2, 3); if (isset($arr[0])) { print('true'); } else { print('false'); }");
        AssertEqual("true", result);
    }

    [PHPILTest]
    public void Isset_With_Array_Element_Missing()
    {
        ResetTestState();
        var result = Execute("<?php $arr = array(1, 2, 3); if (isset($arr[10])) { print('true'); } else { print('false'); }");
        AssertEqual("false", result);
    }

    [PHPILTest]
    public void Isset_With_Multiple_Variables_All_Set()
    {
        ResetTestState();
        var result = Execute("<?php $a = 1; $b = 2; if (isset($a, $b)) { print('true'); } else { print('false'); }");
        AssertEqual("true", result);
    }

    [PHPILTest]
    public void Isset_With_Multiple_Variables_One_Unset()
    {
        ResetTestState();
        var result = Execute("<?php $a = 1; if (isset($a, $b)) { print('true'); } else { print('false'); }");
        AssertEqual("false", result);
    }

    [PHPILTest]
    public void Isset_With_Multiple_Array_Elements()
    {
        ResetTestState();
        var result = Execute("<?php $arr = array(1, 2, 3); if (isset($arr[0], $arr[1])) { print('true'); } else { print('false'); }");
        AssertEqual("true", result);
    }

    [PHPILTest]
    public void Isset_With_Multiple_Mixed_Elements()
    {
        ResetTestState();
        var result = Execute("<?php $x = 5; $arr = array(1, 2); if (isset($x, $arr[0])) { print('true'); } else { print('false'); }");
        AssertEqual("true", result);
    }

    [PHPILTest]
    public void Isset_String_Value()
    {
        ResetTestState();
        var result = Execute("<?php $x = 'hello'; if (isset($x)) { print('true'); } else { print('false'); }");
        AssertEqual("true", result);
    }

    [PHPILTest]
    public void Isset_Empty_String()
    {
        ResetTestState();
        var result = Execute("<?php $x = ''; if (isset($x)) { print('true'); } else { print('false'); }");
        AssertEqual("true", result);
    }

    [PHPILTest]
    public void Isset_Zero_Value()
    {
        ResetTestState();
        var result = Execute("<?php $x = 0; if (isset($x)) { print('true'); } else { print('false'); }");
        AssertEqual("true", result);
    }

    [PHPILTest]
    public void Isset_Array_Index_With_Null()
    {
        ResetTestState();
        var result = Execute("<?php $arr = array(1 => 'a', 2 => null); if (isset($arr[2])) { print('true'); } else { print('false'); }");
        AssertEqual("false", result);
    }

    [PHPILTest]
    public void Isset_With_String_Key()
    {
        ResetTestState();
        var result = Execute("<?php $arr = array('key' => 'value'); if (isset($arr['key'])) { print('true'); } else { print('false'); }");
        AssertEqual("true", result);
    }

    [PHPILTest]
    public void Isset_Return_Value()
    {
        ResetTestState();
        var result = Execute("<?php $x = 5; $result = isset($x); print($result ? 'true' : 'false');");
        AssertEqual("true", result);
    }

    [PHPILTest]
    public void Isset_Return_Value_False()
    {
        ResetTestState();
        var result = Execute("<?php $result = isset($undefined); print($result ? 'true' : 'false');");
        AssertEqual("false", result);
    }
}
