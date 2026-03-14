using PHPIL.Engine.Runtime;

namespace PHPIL.Tests.Engine.Execution;

public class SwitchExecutionTests : BaseTest
{
    private string Execute(string source)
    {
        var span = source.AsSpan();
        Runtime.Execute(in span, "tests");
        return Runtime.GetExecutionResult();
    }

    // -------------------------------------------------------------------------
    // Empty / default only
    // -------------------------------------------------------------------------

    [PHPILTest]
    public void Switch_Empty()
    {
        var result = Execute("<?php switch(1) {}");
        AssertEqual("", result);
    }

    [PHPILTest]
    public void Switch_JustDefault()
    {
        var result = Execute("<?php switch(1) { default: }");
        AssertEqual("", result);
    }

    [PHPILTest]
    public void Switch_DefaultWithPrint()
    {
        var result = Execute("<?php switch(1) { default: print('x'); }");
        AssertEqual("x", result);
    }

    // -------------------------------------------------------------------------
    // Single case — match / no match
    // -------------------------------------------------------------------------

    [PHPILTest]
    public void Switch_SingleCase_Matches()
    {
        var result = Execute("<?php switch(1) { case 1: print('yes'); }");
        AssertEqual("yes", result);
    }

    [PHPILTest]
    public void Switch_SingleCase_NoMatch()
    {
        var result = Execute("<?php switch(2) { case 1: print('yes'); }");
        AssertEqual("", result);
    }

    [PHPILTest]
    public void Switch_SingleCase_NoMatch_DefaultFires()
    {
        var result = Execute("<?php switch(2) { case 1: print('no'); default: print('yes'); }");
        AssertEqual("yes", result);
    }

    // -------------------------------------------------------------------------
    // Multiple cases — correct branch selected
    // -------------------------------------------------------------------------

    [PHPILTest]
    public void Switch_MultipleCases_FirstMatches()
    {
        var result = Execute("<?php switch(1) { case 1: print('a'); case 2: print('b'); case 3: print('c'); }");
        AssertEqual("a", result);
    }

    [PHPILTest]
    public void Switch_MultipleCases_MiddleMatches()
    {
        var result = Execute("<?php switch(2) { case 1: print('a'); case 2: print('b'); case 3: print('c'); }");
        AssertEqual("b", result);
    }

    [PHPILTest]
    public void Switch_MultipleCases_LastMatches()
    {
        var result = Execute("<?php switch(3) { case 1: print('a'); case 2: print('b'); case 3: print('c'); }");
        AssertEqual("c", result);
    }

    [PHPILTest]
    public void Switch_MultipleCases_NoneMatch()
    {
        var result = Execute("<?php switch(99) { case 1: print('a'); case 2: print('b'); }");
        AssertEqual("", result);
    }

    [PHPILTest]
    public void Switch_MultipleCases_NoneMatch_DefaultFires()
    {
        var result = Execute("<?php switch(99) { case 1: print('a'); case 2: print('b'); default: print('z'); }");
        AssertEqual("z", result);
    }

    // -------------------------------------------------------------------------
    // Default placement — before, middle, after cases
    // -------------------------------------------------------------------------

    [PHPILTest]
    public void Switch_DefaultFirst_CaseMatches()
    {
        var result = Execute("<?php switch(1) { default: print('z'); case 1: print('a'); }");
        AssertEqual("a", result);
    }

    [PHPILTest]
    public void Switch_DefaultFirst_NoMatch()
    {
        var result = Execute("<?php switch(99) { default: print('z'); case 1: print('a'); }");
        AssertEqual("z", result);
    }

    [PHPILTest]
    public void Switch_DefaultMiddle_CaseMatches()
    {
        var result = Execute("<?php switch(2) { case 1: print('a'); default: print('z'); case 2: print('b'); }");
        AssertEqual("b", result);
    }

    [PHPILTest]
    public void Switch_DefaultMiddle_NoMatch()
    {
        var result = Execute("<?php switch(99) { case 1: print('a'); default: print('z'); case 2: print('b'); }");
        AssertEqual("z", result);
    }

    // -------------------------------------------------------------------------
    // String switch values
    // -------------------------------------------------------------------------

    [PHPILTest]
    public void Switch_StringValue_Matches()
    {
        var result = Execute("<?php switch('hello') { case 'hello': print('yes'); }");
        AssertEqual("yes", result);
    }

