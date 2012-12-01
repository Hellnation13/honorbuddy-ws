<?php

/**
 * For communicating with Peecs Webservice plugin for Honorbuddy.
 * 
 * Communicates against JSON API.
 * 
 * @link https://github.com/peec/honorbuddy-ws
 * @version 0.0.2
 * @author Peec
 *
 */
class HonorbuddyWS {
	
	/**
	 * 
	 * @var string API key on the server side.
	 */
	private $apiKey;
	
	/**
	 * 
	 * @var string full url to json api (eg. http://localhost:9080 )
	 */
	private $url;
	
	/**
	 * @var string Version used on the server side.
	 */
	public  $apiVersion = "0.0.2";
		
	
	/**
	 * 
	 * @var HB_Game
	 */
	public $game;
	
	/**
	 * 
	 * @var HB_Chat Chat specific commands
	 */
	public $chat;
	
	/**
	 * 
	 * @var HB_Me Player specific commands
	 */
	public $me;
	
	/**
	 * 
	 * @var HB_Bot Bot specific commands
	 */
	public $bot;
	
	
	
	public function __construct($apiKey, $url){
		$this->apiKey = $apiKey;
		$this->url = $url;
	
		
		$this->game = new HB_Game($this);
		$this->chat = new HB_Chat($this);
		$this->me = new HB_Me($this);
		$this->bot = new HB_Bot($this);
		
	}
	
	public function setApiKey($apiKey){
		$this->apiKey = $apiKey;
	}
	
	public function setUrl($url){
		$this->url = $url;
	}
	
}


class HB_Me extends HB_NS{

	
	/**
	 * Gets player info
	 */
	public function getPlayerInfo(){
		return $this->run('me:' . __FUNCTION__);
	}
	
	/**
	 * 
	 * @return string
	 */
	public function getGameStats(){
		return $this->run('me:' . __FUNCTION__);
	}
	public function getItems(){
		return $this->run('me:' . __FUNCTION__);
	}
	
}


class HB_Bot extends HB_NS{
	public function stop(){
		return $this->run('bot:' . __FUNCTION__);
	}
	
	public function start(){
		return $this->run('bot:' . __FUNCTION__);
	}
	public function isRunning(){
		return $this->run('bot:' . __FUNCTION__);
	}
}


class HB_Game extends HB_NS{
	public function getScreenshots(){
		return $this->run('game:' . __FUNCTION__);
	}
	
	public function takeScreenshot(){
		return $this->run('game:' . __FUNCTION__);
	}
}



class HB_Chat extends HB_NS{
	public function send($msg, $chatType = "SAY", $language = null, $channel = null){
		return $this->run('chat:' . __FUNCTION__);
	}
	
	public function logs(){
		return $this->run('chat:' . __FUNCTION__);
	}
}


class HB_NS{

	/**
	 * @var HonorbuddyWS
	 */
	protected $ws;

	/**
	 *
	 * @param HonorbuddyWS $ws
	 */
	public function __construct(HonorbuddyWS $ws){
		$this->ws = $ws;
	}


	/**
	 *
	 * @param string $cmd Command to be sent.
	 * @param array $params
	 * @return array
	 * @throws HB_JsonRequestException Throws exception on error.
	 */
	protected function run($cmd, array $params = null){
		$params['secretKey'] = $this->apiKey;
		$params['apiVersion'] = $this->apiVersion;


		$args = $params === null ? '' : http_build_query($params);
		
		
		$result = file_get_contents('http://localhost:9097?'.$args, false,
				stream_context_create(
						array(
								'http' => array(
										'ignore_errors' => true // to catch errors.
								)
						)
				));
		$ex = explode(' ', isset($http_response_header) && isset($http_response_header[0]) ? $http_response_header[0] : '');
		$statusCode = (int) (isset($ex[1]) ? $ex[1] : 0 );

		$res = json_encode($result);
		if ($statusCode !== 0 && $statusCode !==  200){
			throw new HB_JsonRequestException($res);
		}
		
		return $res;
	}
}

class HB_JsonRequestException extends Exception{
	
	/**
	 * @var array Holds specific data for the request.
	 */
	public $data;
	
	public function __construct($message, $data){
		$this->data = $data;
		parent::__construct($message);
	}
	
	
}
