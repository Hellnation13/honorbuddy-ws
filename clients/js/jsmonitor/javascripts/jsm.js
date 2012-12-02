var JSM = JSM || {}; 

JSM = new Backbone.Marionette.Application();
JSM.HB = new HonorbuddyAPI();

JSM.addRegions({
	content : "#appStub"
});

JSM.bind("initialize:after", function(options) {
	if(Backbone.history) {
		Backbone.history.start();
		console.log("Starting history.");
	}
});
