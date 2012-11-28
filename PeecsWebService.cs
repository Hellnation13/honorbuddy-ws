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
using System.Collections.Specialized;
using System.Text;

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

                    web = new HttpServer(20);
                    web.ProcessRequest += OnRequest;
                    
                    web.Start(webservicePort);
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

        public override void  Dispose()
        {
 	         base.Dispose();
            
            Styx.CommonBot.BotEvents.OnBotStopped -= onStop;
            Styx.CommonBot.BotEvents.OnBotStarted -= onStart;
            Logging.Write("OnStop : WebService");
            if (web != null)
            {
                web.Stop();
                web = null;
            }

        }

        private void onStop(EventArgs args)
        {
            Styx.CommonBot.BotEvents.OnBotStopped -= onStop;
            Styx.CommonBot.BotEvents.OnBotStarted -= onStart;
            if (web != null)
            {
                web.Stop();
                web = null;
            }
        }


        public void OnRequest(HttpListenerContext ctx)
        {
            string result = "{}";
            HttpListenerResponse response = ctx.Response;
            response.ContentType = "application/json";
            response.ContentEncoding = Encoding.UTF8;

            try
            {

                var res = parseResult(ctx.Request.QueryString);
                result = JSON.JsonEncode(res);

                if (res != null && result != null)
                {
                    response.StatusCode = 200;
                }
                else
                {
                    throw new Exception("Request is invalid.");
                }
                
            }
            catch (Exception e)
            {
                response.StatusCode = 400;
                
                Dictionary<string, string> data = new Dictionary<string, string>();
                data["ok"] = "false";
                Logging.Write(string.Format("Error {0} stack: {1}", e.Message, e.StackTrace));
                result = JSON.JsonEncode(data);
            }

            // Support JSONP.
            string callback = ctx.Request.QueryString.Get("callback");
            if (callback != null)
            {
                result = callback+"("+result+")";
                response.ContentType = "application/javascript";
            }


            byte[] buffer = Encoding.UTF8.GetBytes(result);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
            response.Close();

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

                Dictionary<string, string> reqData = new Dictionary<string, string>();
                reqData["secretKey"] = this.secretKey;
                reqData["apiVersion"] = this.Version.ToString();
                foreach(string key in data.Keys){
                    reqData[key] = data[key];
                }

                WebRequest req = HttpWebRequest.Create(new Uri(this.webserviceUrl));
                req.Method = "POST";
                req.ContentType = "application/json";
                using(var streamWriter = new StreamWriter(req.GetRequestStream())){
                    streamWriter.Write(JSON.JsonEncode(reqData));
                    streamWriter.Flush();
                    streamWriter.Close();
                }
            }
            catch (WebException e)
            {
                Logging.Write(string.Format("Error {0} stack: {1}", e.Message, e.StackTrace));
            }
            return false;
		}


        public Dictionary<string, string> parseResult(NameValueCollection res)
        {

            if ((string)res["secretKey"] == "" || (string)res["secretKey"] != this.secretKey){
                Logging.Write("Response is invalid. Must be of type JSON and include \"secretKey\" with the corresponding secret key.");
                return null;
            }
            if ((string)res["apiVersion"] == "" || (string)res["apiVersion"] != this.Version.ToString())
            {
                Logging.Write("Response is invalid. Must be of type JSON and include correct \"apiVersion\". Current API VERSION is " + Version.ToString());
                return null;
            }




            Dictionary<string, string> reqData = new Dictionary<string, string>();
            
            reqData["secretKey"] = this.secretKey;
            reqData["apiVersion"] = this.Version.ToString();

            Dictionary<string, string> result = new Dictionary<string, string>();
            switch ((string)res["cmd"])
            {
                default:
                    Logging.Write(string.Format("No support for cmd: {0}", (string)res["cmd"]));
                    return null;
                case "stats":
                    result = readAllData();
                    break;
            }
            
            foreach (string key in result.Keys)
            {
                reqData[key] = result[key];
            }


            return reqData;
        }
		
		
	}
	
}