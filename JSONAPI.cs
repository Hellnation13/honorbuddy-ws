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
using Styx.Common.Helpers;
using System.Collections.Specialized;
using System.Text;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using Styx.WoWInternals.WoWObjects;

namespace com.peec.webservice
{
    public partial class JSONAPI
    {
        public System.Version Version;
        public string apiKey;

        public GameHandle Game;
        public MeHandle Me;
        public BotHandle Bot;
        public ChatHandle Chat;

        public JSONAPI(string apiKey, System.Version Version)
        {
            this.apiKey = apiKey;
            this.Version = Version;
            this.Game = new GameHandle(this);
            this.Me = new MeHandle(this);
            this.Bot = new BotHandle(this);
            this.Chat = new ChatHandle(this);
        }



        #region Me Specific API

        public class MeHandle : APIStub
        {
            public MeHandle(JSONAPI api) : base(api) { }

            /**
             *  Tries to get all stats, if one stat fails its not added.
             * 
             */
            public Hashtable getAllStats(LocalPlayer Me)
            {
                Hashtable me = new Hashtable();

                try
                {
                    me["items"] = getItems(Me);
                }catch(Exception){ }
                try
                {
                    me["playerInfo"] = getPlayerInfo(Me);
                }
                catch (Exception) { }
                try
                {
                    me["gameStats"] = getGameStats(Me);
                }
                catch (Exception) { }

                return me;
            }


            public Hashtable getItems(LocalPlayer Me)
            {
                Hashtable me = new Hashtable();
                using (Styx.StyxWoW.Memory.AcquireFrame())
                {
                    
                    Hashtable items = new Hashtable();
                    List<WoWItem> its = Me.BagItems;
                    for (int i = 0; i < its.Count; i++)
                    {
                        items[its[i].Entry] = its[i].StackCount;
                    }
                    me["BagItems"] = items;

                    Hashtable items2 = new Hashtable();
                    WoWItem[] equipped = Me.Inventory.Equipped.Items;
                    for (int i = 0; i < equipped.Length; i++)
                    {
                        if (equipped[i] != null) items2[equipped[i].Entry] = equipped[i].DurabilityPercent;
                    }
                    me["EquippedItems"] = items2;
                }
                return me;
            }

            public Hashtable getPlayerInfo(LocalPlayer Me)
            {
                Hashtable me = new Hashtable();
                using (Styx.StyxWoW.Memory.AcquireFrame())
                {
                    
                    me["Level"] = Me.Level;
                    me["Experience"] = Me.Experience;
                    me["Copper"] = Me.Copper;
                    me["NextLevelExperience"] = Me.NextLevelExperience;
                    me["Durability"] = Me.Durability;
                    me["DurabilityPercent"] = Me.DurabilityPercent;
                    me["FreeBagSlots"] = Me.FreeBagSlots;

                    Hashtable loc = new Hashtable();
                    loc["X"] = Me.X;
                    loc["Y"] = Me.Y;
                    loc["Z"] = Me.Z;
                    loc["ZoneText"] = Me.ZoneText;

                    me["WorldLocation"] = loc;
                }
                return me;
            }

            public Hashtable getGameStats(LocalPlayer Me)
            {
                Hashtable me = new Hashtable();
                using (Styx.StyxWoW.Memory.AcquireFrame())
                {
                    me["XPPerHour"] = Styx.CommonBot.GameStats.XPPerHour;
                    me["TimeToLevel"] = Styx.CommonBot.GameStats.TimeToLevel.TotalSeconds;
                    me["MobsKilled"] = Styx.CommonBot.GameStats.MobsKilled;
                    me["MobsPerHour"] = Styx.CommonBot.GameStats.MobsPerHour;
                    me["HonorGained"] = Styx.CommonBot.GameStats.HonorGained;
                    me["HonorPerHour"] = Styx.CommonBot.GameStats.HonorPerHour;
                    me["BGsWon"] = Styx.CommonBot.GameStats.BGsWon;
                    me["BGsLost"] = Styx.CommonBot.GameStats.BGsLost;

                }

                return me;
            }
        }

