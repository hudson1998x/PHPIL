<?php

require_once('Samples/app/autoload.php');

Eventing\Events::On("trigger", function(array $items) {
    print("Received items");
});

Eventing\Events::Dispatch('trigger', []);

print('it doesn\'t print');