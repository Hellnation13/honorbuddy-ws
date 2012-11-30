﻿using Styx.Plugins;
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
using System.Net.NetworkInformation;

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

        
        static HttpServer web;

        // Configuration 
        private string apiKey;
        private int webservicePort;
		
        



        public override void Initialize()
        {

            Logging.Write("Init : WebService");
            startServer();

        }

        public void startServer()
        {

            this.apiKey = WSSettings.Instance.apikey;
            this.webservicePort = WSSettings.Instance.webport;

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
                    
                    web.ProcessRequest -= OnRequest;
                    web.ProcessRequest += OnRequest;
                    web.Start(webservicePort);
                    Logging.Write(string.Format("Listening on http://*:{0}/ for JSON request commands.. ", webservicePort));
                    Logging.Write(string.Format("USING SECRET API KEY: {0}", apiKey));

                }
                catch (Exception e)
                {

                    Logging.Write(string.Format("Error {0} stack: {1}", e.Message, e.StackTrace));
                }

            }
            else
            {
                stopServer();
            }
        }

        public void stopServer()
        {
            if (web != null)
            {
                web.Stop();
                while (web.isListening())
                {
                    Thread.Sleep(1000);
                    Logging.Write("Waiting on webserver to stop.");
                }
            }
        }


        public override void Dispose()
        {
            stopServer();
            base.Dispose();
            
        }


        public void OnRequest(HttpListenerContext ctx)
        {
            string result = "{}";
            HttpListenerResponse response = ctx.Response;
            response.ContentType = "application/json";
            response.ContentEncoding = Encoding.UTF8;

            try
            {

                Hashtable res = parseResult(ctx.Request.QueryString);
                if (res != null)
                {
                    res["ok"] = true;
                }
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

                Hashtable data = new Hashtable();
                data["ok"] = false;
                data["error"] = e.Message;
                data["result"] = new Hashtable();
                Logging.Write(string.Format("Error {0} stack: {1}", e.Message, e.StackTrace));
                result = JSON.JsonEncode(data);
            }

            // Support JSONP.
            string callback = ctx.Request.QueryString.Get("callback");
            if (callback != null)
            {
                result = callback+"("+result+")";
                response.ContentType = "application/javascript";
                response.StatusCode = 200; // jsonp must have 200 code :(
            }


            byte[] buffer = Encoding.UTF8.GetBytes(result);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
            response.Close();

        }


        
		public override void Pulse(){
            
        }

        public Hashtable getStats()
        {
            Hashtable data = new Hashtable();
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

            return data;
		}

        public Hashtable parseResult(NameValueCollection res)
        {

            if ((string)res["secretKey"] == "" || (string)res["secretKey"] != this.apiKey){
                Logging.Write("Response is invalid. Must be of type JSON and include \"secretKey\" with the corresponding secret key.");
                throw new Exception("Could not authenticate this key.");
            }
            if ((string)res["apiVersion"] == "" || (string)res["apiVersion"] != this.Version.ToString())
            {
                Logging.Write("Response is invalid. Must be of type JSON and include correct \"apiVersion\". Current API VERSION is " + Version.ToString());
                throw new Exception(string.Format("Wrong API version of client ({0}), current server API version is: {1}.", res["apiVersion"], Version.ToString()));
            }




            Hashtable reqData = new Hashtable();
            
            reqData["secretKey"] = this.apiKey;
            reqData["apiVersion"] = this.Version.ToString();

            Hashtable result = new Hashtable();
            switch ((string)res["cmd"])
            {
                default:
                    throw new Exception(string.Format("{0} is not a valid cmd.", res["cmd"]));
                case "me:stats":
                    result = getStats();
                    break;
                case "bot:start":
                    if (Styx.CommonBot.TreeRoot.IsRunning)
                    {
                        throw new Exception("Bot is currently running.");
                    }
                    else
                    {
                        Styx.CommonBot.TreeRoot.Start();
                        result["success"] = "Bot started.";
                    }
                    break;
                case "bot:isRunning":
                    result["isRunning"] = Styx.CommonBot.TreeRoot.IsRunning;
                    break;
                case "bot:stop":
                    if (!Styx.CommonBot.TreeRoot.IsRunning)
                    {
                        throw new Exception("Bot is not running.");
                    }
                    else
                    {
                        Styx.CommonBot.TreeRoot.Stop();
                        result["success"] = "Bot stopped.";
                    }
                    break;


            }
            reqData["result"] = result;


            return reqData;
        }
		
		
	}
	
}