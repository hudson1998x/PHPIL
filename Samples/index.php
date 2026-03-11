<?php

trait MyObject
{
    public function getNumber(): int
    {
        return 5;
    }    
}

class Other
{
    use MyObject;    
}