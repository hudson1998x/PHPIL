using PHPIL.Tests;

namespace PHPIL.Tests.Engine.Execution
{
    public class NamespaceTests : BaseTest
    {
        private string Execute(string source)
        {
            var span = source.AsSpan();
            PHPIL.Engine.Runtime.Runtime.Execute(in span, "tests");
            return PHPIL.Engine.Runtime.Runtime.GetExecutionResult();
        }

        [PHPILTest]
        public void TestNamespacedFunctionCall()
        {
            var source = @"<?php
namespace MyNamespace;
function my_func() { return 'Hello from namespace'; }
print(my_func());
";
            var result = Execute(source);
            AssertEqual("Hello from namespace", result);
        }

        [PHPILTest]
        public void TestFullyQualifiedCall()
        {
            var source = @"<?php
namespace A;
function test() { return 'A'; }
namespace B;
function test() { return 'B'; }
print(\A\test());
print(\B\test());
";
            var result = Execute(source);
            AssertEqual("AB", result);
        }

        [PHPILTest]
        public void TestNamespaceAliasing()
        {
            var source = @"<?php
namespace My\Project\Tool;
function run() { return 'Tool Running'; }

namespace Main;
use My\Project\Tool;
print(Tool\run());
";
            var result = Execute(source);
            AssertEqual("Tool Running", result);
        }

        [PHPILTest]
        public void TestGlobalFallback()
        {
            var source = @"<?php
function global_func() { return 'global'; }
namespace A;
print(global_func());
";
            var result = Execute(source);
            AssertEqual("global", result);
        }
    }
}
