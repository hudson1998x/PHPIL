<?php

trait MyObject
{
    public function getNumber(int $number): int
    {
        return $number;
    }    
}

class Other
{
    use MyObject;    
}

$inst = new Other();

print($inst->getNumber(255));