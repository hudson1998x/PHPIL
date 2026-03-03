<?php

$executor = function(int $i = 0): bool {
    if ($i > 5)
    {
        print("Hello World: " . $i);
    }
}

function do_while(Closure $callable): void {
    $i = 0;
    while($i < 8)
    {
        $callable($i);
        $i++;
    }
}

do_while($executor);