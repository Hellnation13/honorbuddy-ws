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
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using Styx.WoWInternals.WoWObjects;

namespace com.peec.webservice
{
    public partial class PeecsWebService : HBPlugin
    {
		
	
		// Metas
        public override string Author { get { return "Peec"; } }
        public override string Name { get { return "Peecs WebService"; } }
        public override System.Version Version { get { return new Version(0, 0, 2); } }
        public override bool WantButton { get { return true; } }

        private LocalPlayer Me { get { return Styx.StyxWoW.Me; } }

        public override void OnButtonPress()
        {
            new FormSettings().ShowDialog();
        }

        
        static HttpServer web;
        private List<ChatLog> chatLogs;
        private String WoWPath;

        // Configuration 
        private string apiKey;
        private int webservicePort;
		
        



        public override void Initialize()
        {
            chatLogs = new List<ChatLog>();

            Chat.Say += QueueChat;
            Chat.Yell += QueueChat;
            Chat.Whisper += QueueChat;
            Chat.Party += QueueChat;
            Chat.PartyLeader += QueueChat;
            Chat.Battleground += QueueChat;
            Chat.BattlegroundLeader += QueueChat;
            Chat.Raid += QueueChat;
            Chat.RaidLeader += QueueChat;


            Styx.CommonBot.BotEvents.OnBotStopped += OnStop;
            Styx.CommonBot.BotEvents.OnBotStarted += OnStart;

            Logging.Write("Init : WebService");

            startServer();

            WoWPath = Path.GetDirectoryName(Styx.StyxWoW.Memory.Process.MainModule.FileName);


        }
        public override void Dispose()
        {
            base.Dispose();

            stopServer();
            Chat.Say -= QueueChat;
            Chat.Yell -= QueueChat;
            Chat.Whisper -= QueueChat;
            Chat.Party -= QueueChat;
            Chat.PartyLeader -= QueueChat;
            Chat.Battleground -= QueueChat;
            Chat.BattlegroundLeader -= QueueChat;
            Chat.Raid -= QueueChat;
            Chat.RaidLeader -= QueueChat;

            Styx.CommonBot.BotEvents.OnBotStopped -= OnStop;
            Styx.CommonBot.BotEvents.OnBotStarted -= OnStart;

            chatLogs.Clear();

            

        }

        public void OnStart(EventArgs args)
        {
            chatLogs.Clear();
        }
        public void OnStop(EventArgs args)
        {
            chatLogs.Clear();
        }




        private DirectoryInfo getScreenshotDir(){
            return new DirectoryInfo(WoWPath + "\\Screenshots\\");
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


        


        public void OnRequest(HttpListenerContext ctx)
        {
            string result = "{}";
            byte[] buffer;
            HttpListenerResponse response = ctx.Response;

            // Check access..
            try
            {
                checkRequestAccess(ctx.Request.QueryString);
            }
            catch (Exception e)
            {
                response.ContentType = "application/json";
                response.StatusCode = 400;
                Hashtable data = new Hashtable();
                data["ok"] = false;
                data["error"] = e.Message;
                data["result"] = new Hashtable();
                Logging.Write(string.Format("Error {0} stack: {1}", e.Message, e.StackTrace));
                result = JSON.JsonEncode(data);
                HttpServer.SendResponse(response, result);
                return;
            }



            // Image
            string img = ctx.Request.QueryString.Get("img");
            if (img != null)
            {
                response.StatusCode = 200;
                img = WoWPath + "\\Screenshots\\" + img;
                    
                response.ContentType = "image/jpeg";
                buffer = System.IO.File.ReadAllBytes(img);
                HttpServer.SendResponse(response, buffer);
                return;
            }

            // Json

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

            HttpServer.SendResponse(response, result);
        }

        


        
		public override void Pulse(){

            if (chatLogs.Count >= 5000)
            {
                chatLogs.RemoveAll(delegate(ChatLog l) {
                    return !l.IsWhisper();
                });
            }
        }

        


        public void checkRequestAccess(NameValueCollection res)
        {
            if ((string)res["secretKey"] == "" || (string)res["secretKey"] != this.apiKey)
            {
                Logging.Write("Response is invalid. Must be of type JSON and include \"secretKey\" with the corresponding secret key.");
                throw new Exception("Could not authenticate this key.");
            }
            if ((string)res["apiVersion"] == "" || (string)res["apiVersion"] != this.Version.ToString())
            {
                Logging.Write("Response is invalid. Must be of type JSON and include correct \"apiVersion\". Current API VERSION is " + Version.ToString());
                throw new Exception(string.Format("Wrong API version of client ({0}), current server API version is: {1}.", res["apiVersion"], Version.ToString()));
            }
        }

        public Hashtable parseResult(NameValueCollection res)
        {

            

            Hashtable reqData = new Hashtable();
            
            reqData["secretKey"] = this.apiKey;
            reqData["apiVersion"] = this.Version.ToString();


            JSONAPI reqHandler = new JSONAPI(this.apiKey, this.Version);

            Hashtable result = new Hashtable();
            switch ((string)res["cmd"])
            {
                default:
                    throw new Exception(string.Format("{0} is not a valid cmd.", res["cmd"]));
                case "me:playerInfo":
                    result = reqHandler.Me.getPlayerInfo(Me);
                    break;
                case "me:gameStats":
                    result = reqHandler.Me.getGameStats(Me);
                    break;
                case "me:items":
                    result = reqHandler.Me.getItems(Me);
                    break;
                case "chat:send":
                    LuaAPI.SendChatMessage(
                        LuaAPI.cs(res["msg"]), 
                        LuaAPI.cs(res["chatType"]),
                        LuaAPI.cs(res["language"]), 
                        LuaAPI.cs(res["channel"])
                        );
                    break;
                case "game:getScreenshots":
                    result = reqHandler.Game.getScreenshots(getScreenshotDir());
                    break;
                case "game:takeScreenshot":
                    result = reqHandler.Game.takeScreenshot(getScreenshotDir());
                    break;
                case "bot:start":
                    result = reqHandler.Bot.start();
                    break;
                case "chat:logs":
                    result = reqHandler.Chat.logs(chatLogs, res["EventName"]);
                    break;
                case "bot:isRunning":
                    result = reqHandler.Bot.isRunning();
                    break;
                case "bot:stop":
                    result = reqHandler.Bot.stop();
                    break;
                


            }
            reqData["result"] = result;


            return reqData;
        }

        


        private void QueueChat(Styx.CommonBot.Chat.ChatLanguageSpecificEventArgs e)
        {
            chatLogs.Add(new ChatLog(e.EventName, e.Author, e.Message, e.FireTimeStamp));
        }

        public class ChatLog
        {
            public string EventName, Author, Message;
            public uint FireTimeStamp;
            public ChatLog(string EventName, string Author, string Message, uint FireTimeStamp)
            {
                this.EventName = EventName;
                this.Author = Author;
                this.Message = Message;
                this.FireTimeStamp = FireTimeStamp;
            }
            public bool IsWhisper()
            {
                return EventName == "CHAT_MSG_WHISPER";
            }
        }
		
	}
	
}