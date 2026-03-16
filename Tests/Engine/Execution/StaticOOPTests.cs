using PHPIL.Tests;

namespace PHPIL.Tests.Engine.Execution
{
    public class StaticOOPTests : BaseTest
    {
        private string Execute(string source)
        {
            var span = source.AsSpan();
            PHPIL.Engine.Runtime.Runtime.Execute(in span, "tests");
            return PHPIL.Engine.Runtime.Runtime.GetExecutionResult();
        }

        [PHPILTest]
        public void Static_StringProperty_SelfAccess()
        {
            var result = Execute(@"<?php
namespace Test;
class MyClass {
    private static string $value = 'hello';
    public static function getValue() {
        return self::$value;
    }
}
print(MyClass::getValue());
");
            AssertEqual("hello", result);
        }

        [PHPILTest]
        public void Static_IntegerProperty_SelfAccess()
        {
            var result = Execute(@"<?php
namespace Test;
class Counter {
    private static int $count = 42;
    public static function getCount() {
        return self::$count;
    }
}
print(Counter::getCount());
");
            AssertEqual("42", result);
        }

        [PHPILTest]
        public void Static_ArrayProperty_KeyAccess()
        {
            var result = Execute(@"<?php
namespace Test;
class EventStore {
    private static array $events = [];
    public static function register($key, $value) {
        self::$events[$key] = $value;
    }
    public static function get($key) {
        return self::$events[$key];
    }
}
EventStore::register('test', 'value123');
print(EventStore::get('test'));
");
            AssertEqual("value123", result);
        }

        [PHPILTest]
        public void Static_ArrayProperty_AppendAndIterate()
        {
            var result = Execute(@"<?php
namespace Eventing;
class Events {
    private static array $eventTable = [];
    
    public static function On($evName, $callable) {
        print('On Called');
        self::$eventTable[$evName] = [$callable];
    }
    
    public static function Dispatch($evName, $obj) {
        foreach(self::$eventTable[$evName] as $sub) {
            $sub($obj);
        }
    }
}

Events::On('request', function($url) {
    print('[URL] ' . $url);
});

Events::Dispatch('request', '/path/to/users');
");
            AssertEqual("On Called[URL] /path/to/users", result);
        }

        [PHPILTest]
        public void Static_ArrayProperty_SimpleAssignment()
        {
            var result = Execute(@"<?php
namespace Test;
class Storage {
    private static array $data = [];
    public static function set($key, $value) {
        self::$data[$key] = $value;
    }
    public static function get($key) {
        return self::$data[$key];
    }
}
Storage::set('name', 'testvalue');
print(Storage::get('name'));
");
            AssertEqual("testvalue", result);
        }

        [PHPILTest]
        public void Static_Namespace_QualifiedClassName()
        {
            var result = Execute(@"<?php
namespace App\Services;
class Logger {
    private static string $prefix = 'APP';
    public static function log($msg) {
        return self::$prefix . ': ' . $msg;
    }
}
print(Logger::log('test'));
");
            AssertEqual("APP: test", result);
        }

        [PHPILTest]
        public void Static_Property_NestedNamespace()
        {
            var result = Execute(@"<?php
namespace App\Module\Core;
class Config {
    private static string $version = '1.0';
    
    public static function getVersion() {
        return self::$version;
    }
}
print(Config::getVersion());
");
            AssertEqual("1.0", result);
        }

        [PHPILTest]
        public void Static_ArrayProperty_Overwrite()
        {
            var result = Execute(@"<?php
namespace Cache;
class SimpleCache {
    private static array $values = [];
    
    public static function put($k, $v) {
        self::$values[$k] = $v;
    }
    
    public static function get($k) {
        return self::$values[$k];
    }
}

SimpleCache::put('key1', 'val1');
SimpleCache::put('key1', 'val2');
print(SimpleCache::get('key1'));
");
            AssertEqual("val2", result);
        }

        [PHPILTest]
        public void Static_StaticMethodCallWithSelf()
        {
            var result = Execute(@"<?php
namespace Test;
class Helper {
    private static string $greeting = 'Hi';
    
    public static function greet($name) {
        return self::$greeting . ' ' . $name;
    }
    
    public static function getGreeting() {
        return self::$greeting;
    }
}

print(Helper::greet('Alice'));
print(Helper::getGreeting());
");
            AssertEqual("Hi AliceHi", result);
        }
    }
}