    [PHPILTest]
    public void Switch_StringValue_NoMatch()
    {
        var result = Execute("<?php switch('hello') { case 'world': print('no'); }");
        AssertEqual("", result);
    }

    [PHPILTest]
    public void Switch_StringValue_NoMatch_DefaultFires()
    {
        var result = Execute("<?php switch('hello') { case 'world': print('no'); default: print('yes'); }");
        AssertEqual("yes", result);
    }

    [PHPILTest]
    public void Switch_StringValue_MultipleStringCases()
    {
        var result = Execute("<?php switch('b') { case 'a': print('1'); case 'b': print('2'); case 'c': print('3'); }");
        AssertEqual("2", result);
    }

    // -------------------------------------------------------------------------
    // Type strictness — int vs string
    // -------------------------------------------------------------------------

    [PHPILTest]
    public void Switch_StrictEquals_IntDoesNotMatchString()
    {
        // switch uses strict (===) comparison, so 1 should not match '1'
        var result = Execute("<?php switch(1) { case '1': print('matched'); default: print('no'); }");
        AssertEqual("no", result);
    }

    [PHPILTest]
    public void Switch_StrictEquals_ZeroDoesNotMatchFalse()
    {
        var result = Execute("<?php switch(0) { case false: print('matched'); default: print('no'); }");
        AssertEqual("no", result);
    }

    // -------------------------------------------------------------------------
    // Case body with multiple statements
    // -------------------------------------------------------------------------

    [PHPILTest]
    public void Switch_CaseBody_MultipleStatements()
    {
        var result = Execute("<?php switch(1) { case 1: print('a'); print('b'); print('c'); }");
        AssertEqual("abc", result);
    }

    [PHPILTest]
    public void Switch_DefaultBody_MultipleStatements()
    {
        var result = Execute("<?php switch(99) { default: print('x'); print('y'); print('z'); }");
        AssertEqual("xyz", result);
    }

    // -------------------------------------------------------------------------
    // Expression in switch condition
    // -------------------------------------------------------------------------

    [PHPILTest]
    public void Switch_ExpressionCondition_Matches()
    {
        var result = Execute("<?php $x = 2; switch($x + 1) { case 3: print('yes'); }");
        AssertEqual("yes", result);
    }

    [PHPILTest]
    public void Switch_ExpressionCondition_NoMatch()
    {
        var result = Execute("<?php $x = 2; switch($x + 1) { case 99: print('no'); default: print('yes'); }");
        AssertEqual("yes", result);
    }

    // -------------------------------------------------------------------------
    // Variable as case value
    // -------------------------------------------------------------------------

    [PHPILTest]
    public void Switch_VariableCaseValue_Matches()
    {
        var result = Execute("<?php $a = 5; switch(5) { case $a: print('yes'); }");
        AssertEqual("yes", result);
    }

    [PHPILTest]
    public void Switch_VariableCaseValue_NoMatch()
    {
        var result = Execute("<?php $a = 5; switch(6) { case $a: print('no'); default: print('yes'); }");
        AssertEqual("yes", result);
    }

    // -------------------------------------------------------------------------
    // Nested switch
    // -------------------------------------------------------------------------

    [PHPILTest]
    public void Switch_Nested_OuterMatchInnerMatch()
    {
        var result = Execute(@"<?php
            switch(1) {
                case 1:
                    switch(2) {
                        case 2: print('inner'); 
                    }
                    print('outer');
            }
        ");
        AssertEqual("innerouter", result);
    }

    [PHPILTest]
    public void Switch_Nested_OuterMatchInnerNoMatch()
    {
        var result = Execute(@"<?php
            switch(1) {
                case 1:
                    switch(99) {
                        case 2: print('inner');
                        default: print('default');
                    }
                    print('outer');
            }
        ");
        AssertEqual("defaultouter", result);
    }

    [PHPILTest]
    public void Switch_Nested_OuterNoMatch()
    {
        var result = Execute(@"<?php
            switch(99) {
                case 1:
                    switch(2) {
                        case 2: print('inner');
                    }
            }
        ");
        AssertEqual("", result);
    }

    // -------------------------------------------------------------------------
    // Switch inside a loop
    // -------------------------------------------------------------------------

    [PHPILTest]
    public void Switch_InsideForLoop()
    {
        var result = Execute(@"<?php
            for ($i = 1; $i <= 3; $i++) {
                switch($i) {
                    case 1: print('a');
                    case 2: print('b');
                    case 3: print('c');
                }
            }
        ");
        AssertEqual("abc", result);
    }
}