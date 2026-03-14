<?php

namespace Eventing;

class Events
{
    private static Events $self;
    
    private array $subscriberTable = [];
    
    private function __construct()
    {
        self::$self = $this;
    }

    public static function On(string $evName, Closure $callable)
    {
        if(!self::$self)
        {
            self::$self = new self();
        } 
        if (!self::$self->subscriberTable[$evName])
        {
            self::$self->subscriberTable[$evName] = [];
        }
        self::$self->subscriberTable[$evName][] = $callable;
    }

    public static function Dispatch(string $evName, array $data)
    {
        if(!self::$self)
        {
            self::$self = new self();
        } 
        if (!self::$self->subscriberTable[$evName])
        {
            self::$self->subscriberTable[$evName] = [];
        }
        foreach(self::$self->subscriberTable[$evName] as $subscriber)
        {
            $subscriber($data);
        }
    }
}