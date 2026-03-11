using PHPIL.Engine.Runtime;

namespace PHPIL.Tests.Engine.Execution;

public class OperatorExecutionTests : BaseTest
{
    private string Execute(string source)
    {
        var span = source.AsSpan();
        Runtime.Execute(in span, "tests");
        return Runtime.GetExecutionResult();
    }

    [PHPILTest]
    public void Compound_AddAssign()
    {
        var result = Execute("<?php $x = 10; $x += 5; print($x);");
        AssertEqual("15", result);
    }

    [PHPILTest]
    public void Compound_SubtractAssign()
    {
        var result = Execute("<?php $x = 10; $x -= 5; print($x);");
        AssertEqual("5", result);
    }

    [PHPILTest]
    public void Compound_ConcatAppend()
    {
        var result = Execute("<?php $s = 'Hello'; $s .= ' World'; print($s);");
        AssertEqual("Hello World", result);
    }

    [PHPILTest]
    public void Logical_And_ShortCircuit()
    {
        var result = Execute("<?php $x = 0; false && ($x = 1); print($x);");
        AssertEqual("0", result);
    }

    [PHPILTest]
    public void Logical_Or_ShortCircuit()
    {
        var result = Execute("<?php $x = 0; true || ($x = 1); print($x);");
        AssertEqual("0", result);
    }

    [PHPILTest]
    public void Logical_Not()
    {
        var result = Execute("<?php $x = true; print(!$x ? 'yes' : 'no');");
        AssertEqual("no", result);

        result = Execute("<?php $x = false; print(!$x ? 'yes' : 'no');");
        AssertEqual("yes", result);
    }

    [PHPILTest]
    public void Strict_Equality()
    {
        var result = Execute("<?php print(1 === 1 ? 'yes' : 'no');");
        AssertEqual("yes", result);
        
        result = Execute("<?php print(1 === '1' ? 'yes' : 'no');");
        AssertEqual("no", result);
    }

    [PHPILTest]
    public void Prefix_Increment_Decrement()
    {
        var result = Execute("<?php $x = 5; print(++$x); print($x);");
        AssertEqual("66", result);

        result = Execute("<?php $x = 5; print(--$x); print($x);");
        AssertEqual("44", result);
    }

    [PHPILTest]
    public void Postfix_Increment_Decrement()
    {
        var result = Execute("<?php $x = 5; print($x++); print($x);");
        AssertEqual("56", result);

        result = Execute("<?php $x = 5; print($x--); print($x);");
        AssertEqual("54", result);
    }
}
