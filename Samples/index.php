<?php

$i = 0;


while(true)
{
    if ($i > 9)
    {
        print("Waiting for break: " . $i);
        $i++;
        break;
    }
    if ($i > 3)
    {
        print("Continuing" . $i);
        $i++;
        continue;
    }
    print("Whoo!" . $i);
    
   
    $i++;
}