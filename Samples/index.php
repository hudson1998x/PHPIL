<?php

$user = "John";

function print_username(string $username): void
{
    print("Hello, " . $username);    
}

print_username($username);