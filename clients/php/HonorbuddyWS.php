<?php

/**
 * TODO
 */
class HonorbuddyWS{
	
	const $VERSION = "0.0.2";
	
	private $apiKey, $url;
	
	
	public function __construct($apiKey, $url){
		$this->apiKey = $apiKey;
		this->url = $url;
	}
	
	
	
	public function getStats(){
		return $this->getData();
	}
	
	
	
	private function getData($params){
		$params['secretKey'] = $this->apiKey;
		$params['apiVersion'] = self::$VERSION;
	
		$result = file_get_contents('http://localhost:9097?'.http_build_query($params), false);
		return json_encode($result);
	}
	
	
	
}