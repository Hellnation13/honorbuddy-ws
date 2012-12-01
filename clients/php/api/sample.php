<?php

include "HonorbuddyWS.php";

// Create instance.
$ws = new HonorbuddyWS("yours33cretk333yyy", "http://localhost:9097");

// Sample of getting all items.
try{
	$items = $ws->me->getItems();

	// Print all the items.
	print_r($items['result']);
	
// On error on all api methods:
}catch(HB_JsonRequestException $e){
	echo $e->getMessage(); // Contains the error message.
	
	print_r($e->data); // Data contains array of all error information.
}


