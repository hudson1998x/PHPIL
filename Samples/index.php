<?php

$closure = function() {
    print("Closure works");
}

$otherMethod = "print";

$otherMethod("dynamic access works");

$closure();