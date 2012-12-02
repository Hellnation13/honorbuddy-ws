JSM.module("Layout", function(Layout, JSM, Backbone, Marionette, $, _) {

	
	var Layout = Marionette.Layout.extend({
		template : "#tpl-template",
		regions : {
			header : "#header",
			sidebar: '#sidebar',
			main : "#main",
			footer : "#footer"
		},
		initialize : function() {
			console.log('Initializing the master layout');
			
		},
		onRender: function(){
			this.header.show(new Header());
			this.sidebar.show(new Sidebar());
		}
	});
	
	
	var Header = Marionette.ItemView.extend({
		template: '#tpl-header',
		ui: {
			currentStatus: '#currentStatus'
		},
		onRender: function(){
			this.$("[rel=tooltip]").tooltip({
				placement: 'right'
			});
		}
	});
	
	var AjaxLoader = Marionette.Layout.extend({
		initialize: function(attrs){
			this.model = new Backbone.Model({message: attrs.message});
		},
		template: '#tpl-ajax-loader'
	});
	
	var MainUnableConnect = Marionette.ItemView.extend({
		template: '#tpl-unable-connect'
	});
	
	
	
	var ConnectionPage = Marionette.Layout.extend({
		template: '#tpl-connection-success-panel',
		regions: {
			page: '#connectionPage'
		},
		events: {
			'click .navTo': 'navTo'
		},
		navTo: function(){
			
		},
		onRender: function(){
			this.page.show(new MainFrame());

		}
	});
	
	
	var MainFrame = Marionette.Layout.extend({
		template: '#tpl-frame-main',
		regions: {
			playerInfo: '#playerStats',
			screenshots: '#screenshots'
		},
		onRender: function(){
			this.setScreens();
			this.playerInfo.show(new MainFrameStats());
		},
		events: {
			'click #takeScreen': 'takeScreen'
		},
		takeScreen: function(e){
			e.preventDefault();
			this.screenshots.show(new AjaxLoader({message: "Taking screneshot"}));
			var that = this;
			JSM.HB.game.takeScreenshot(function(data){
				that.setScreens();
			}, function(data){
				this.screenshots.show(new GenericError({message: data.error}));
			});
		},
		setScreens: function(){
			this.screenshots.show(new AjaxLoader({message: "Getting screenshots"}));
			var that = this;
			JSM.HB.game.getScreenshots(function(data){
				var arr = [];
				_.each(data.result, function(val, key){arr[key] = val; }); // to Array.
				var col = new JSM.Model.ScreenshotCollection(arr.slice(0,4));
				that.screenshots.show(new ScreenshotsView({collection: col}));
			}, function(){
				this.screenshots.show(new GenericError({message: data.error}));
			});
		}
	});
	
	var MainFrameStats = Marionette.Layout.extend({
		template: '#tpl-frame-main-stats',
		regions: {
			items: '#s-items',
			playerInfo: '#s-playerInfo',
			gameStats: '#s-gameStats'
		},
		events: {
			'click #player-refresh': 'updatePlayer',
			'click #game-refresh': 'updateGameStats',
			
		},
		onRender: function(){
			this.updateGameStats();
		},
		updatePlayer: function(){
			var that = this;
			this.playerInfo.show(new AjaxLoader({message: "Refreshing player info"}));
			JSM.HB.me.getPlayerInfo(function(data){
				that.playerInfo.show(new MainFrameStatsPlayer({model: new Backbone.Model(data.result)}));
			});
		},
		updateGameStats: function(){
			var that = this;
			this.gameStats.show(new AjaxLoader({message: "Refreshing game stats"}));
			JSM.HB.me.getGameStats(function(data){
				that.gameStats.show(new MainFrameStatsGame({model: new Backbone.Model(data.result)}));
			});
		}
	});
	var MainFrameStatsPlayer = Marionette.ItemView.extend({
		template: '#tpl-frame-main-stats-player',
		onRender: function(){
			this.$('#nextLevel_bar').css('width', (this.model.get("Experience") / this.model.get("NextLevelExperience") * 100) + "%")
			this.$('#durability_bar').css('width', (100 - this.model.get("DurabilityPercent")) + "%");
		}
	});
	var MainFrameStatsGame = Marionette.ItemView.extend({
		template: '#tpl-frame-main-stats-game',
		onRender: function(){
			var tot = this.model.get("BGsWon") + this.model.get("BGsLost");
			
			var winPercent = tot == 0 ? 0 : parseInt(this.model.get("BGsWon") / tot * 100);
			
			this.$('#bg_winPercent').text(winPercent + "%");
		}
	});
	
	
	var ScreenshotView = Marionette.ItemView.extend({
		template: '#tpl-screenshot',
		templateHelpers: {
			getFullUrl: function(url){
				return JSM.HB.apiLocation + url;
			}
		}
	});
	var ScreenshotsView = Marionette.CollectionView.extend({
		itemView: ScreenshotView
		
	});
	
	
	var GenericError = Marionette.ItemView.extend({
		initialize: function(attrs){
			this.model = new Backbone.Model({message: attrs.message});
		},
		className: 'alert alert-error',
		template: '#tpl-generic-error'
	});
	
	
	
	var Sidebar = Marionette.Layout.extend({
		template: '#tpl-siderbar',
		ui: {
			apikey: '#apiKey',
			conUrl: '#conUrl'
		},
		events: {
			'change #apiKey': 'updateConnection',
			'change #conUrl': 'updateConnection'
		},
		updateConnection: function(){
			JSM.layout.main.show(new AjaxLoader({message: "Waiting for valid connection details..."}));
			JSM.HB.secretKey = this.ui.apikey.val();
			JSM.HB.setApiLocation(this.ui.conUrl.val());
			// Test..
			JSM.HB.checkConnection(function (){
				JSM.layout.main.show(new ConnectionPage());
			}, function(){
				JSM.layout.main.show(new MainUnableConnect());
			});
			
		},
		onRender: function(){
			this.updateConnection();
		}
	});
	
	

	JSM.addInitializer(function() {
		console.log("Init : Layout");
		JSM.layout = new Layout();
		JSM.content.show(JSM.layout);
	});

	
	Layout.getLoader = function(message){
		return _.template($("#tpl-ajax-loader").html(), {message: message});		
	};
	
	
	return Layout;
});

