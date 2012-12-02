<?php
/**
 * JSON Proxy for javascript.
 * Lets you use normal JSON instead of JSONP!
 */
 
$url = "http://localhost:9074/"; // Set this to your honorbuddy api.


// --- DONT EDIT BELOW ---
$result = file_get_contents($url . '?'.http_build_query($_GET), false,stream_context_create(array('http' => array( 'ignore_errors' => true))));
foreach($http_response_header as $v){
	header($v);
}

echo $result;