<?php

spl_autoload_register(function(string $className) {
    
    $path = 'Samples/app/code/';
    $path .= str_replace('\\', '/', $className);
    $path .= '.php';
    
    require_once($path);
});