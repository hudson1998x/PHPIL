<?php

class User
{
    public int $id;
    public string $username;
    
    public function dump()
    {
        print("User ( Id: {$this->id}, Username: {$this->username} )\n");
    }        
}

$usernames = ["john", "morgan", "elijah", "luke","barry", "mitchell", "paul"];
$ids       = [1     , 2       , 3       , 4     ,5      , 6         , 7];

$built = [];

foreach($usernames as $pos => $uname)
{
    $user = new User();
    $user->id = $ids[$pos];
    $user->username = $uname;
    $built[] = $user;    
}

foreach($built as $user)
{
    $user->dump();
}
