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
			this.ui.currentStatus.effect("pulsate", { times:3 }, 3000);
		},
		statsIntervalLock: false,
		startTimers: function(){
			this.statusId = setInterval(this.updateStatus,2000, this);
		},
		stopTimers: function(){
			if (this.statusId){
				clearInterval(this.intervalId);
			}
			
		},
		updateStatus: function(that){
			if (that.statsIntervalLock)return;
			that.statsIntervalLock = true;
			JSM.HB.bot.isRunning(function(data){
				that.statsIntervalLock = false;
				if (data.result.isRunning){
					that.ui.currentStatus.attr('class','r_status ok');
				}else{
					that.ui.currentStatus.attr('class','r_status fail');
				}
			}, function(){
				that.statsIntervalLock = false;
				that.ui.currentStatus.attr('class','r_status unknown');
			});
			that.ui.currentStatus.effect("pulsate", { times:1 }, 3000);
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
		onRender: function(){
			this.page.show(new MainFrame());

			JSM.layout.header.currentView.startTimers();
		},
		onClose: function(){
			JSM.layout.header.currentView.stopTimers();
		}
	});
	
	
	var MainFrame = Marionette.Layout.extend({
		template: '#tpl-frame-main',
		regions: {
			playerInfo: '#playerStats',
			screenshots: '#screenshots'
		},
		gameStatIntervalLock: false,
		onRender: function(){
			this.updateGameStats(this);
			this.setScreens();
			
			// Timer for game stats
			this.gameStatsId = setInterval(this.updateGameStats,5000, this);
		},
		updateGameStats: function(that){
			if (that.gameStatIntervalLock)return;
			
			console.log("UPDATEGAMESTATS.");
			that.gameStatIntervalLock = true;
			JSM.HB.me.getAllStats(function(data){
				that.gameStatIntervalLock = false;
				that.playerInfo.show(new MainFrameStats({model: new Backbone.Model(data.result)}));
			}, function(data){
				that.gameStatIntervalLock = false;
				that.playerInfo.close();
			});
		},
		onClose: function(){
			clearInterval(this.gameStatsId);
			
		},
		events: {
			'click #takeScreen': 'takeScreen'
		},
		takeScreen: function(e){
			e.preventDefault();
			this.screenshots.show(new AjaxLoader({message: "Taking screneshot"}));;
			var that = this;
			JSM.HB.game.takeScreenshot(function(data){
				that.setScreens();
			}, function(data){
				this.screenshots.show(new GenericError({message: data.error}));
			});
		},
		setScreens: function(){
			this.screenshots.show(new AjaxLoader({message: "Getting screenshots"}));;
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
		onRender: function(){
			this.items.show(new MainFrameStatsI({model: this.model}));
			this.items.show(new MainFrameStatsP({model: this.model}));
			this.items.show(new MainFrameStatsG({model: this.model}));
		}
	});
	
	var MainFrameStatsI = Marionette.ItemView.extend({
		template: '#tpl-frame-main-stats-i',
	});
	var MainFrameStatsP = Marionette.ItemView.extend({
		template: '#tpl-frame-main-stats-p',
	});
	var MainFrameStatsG = Marionette.ItemView.extend({
		template: '#tpl-frame-main-stats-g',
		
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

