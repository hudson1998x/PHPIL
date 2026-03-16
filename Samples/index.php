<?php

require_once('app/bootstrap.php');

Eventing\Events::Dispatch('request', $_SERVER['REQUEST_URI']);