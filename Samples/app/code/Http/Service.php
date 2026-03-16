<?php

namespace Http;

// use Eventing\Events;

class Service
{
    public function __construct()
    {
        \Eventing\Events::On('request', function($url) {
            var_dump($url);
        });
    }
}