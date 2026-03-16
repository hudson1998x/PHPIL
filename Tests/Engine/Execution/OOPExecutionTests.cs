using PHPIL.Engine.Runtime;
using PHPIL.Engine.Visitors;

namespace PHPIL.Tests.Engine.Execution;

public class OOPExecutionTests : BaseTest
{
    private string Execute(string source)
    {
        var span = source.AsSpan();
        Runtime.Execute(in span, "tests");
        return Runtime.GetExecutionResult();
    }

    [PHPILTest]
    public void Class_SimpleDeclaration()
    {
        var result = Execute("<?php class SimpleClass { } $t = new SimpleClass(); print('ok');");
        AssertEqual("ok", result);
    }

    [PHPILTest]
    public void Class_Instantiation_WithConstructor()
    {
        var result = Execute("<?php class ClassWithCtor { public function __construct() { print('constructed'); } } $t = new ClassWithCtor();");
        AssertEqual("constructed", result);
    }

    [PHPILTest]
    public void Class_Instantiation_WithArguments()
    {
        var result = Execute("<?php class ClassWithArgs { public function __construct($a, $b) { print($a + $b); } } $t = new ClassWithArgs(2, 3);");
        AssertEqual("5", result);
    }

    [PHPILTest]
    public void Class_MethodCall()
    {
        var result = Execute("<?php class ClassWithMethod { public function sayHello() { print('hello'); } } $t = new ClassWithMethod(); $t->sayHello();");
        AssertEqual("hello", result);
    }

    [PHPILTest]
    public void Class_MethodCall_WithReturn()
    {
        var result = Execute("<?php class CalculatorClass { public function add($a, $b) { return $a + $b; } } $calc = new CalculatorClass(); print($calc->add(3, 4));");
        AssertEqual("7", result);
    }

    [PHPILTest]
    public void Class_PropertyAccess()
    {
        var result = Execute("<?php class ClassWithProp { public $value = 42; } $t = new ClassWithProp(); print($t->value);");
        AssertEqual("42", result);
    }

    [PHPILTest]
    public void Class_PropertyAssignment()
    {
        var result = Execute("<?php class ClassWithProp2 { public $value = 0; } $t = new ClassWithProp2(); $t->value = 100; print($t->value);");
        AssertEqual("100", result);
    }

    [PHPILTest]
    public void Class_ChainedMethodCalls()
    {
        var result = Execute("<?php class ChainedClass { public function first() { print('first'); return $this; } public function second() { print('second'); } } $t = new ChainedClass(); $t->first()->second();");
        AssertEqual("firstsecond", result);
    }

    [PHPILTest]
    public void InstanceOf_True()
    {
        var result = Execute("<?php class InstanceTest { } $t = new InstanceTest(); print($t instanceof InstanceTest ? 'yes' : 'no');");
        AssertEqual("yes", result);
    }

    [PHPILTest]
    public void InstanceOf_False()
    {
        var result = Execute("<?php class InstanceTestA { } class InstanceTestB { } $t = new InstanceTestA(); print($t instanceof InstanceTestB ? 'yes' : 'no');");
        AssertEqual("no", result);
    }

    [PHPILTest]
    public void InstanceOf_WithVariable()
    {
        var result = Execute("<?php class InstanceVar { } $t = new InstanceVar(); $className = 'InstanceVar'; print($t instanceof $className ? 'yes' : 'no');");
        AssertEqual("yes", result);
    }

    [PHPILTest]
    public void Interface_Declaration()
    {
        var result = Execute("<?php interface TestInterface { public function doSomething(); } class TestImpl implements TestInterface { public function doSomething() { print('done'); } } $t = new TestImpl(); $t->doSomething();");
        AssertEqual("done", result);
    }

    [PHPILTest]
    public void Interface_Multiple()
    {
        var result = Execute("<?php interface InterfaceA { public function a(); } interface InterfaceB { public function b(); } class MultiImpl implements InterfaceA, InterfaceB { public function a() { print('a'); } public function b() { print('b'); } } $t = new MultiImpl(); $t->a(); $t->b();");
        AssertEqual("ab", result);
    }

    [PHPILTest]
    public void Trait_Declaration()
    {
        var result = Execute("<?php trait SimpleTrait { public function sayHello() { print('hello from trait'); } } class ClassUsingTrait { use SimpleTrait; } $t = new ClassUsingTrait(); $t->sayHello();");
        AssertEqual("hello from trait", result);
    }

