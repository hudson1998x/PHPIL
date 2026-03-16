<?php

require_once('app/bootstrap.php');

Eventing\Events::On('request', function($url) {
    print("[URL] {$url}");
});

Eventing\Events::Dispatch('request', $_SERVER['REQUEST_URI']);

print('<pre>');
var_dump($_SERVER);