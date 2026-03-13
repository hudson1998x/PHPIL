using PHPIL.Engine.Runtime;

namespace PHPIL.Tests.Engine.Execution.Functions;

public class AnonymousFunctionsTests : BaseTest
{
    private string Execute(string source)
    {
        var span = source.AsSpan();
        Runtime.Execute(in span, "tests");
        return Runtime.GetExecutionResult();
    }

    [PHPILTest]
    public void Callback_AnonymousFunction_BoolTrue_PrintsOne()
    {
        const string code = @"<?php 
function execute_callback(Closure $handle) {
    $handle(true);
}
execute_callback(function(bool $success){
    print($success);
});
";
        AssertEqual("1", Execute(code));
    }

    [PHPILTest]
    public void Callback_AnonymousFunction_NoType_PrintsOne()
    {
        const string code = @"<?php 
function execute_callback(Closure $handle) {
    $handle(true);
}
execute_callback(function($success){
    print($success);
});
";
        AssertEqual("1", Execute(code));
    }

    [PHPILTest]
    public void AnonymousFunction_ReturnValue_UsedInExpression()
    {
        const string code = @"<?php 
$sum = (function(int $a, int $b) {
    return $a + $b;
})(3, 4);
print($sum);
";
        AssertEqual("7", Execute(code));
    }

    [PHPILTest]
    public void AnonymousFunction_UseCapture_ModifiesOuterVariable()
    {
        const string code = @"<?php 
$counter = 0;
$increment = function() use (&$counter) {
    $counter++;
};
$increment();
$increment();
print($counter);
";
        AssertEqual("2", Execute(code));
    }
}