    [PHPILTest]
    public void Trait_Multiple()
    {
        var result = Execute("<?php trait TraitAlpha { public function methodA() { print('A'); } } trait TraitBeta { public function methodB() { print('B'); } } class ClassUsingTraits { use TraitAlpha, TraitBeta; } $t = new ClassUsingTraits(); $t->methodA(); $t->methodB();");
        AssertEqual("AB", result);
    }

    [PHPILTest]
    public void Trait_Precedence()
    {
        var result = Execute("<?php trait TraitPrecedence { public function greet() { print('trait'); } } class ClassWithOverride { use TraitPrecedence; public function greet() { print('class'); } } $t = new ClassWithOverride(); $t->greet();");
        AssertEqual("class", result);
    }

    [PHPILTest]
    public void Static_MethodCall()
    {
        var result = Execute("<?php class StaticMethodClass { public static function staticMethod() { print('static'); } } StaticMethodClass::staticMethod();");
        AssertEqual("static", result);
    }

    [PHPILTest]
    public void Static_PropertyAccess()
    {
        var result = Execute("<?php class StaticPropClass { public static $value = 42; } print(StaticPropClass::$value);");
        AssertEqual("42", result);
    }

    [PHPILTest]
    public void Class_Inheritance()
    {
        var result = Execute("<?php class ParentClass { public function parentMethod() { print('parent'); } } class ChildClass extends ParentClass { public function childMethod() { print('child'); } } $c = new ChildClass(); $c->parentMethod(); $c->childMethod();");
        AssertEqual("parentchild", result);
    }

    [PHPILTest]
    public void Class_Inheritance_MethodOverride()
    {
        var result = Execute("<?php class ParentOverride { public function greet() { print('parent'); } } class ChildOverride extends ParentOverride { public function greet() { print('child'); } } $c = new ChildOverride(); $c->greet();");
        AssertEqual("child", result);
    }

    [PHPILTest]
    public void This_Keyword()
    {
        var result = Execute("<?php class ThisClass { public $value = 10; public function getValue() { return $this->value; } } $t = new ThisClass(); print($t->getValue());");
        AssertEqual("10", result);
    }

    [PHPILTest]
    public void Class_Constant()
    {
        var result = Execute("<?php class ConstClass { const VALUE = 100; } print(ConstClass::VALUE);");
        AssertEqual("100", result);
    }

    [PHPILTest]
    public void Class_Namespace_Simple()
    {
        var result = Execute("<?php namespace TestNs; class NamespacedClass { public function hello() { print('hello'); } } $t = new NamespacedClass(); $t->hello();");
        AssertEqual("hello", result);
    }

    [PHPILTest]
    public void Class_UseStatement()
    {
        var result = Execute("<?php namespace TestNs\\App; use TestNs\\Other\\MyClass; namespace TestNs\\Other; class MyClass { public function greet() { print('greet'); } } namespace TestNs\\App; $t = new MyClass(); $t->greet();");
        AssertEqual("greet", result);
    }

    [PHPILTest]
    public void TypeTable_ContainsRegisteredClass()
    {
        Execute("<?php class TypeTableTestClass { } $instance = new TypeTableTestClass();");
        var type = TypeTable.GetType("TypeTableTestClass");
        AssertNotNull(type, "Class should be registered in TypeTable");
    }

    [PHPILTest]
    public void TypeTable_ContainsRegisteredInterface()
    {
        Execute("<?php interface TypeTableTestInterface { }");
        var type = TypeTable.GetType("TypeTableTestInterface");
        AssertNotNull(type, "Interface should be registered in TypeTable");
    }

    [PHPILTest]
     public void TypeTable_ContainsRegisteredTrait()
     {
         Execute("<?php trait TypeTableTestTrait { }");
         var type = TypeTable.GetType("TypeTableTestTrait");
         AssertNotNull(type, "Trait should be registered in TypeTable");
     }

     [PHPILTest]
     public void Method_VariadicArgs()
     {
         var result = Execute("<?php class TestClass { public function test(...$args) { foreach ($args as $arg) { print($arg); } } } $obj = new TestClass(); $obj->test(1, 2, 3);");
         AssertEqual("123", result);
     }
 }
