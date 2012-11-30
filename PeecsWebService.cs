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
        private List<ChatLog> chatLogs;

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

            Logging.Write("Init : WebService");
            startServer();

            

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

            chatLogs.Clear();

            

        }

        private DirectoryInfo getScreenshotDir(){
            return new DirectoryInfo(WSSettings.Instance.WoWPath + "\\Screenshots\\");
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
                buffer = Encoding.UTF8.GetBytes(result);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
                response.Close();
                return;
            }



            // Image
            string img = ctx.Request.QueryString.Get("img");
            if (img != null)
            {
                response.StatusCode = 200;
                img = WSSettings.Instance.WoWPath + "\\Screenshots\\" + img;
                    
                response.ContentType = "image/jpeg";
                buffer = System.IO.File.ReadAllBytes(img);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
                response.Close();
                
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


            buffer = Encoding.UTF8.GetBytes(result);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
            response.Close();

        }

        


        
		public override void Pulse(){

            if (chatLogs.Count >= 5000)
            {
                chatLogs.RemoveAll(delegate(ChatLog l) {
                    return !l.IsWhisper();
                });
            }
        }

        public Hashtable getStats()
        {
            Hashtable data = new Hashtable();
                using (Styx.StyxWoW.Memory.AcquireFrame())
                {
                    
                    data["Level"] = Styx.StyxWoW.Me.Level;
                    data["Experience"] = Styx.StyxWoW.Me.Experience;
                    data["NextLevelExperience"] = Styx.StyxWoW.Me.NextLevelExperience;
                    data["XPPerHour"] = Styx.CommonBot.GameStats.XPPerHour;
                    data["TimeToLevel"] = Styx.CommonBot.GameStats.TimeToLevel.TotalSeconds;
                    data["MobsKilled"] = Styx.CommonBot.GameStats.MobsKilled;
                    data["MobsPerHour"] = Styx.CommonBot.GameStats.MobsPerHour;
                    data["HonorGained"] = Styx.CommonBot.GameStats.HonorGained;
                    data["HonorPerHour"] = Styx.CommonBot.GameStats.HonorPerHour;
                    data["BGsWon"] = Styx.CommonBot.GameStats.BGsWon;
                    data["BGsLost"] = Styx.CommonBot.GameStats.BGsLost;
                    data["Copper"] = Styx.StyxWoW.Me.Copper;
                    data["NodeCollectionCount"] = Bots.Gatherbuddy.GatherbuddyBot.NodeCollectionCount;

                }

            return data;
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

            Hashtable result = new Hashtable();
            switch ((string)res["cmd"])
            {
                default:
                    throw new Exception(string.Format("{0} is not a valid cmd.", res["cmd"]));
                case "me:stats":
                    result = getStats();
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
                    FileInfo[] files = getScreenshotDir().GetFiles();
                    Array.Sort(files, delegate(FileInfo f1, FileInfo f2)
                    {
                        return f2.LastWriteTime.CompareTo(f1.LastWriteTime);
                    });

                    for (int i = 0; i < files.Length; i++ )
                    {
                        if (i == 50) continue; // Max 50.
                        
                        FileInfo f = files[i];
                        
                        Hashtable t = new Hashtable();
                        t["screenshot"] = f.Name;
                        t["url"] = "?img=" + f.Name + "&secretKey=" + apiKey + "&apiVersion=" + Version.ToString();
                        result[i] = t;

                    }
                    break;
                case "game:takeScreenshot":
                    LuaAPI.TakeScreenshot();

                    FileInfo file = Utils.GetLatestFileInDir(getScreenshotDir());
                    result["screenshot"] = file.Name;
                    result["url"] = "?img=" + file.Name + "&secretKey=" + apiKey + "&apiVersion=" + Version.ToString();


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
                case "chat:logs":
                    List<ChatLog> list;
                    if (res["EventName"] == null || res["EventName"] == "")
                    {
                        list = chatLogs;
                    }
                    else
                    {
                        list = chatLogs.FindAll(delegate(ChatLog cl) { return cl.EventName  == res["type"]; });
                    }
                    
                    for(int i=0; i < list.Count; i++){
                        ChatLog l = list[i];
                        Hashtable listing = new Hashtable();
                        listing["EventName"] = l.EventName;
                        listing["Author"] = l.Author;
                        listing["FireTimeStamp"] = l.FireTimeStamp;
                        listing["Message"] = l.Message;
                        result[i] = listing;
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

        


        private void QueueChat(Styx.CommonBot.Chat.ChatLanguageSpecificEventArgs e)
        {
            Logging.Write("Queue chat..." + e.EventName);
            chatLogs.Add(new ChatLog(e.EventName, e.Author, e.Message, e.FireTimeStamp));
        }

        private class ChatLog
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