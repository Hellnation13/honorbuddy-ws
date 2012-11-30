<?php

/**
 * TODO
 */
class HonorbuddyWS{
	private $apiKey, $url, $apiVersion = "0.0.2";
	
	
	public function __construct($apiKey, $url){
		$this->apiKey = $apiKey;
		$this->url = $url;
	}
	
	
	
	
	
	private function getData($cmd, $params){
		$params['secretKey'] = $this->apiKey;
		$params['apiVersion'] = $this->apiVersion;
		
		
		$result = file_get_contents('http://localhost:9097?'.http_build_query($params), false);
		return json_encode($result);
	}
	
	
	
}