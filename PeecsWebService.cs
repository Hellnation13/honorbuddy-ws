using Styx.Plugins;
using Styx.WoWInternals;
using Styx.CommonBot;
using Styx.Common;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Styx.MemoryManagement;
using System.Web;
using System.Net;
using System.Threading;
using Styx.Common.Helpers;

namespace com.peec.webservice
{
    public partial class PeecsWebService : HBPlugin
    {
		
	
		// Metas
        public override string Author { get { return "Peec"; } }
        public override string Name { get { return "Peecs WebService"; } }
        public override System.Version Version { get { return new Version(0, 0, 2); } }
        public override bool WantButton { get { return true; } }
        public override void OnButtonPress()
        {
            new FormSettings().ShowDialog();
        }

        private readonly WaitTimer _itemCheckTimer = WaitTimer.TenSeconds;
        private HttpServer web;

        // Configuration 
        private string secretKey = "MY_SECRET_KEY";
        
        private string webserviceUrl = "http://localhost/api"; // Update server.
        
        // Request server
        private int webservicePort = 9096; 
        private bool enabledPostback = false;
        private string webserviceHost = "localhost";
		// How often to send requests.
		const int PUSH_WAIT_TIME = 20;
        

        public override void Initialize()
        {

            Logging.Write("Init : WebService");

            Styx.CommonBot.BotEvents.OnBotStopped += onStop;
            Styx.CommonBot.BotEvents.OnBotStarted += onStart;

        }

        

        private void onStart(EventArgs args)
        {
            Logging.Write("OnStart : WebService");


            this.secretKey = WSSettings.Instance.apikey;
            this.webserviceUrl = WSSettings.Instance.apiurl;
            this.webservicePort = WSSettings.Instance.webport;
            this.webserviceHost = WSSettings.Instance.weburl;
            this.enabledPostback = WSSettings.Instance.enablePostback;

            Logging.Write(string.Format("Starting Webservice Plugin. Using postback: {0}, using webserver: {1}", this.enabledPostback, WSSettings.Instance.enableWebserver));

            if (WSSettings.Instance.enableWebserver)
            {


                try
                {
                    if (!HttpListener.IsSupported)
                    {
                        Logging.Write("HttpListener not supported.");
                        return;
                    }

                    web = new HttpServer(OnRequest, "http://" + webserviceHost + ":" + webservicePort + "/");
                    
                    web.Run();
                    Logging.Write(string.Format("Listening on http://{0}:{1}/ for JSON request commands.. ", webserviceHost, webservicePort));
                    Logging.Write(string.Format("USING SECRET API KEY: {0}", secretKey));

                }
                catch (Exception e)
                {
                    Logging.Write(string.Format("Error {0} stack: {1}", e.Message, e.StackTrace));
                }
                
            }

            if (enabledPostback)
            {
                Logging.Write(string.Format("Posting stats and actions to API: {0} . ", this.webserviceUrl));
            }


        }
        private void onStop(EventArgs args)
        {
            Logging.Write("OnStop : WebService");

            Styx.CommonBot.BotEvents.OnBotStopped -= onStop;
            Styx.CommonBot.BotEvents.OnBotStarted -= onStart;

            if (web != null)
            {
                web.Stop();
            }
        }


        public string OnRequest(HttpListenerResponse response, HttpListenerRequest request)
        {

            try
            {
                Boolean ok = false;
                using (var streamReader = new StreamReader(request.InputStream))
                {
                    string reqString = streamReader.ReadToEnd();
                    if (reqString == "")
                    {
                        ok = false;
                    }
                    else
                    {

                        var req = (Hashtable)JSON.JsonDecode(reqString);
                        ok = parseResult(req);
                    }
                }
                response.StatusCode = ok ? 200 : 400;
                response.ContentType = "text/json";
                Dictionary<string, string> data = new Dictionary<string, string>();
                data["ok"] = ok + "";

                return JSON.JsonEncode(data);
            }
            catch (Exception e)
            {
                response.StatusCode = 400;
                response.ContentType = "text/json";
                Dictionary<string, string> data = new Dictionary<string, string>();
                data["ok"] = "false";
                Logging.Write(string.Format("Error {0} stack: {1}", e.Message, e.StackTrace));
                return JSON.JsonEncode(data);
            }
            
        }


        
		public override void Pulse(){
            if (!_itemCheckTimer.IsFinished)
                return;

            Logging.Write("Pulse : WebService" + Version.ToString());

            _itemCheckTimer.Reset();

            if (this.enabledPostback) pushData(readAllData());
		}
		