        #endregion


        #region Bot Specific API

        public class BotHandle : APIStub
        {
            public BotHandle(JSONAPI api) : base(api) { }


            public Hashtable start()
            {
                Hashtable result = new Hashtable();
                if (Styx.CommonBot.TreeRoot.IsRunning)
                {
                    throw new Exception("Bot is currently running.");
                }
                else
                {
                    Styx.CommonBot.TreeRoot.Start();
                    result["success"] = "Bot started.";
                }
                return result;
            }

            public Hashtable stop()
            {
                Hashtable result = new Hashtable();
                if (!Styx.CommonBot.TreeRoot.IsRunning)
                {
                    throw new Exception("Bot is not running.");
                }
                else
                {
                    Styx.CommonBot.TreeRoot.Stop();
                    result["success"] = "Bot stopped.";
                }
                return result;
            }

            public Hashtable isRunning()
            {
                Hashtable result = new Hashtable();
                result["isRunning"] = Styx.CommonBot.TreeRoot.IsRunning;
                return result;
            }


        }


        #endregion




        #region Chat Specific API
        public class ChatHandle : APIStub
        {
            public ChatHandle(JSONAPI api) : base(api) { }

            public Hashtable logs(List<com.peec.webservice.PeecsWebService.ChatLog> chatLogs, string EventName)
            {
                Hashtable result = new Hashtable();

                List<com.peec.webservice.PeecsWebService.ChatLog> list;
                if (EventName == null || EventName == "")
                {
                    list = chatLogs;
                }
                else
                {
                    list = chatLogs.FindAll(delegate(com.peec.webservice.PeecsWebService.ChatLog cl) { return cl.EventName == EventName; });
                }

                list.Sort((x, y) => x.FireTimeStamp.CompareTo(y.FireTimeStamp));

                for (int i = 0; i < list.Count; i++)
                {
                    if (i > 200) continue;
                    com.peec.webservice.PeecsWebService.ChatLog l = list[i];
                    Hashtable listing = new Hashtable();
                    listing["EventName"] = l.EventName;
                    listing["Author"] = l.Author;
                    listing["FireTimeStamp"] = l.FireTimeStamp;
                    listing["Message"] = l.Message;
                    result[i] = listing;
                }


                return result;
            }

        }

        #endregion




        #region Game Specific API

        public class GameHandle : APIStub
        {
            public GameHandle(JSONAPI api) : base(api) { }



            public Hashtable getScreenshots(DirectoryInfo screenDir)
            {
                Hashtable result = new Hashtable();

                FileInfo[] files = screenDir.GetFiles();
                Array.Sort(files, delegate(FileInfo f1, FileInfo f2)
                {
                    return f2.LastWriteTime.CompareTo(f1.LastWriteTime);
                });

                for (int i = 0; i < files.Length; i++)
                {
                    if (i == 50) continue; // Max 50.

                    FileInfo f = files[i];

                    Hashtable t = new Hashtable();
                    t["screenshot"] = f.Name;
                    t["url"] = "?img=" + f.Name + "&secretKey=" + api.apiKey + "&apiVersion=" + api.Version.ToString();
                    result[i] = t;

                }
                return result;
            }


            public Hashtable takeScreenshot(DirectoryInfo screenDir)
            {
                Hashtable result = new Hashtable();
                LuaAPI.TakeScreenshot();

                FileInfo file = Utils.GetLatestFileInDir(screenDir);
                result["screenshot"] = file.Name;
                result["url"] = "?img=" + file.Name + "&secretKey=" + api.apiKey + "&apiVersion=" + api.Version.ToString();

                return result;
            }
        }


        #endregion





        abstract public class APIStub
        {
            protected JSONAPI api;

            public APIStub(JSONAPI api)
            {
                this.api = api;
            }
        }

    }
}
