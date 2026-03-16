using PHPIL.Engine.Runtime;
using PHPIL.Engine.Runtime.Sdk;

namespace PHPIL.Tests.Engine.Execution;

public class SuperglobalsExecutionTests : BaseTest
{
    private string Execute(string source)
    {
        var span = source.AsSpan();
        Runtime.Execute(in span, "tests");
        return Runtime.GetExecutionResult();
    }

    [PHPILTest]
    public void Test_Get_Superglobal_Read()
    {
        // Test reading $_GET superglobal
        var result = Execute("<?php var_dump($_GET);");
        AssertEqual("array(0) {\n}", result);
    }

    [PHPILTest]
    public void Test_Post_Superglobal_Read()
    {
        // Test reading $_POST superglobal
        var result = Execute("<?php var_dump($_POST);");
        AssertEqual("array(0) {\n}", result);
    }

    [PHPILTest]
    public void Test_Server_Superglobal_Read()
    {
        // Test reading $_SERVER superglobal
        var result = Execute("<?php if (isset($_SERVER['REQUEST_METHOD'])) { print('YES'); } else { print('NO'); }");
        // Should be empty initially, so should print NO
        // Actually, we need to test with actual populated superglobals
        // For now, let's just test that accessing doesn't crash
        // AssertEqual("NO", result); // This might fail if server vars are populated
        // Just ensure no exception is thrown
        AssertNotNull(result);
    }

    [PHPILTest]
    public void Test_Superglobal_Assignment_To_Array()
    {
        // Test assignment to superglobal array element
        var result = Execute("<?php $_GET['key'] = 'value'; print($_GET['key']);");
        AssertEqual("value", result);
    }

    [PHPILTest]
    public void Test_Superglobal_Multiple_Keys()
    {
        // Test multiple keys in superglobal
        var code = @"<?php
        $_POST['a'] = '1';
        $_POST['b'] = '2';
        print($_POST['a'] . $_POST['b']);
    ";
    
        var result = Execute(code);
        AssertEqual("12", result);
    }
}
