/**
 * Test application.
 */
$(function(){
	$("#result").val(""); // Force set value to nothing at reload.
	$("#chat_msg").val("yup ..");
	// Used for errors and success.
	var successCallback = function(data){
		$("#result").val(JSON.stringify(data));

		$("#datamapped").html(renderContents(data));

		$('#reqStatus').text("REQUEST OK");
		$('#reqStatus').attr('style', 'color: green;');

		$('#panelRequest').show();

	};

	var api = new HonorbuddyAPI($("#serverInformation").val(), $("#apiKey").val(),
			successCallback, 
			// Global Error callback..
			function(data){
		successCallback(data);

		$('#reqStatus').text("REQUEST FAILED");
		$('#reqStatus').attr('style', 'color: red;');
	}
	);



	$('.serverHost').text(api.apiLocation);



	api.onBeforeRun = function(params){
		var strParams = "";
		$.each(params, function(k, v){
			strParams +=  "&" + k + "=" + v;
		});

		$('#requestSent').text(this.apiLocation + strParams);
	};


	$("#serverInformation").change(function(){
		api.setApiLocation($(this).val());
		$('.serverHost').text(api.apiLocation);
	});
	$("#apiKey").change(function(){
		api.secretKey = $(this).val();
	});

	$('.apiLookup').click(function(){
		$("#result").val("");
		$("#datamapped").text("");
		switch($(this).data('cmd')){
		case "me:playerInfo":
			api.me.getPlayerInfo();
			break;
		case "me:gameStats":
			api.me.getGameStats();
			break;
		case "me:items":
			api.me.getItems();
			break;
		case "bot:stop":
			api.bot.stop();
			break;
		case "bot:start":
			api.bot.start();
			break;
		case "bot:isRunning":
			api.bot.isRunning();
			break;
		case "game:getScreenshots":
			api.game.getScreenshots();
			break;
		case "game:takeScreenshot":
			api.game.takeScreenshot();
			break;
		case "chat:send":
			api.chat.send($('#chat_msg').val(), $('#chat_chatType').val(),$('#chat_language').val(),$('#chat_channel').val());
			break;
		case "chat:logs":
			api.chat.logs($('#chatLogs_EventName').val());
			break;

		}
	});
});



/* App helpers.. */

/* Just to print list recursive of json..*/
function renderContents(contents) {
	var index, ul;

	// Create a list for these contents
	ul = $("<ul>");

	// Fill it in
	$.each(contents, function(index, entry) {
		var li;

		// Create list item
		li = $("<li>");

		// Set the text
		li.html("<strong>" + index + "</strong>: " + (typeof entry !== 'object' ? entry : ""));

		// Append a sublist of its contents if it has them
		if (typeof entry === 'object') {
			li.append(renderContents(entry));
		}

		// Add this item to our list
		ul.append(li);
	});

	// Return it
	return ul;
}