		public Dictionary<string,string> readAllData(){
            Dictionary<string, string> data = new Dictionary<string, string>();
            try
            {
                using (Styx.StyxWoW.Memory.AcquireFrame())
                {

                    data["level"] = Convert.ToInt32(Styx.StyxWoW.Me.Level).ToString();
                    data["xp"] = Convert.ToUInt32(Styx.StyxWoW.Me.Experience).ToString();
                    data["xp_needed"] = Convert.ToUInt32(Styx.StyxWoW.Me.NextLevelExperience).ToString();
                    data["xph"] = Convert.ToUInt32(Styx.CommonBot.GameStats.XPPerHour).ToString();
                    data["timetolevel"] = Convert.ToUInt32(Styx.CommonBot.GameStats.TimeToLevel.TotalSeconds).ToString();
                    data["kills"] = Convert.ToUInt32(Styx.CommonBot.GameStats.MobsKilled).ToString();
                    data["killsh"] = Convert.ToUInt32(Styx.CommonBot.GameStats.MobsPerHour).ToString();
                    data["honor"] = Convert.ToUInt32(Styx.CommonBot.GameStats.HonorGained).ToString();
                    data["honorh"] = Convert.ToUInt32(Styx.CommonBot.GameStats.HonorPerHour).ToString();
                    data["bgwin"] = Convert.ToUInt32(Styx.CommonBot.GameStats.BGsWon).ToString();
                    data["bglost"] = Convert.ToUInt32(Styx.CommonBot.GameStats.BGsLost).ToString();
                    data["gold"] = Convert.ToUInt32(Styx.StyxWoW.Me.Copper).ToString();
                    data["nodeh"] = JSON.JsonEncode(Bots.Gatherbuddy.GatherbuddyBot.NodeCollectionCount);

                }
            }
            catch (Exception e)
            {
               Logging.Write(string.Format("Error {0} stack: {1}", e.Message, e.StackTrace));
            }

            return data;
		}

		
		public Boolean pushData(Dictionary<string, string> data){
            try
            {

                Dictionary<string, Dictionary<string, string>> reqData = new Dictionary<string, Dictionary<string, string>>();
                reqData["meta"]["secretKey"] = this.secretKey;
                reqData["meta"]["apiVersion"] = this.Version.ToString();
                
                reqData["result"] = data;

                WebRequest req = HttpWebRequest.Create(new Uri(this.webserviceUrl));
                req.Method = "POST";
                req.ContentType = "text/json";
                using(var streamWriter = new StreamWriter(req.GetRequestStream())){
                    streamWriter.Write(JSON.JsonEncode(reqData));
                    streamWriter.Flush();
                    streamWriter.Close();
                }
                var resp = (HttpWebResponse)req.GetResponse();
                if (resp.StatusCode != HttpStatusCode.OK)
                {
                    Logging.Write(string.Format("Response was not status code 200, but {0}, could not parse response. Service should return 200 if success.", resp.StatusCode.ToString()));
                }
                using (var streamReader = new StreamReader(resp.GetResponseStream()))
                {
                    var result = (Hashtable)JSON.JsonDecode(streamReader.ReadToEnd());
                    return parseResult(result);
                }
            }
            catch (WebException e)
            {
                Logging.Write(string.Format("Error {0} stack: {1}", e.Message, e.StackTrace));
            }
            return false;
		}


        public Boolean parseResult(Hashtable res)
        {
            if (res == null)
            {
                Logging.Write("Error, got invalid request / response, should be valid JSON.");
            }

            if ((string)res["secretKey"] == "" || (string)res["secretKey"] != this.secretKey){
                Logging.Write("Response is invalid. Must be of type JSON and include \"secretKey\" with the corresponding secret key.");
                return false;
            }
            if ((string)res["apiVersion"] == "" || (string)res["apiVersion"] != this.Version.ToString())
            {
                Logging.Write("Response is invalid. Must be of type JSON and include correct \"apiVersion\". Current API VERSION is " + Version.ToString());
                return false;
            }

            switch ((string)res["cmd"])
            {
                default:
                    Logging.Write(string.Format("No support for cmd: {0}", (string)res["cmd"]));
                    break;
                // Simple ok response.
                case "ok":
                    return true;
                // TODO.
            }

            return false;
        }
		
		
	}
	
}