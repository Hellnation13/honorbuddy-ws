/**
 * Generic API to use with Peecs Webservice.
 * @version 0.0.2
 * @author Peec
 */
var HonorbuddyAPI = function(apiLocation, secretKey, apiVersion, successCallback, errorCallback){

	var that = this;


	/* Me API */
	var me = function(){};
	me.getPlayerInfo = function(sHandler, eHandler){
		that.run("me:playerInfo", sHandler, eHandler);
	};
	me.getGameStats = function(sHandler, eHandler){
		that.run("me:gameStats", sHandler, eHandler);
	};
	me.getItems = function(sHandler, eHandler){
		that.run("me:items", sHandler, eHandler);
	};

	this.me = me;

	/* Bot API*/
	var bot = function(){};
	bot.stop = function(sHandler, eHandler){
		that.run("bot:stop", sHandler, eHandler);
	};
	bot.start = function(sHandler, eHandler){
		that.run("bot:start", sHandler, eHandler);
	};
	bot.isRunning = function(sHandler, eHandler){
		that.run("bot:isRunning", sHandler, eHandler);
	};
	this.bot = bot;


	/* Game API */
	var game = function(){};
	game.getScreenshots = function(sHandler, eHandler){
		that.run("game:getScreenshots", sHandler, eHandler);
	};
	game.takeScreenshot = function(sHandler, eHandler){
		that.run("game:takeScreenshot", sHandler, eHandler);
	};
	this.game = game;

	/* Chat API */
	var chat = function(){};
	chat.send = function(msg, chatType, language, channel, sHandler, eHandler){
		that.run("chat:send", sHandler, eHandler, {
			msg: msg,
			chatType: chatType,
			language: language,
			channel: channel
		});
	};
	chat.logs = function(EventName, sHandler, eHandler){
		that.run("chat:logs", sHandler, eHandler, {
			EventName: EventName
		});
	};
	this.chat = chat;


	/* Generic Run command */
	this.run = function(cmd, sHandler, eHandler, extras){

		var params = {
				"cmd": cmd,
				"secretKey": this.secretKey,
				"apiVersion": this.apiVersion
		};
		if (typeof extras !== 'undefined'){
			for (var attrname in extras) { params[attrname] = extras[attrname]; }
		}

		this.onBeforeRun(params);

		var req = $.ajax({
			url: this.apiLocation + (this.useJsonp ? "?callback=?" : ""),
			data: params,
			dataType: this.useJsonp ? "jsonp" : "json"
		});

		var s = sHandler || this.successCallback;
		var e = eHandler || this.errorCallback;
		if (!this.useJsonp){
			req.success(s);
			req.error(function(xhr){
				e(JSON.parse(xhr.responseText));
			});
		}else{
			req.success(function(data){
				if (data.error){
					e(data);
				}else{
					s(data);
				}
			});
		}

	};

	this.setApiLocation = function(loc){
		this.apiLocation = loc;
	};

	/**
	 * API Location (example http://localhost:9097)
	 */
	this.setApiLocation(apiLocation);
	/**
	 * Server API Key
	 */
	this.secretKey = secretKey;
	/**
	 * Server api version.
	 */
	this.apiVersion = apiVersion;
	/**
	 * Global error callback.
	 */
	this.successCallback = successCallback;
	/**
	 * Global success callback.
	 */
	this.errorCallback = errorCallback;

	/**
	 * Whenever to use JSONP or not.
	 */
	this.useJsonp = true;

	/**
	 * Run before ajax request is done
	 * @param array params Array of parameters sent to the ajax request.
	 */
	this.onBeforeRun = function(params){};

};
