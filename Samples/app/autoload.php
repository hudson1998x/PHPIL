<?php

spl_autoload_register(function(string $className) {
    
    $path = 'app/code/';
    $path .= str_replace('\\', '/', $className);
    $path .= '.php';
    
    print($path);
    die("");
});