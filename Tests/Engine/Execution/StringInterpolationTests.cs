using PHPIL.Engine.Runtime;

namespace PHPIL.Tests.Engine.Execution;

public class StringInterpolationTests : BaseTest
{
    private string Execute(string source)
    {
        var span = source.AsSpan();
        Runtime.Execute(in span, "tests");
        return Runtime.GetExecutionResult();
    }

    [PHPILTest]
    public void Simple_Variable_Interpolation()
    {
        var result = Execute("<?php $username = 'John'; print(\"Hello $username\");");
        AssertEqual("Hello John", result);
    }

    [PHPILTest]
    public void Curly_Brace_Variable_Interpolation()
    {
        var result = Execute("<?php $username = 'John'; print(\"Hello {$username}\");");
        AssertEqual("Hello John", result);
    }

    [PHPILTest]
    public void Multiple_Interpolations()
    {
        var result = Execute("<?php $greeting = 'Hello'; $name = 'John'; print(\"$greeting {$name}!\");");
        AssertEqual("Hello John!", result);
    }

    [PHPILTest]
    public void Interpolation_With_Numbers()
    {
        var result = Execute("<?php $count = 42; print(\"Count: $count\");");
        AssertEqual("Count: 42", result);
    }

    // Object operator tests are omitted for now, 
    // but the parser support is there.
    // TODO: Add tests for $user->name interpolation once classes are implemented.
}
