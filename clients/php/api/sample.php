<?php

include "HonorbuddyWS.php";

// Create instance.
$ws = new HonorbuddyWS("yours33cretk333yyy", "http://localhost:9097");

// Sample of getting all items.
$items = $ws->me->getItems();

// Print..
print_r($items);

