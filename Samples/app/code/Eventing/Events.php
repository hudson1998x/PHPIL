<?php

namespace Eventing;

class Events
{
    private static array $eventTable = [];
    
    public static function On(string $evName, $callable)
    {
        if (!isset(self::$eventTable[$evName]))
        {
            self::$eventTable[$evName] = [];
        }
        self::$eventTable[$evName][] = $callable;
    }    
    
    public static function Dispatch(string $evName, $obj)
    {
        foreach(self::$eventTable[$evName] as $sub)
        {
            $sub($obj);
        }
    }
}