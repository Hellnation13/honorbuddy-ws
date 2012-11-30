using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Styx.WoWInternals;

namespace com.peec.webservice
{
    class LuaAPI
    {

        static string[] chatTypeIds = { "SAY", "EMOTE", "YELL", "PARTY", "GUILD", "OFFICER", "RAID", "RAID_WARNING", "BATTLEGROUND", "WHISPER", "CHANNEL", "AFK", "DND" };


        /**
         * Following specs from:
         * http://www.wowwiki.com/API_SendChatMessage
         */
        static public void SendChatMessage(string msg, string chatType = "SAY", string language = null, string channel = null)
        {
            if (msg == null) throw new ArgumentException("Message must be set.");
            if (msg.Length > 255) throw new ArgumentException("Message must be < 255 characters");

            if (chatType != null && !chatTypeIds.Any(chatType.Contains))
            {
                throw new ArgumentException("chatType must be one of " + String.Join(", ", chatTypeIds));
            }


            string[] reqChatType = { "CHANNEL", "WHISPER" };
            if (chatType != null && reqChatType.Any(chatType.Contains) && channel == null)
            {
                throw new ArgumentException(" channel is required for the CHANNEL/WHISPER chat types ");
            }

            runLua(string.Format("SendChatMessage({0}, {1}, {2}, {3});",
                a(msg),
                a(chatType),
                a(language),
                a(channel)
                ));
        }

        static public bool TakeScreenshot()
        {
            runLua("TakeScreenshot()");
            return new LuaEventWait("SCREENSHOT_SUCCEEDED").Wait(new TimeSpan(0, 0, 30));
        }



        static private void runLua(String cmd)
        {
            Lua.DoString(cmd);
        }


        static private String a(String arg)
        {
            // for function calls just return it.
            if (arg != null && arg.IndexOfAny("()".ToCharArray()) != -1)
            {
                return arg;
            }
            // for numbers.
            int i;
            if (arg != null && int.TryParse(arg, out i))
            {
                return arg;
            }
            
            // strings
            return arg == null ? "nil" : "\""+arg+"\"";
        }


        /**
         * Returns null if empty string, else string.
         */
        static public String cs(String s){
            return s == "" ?  null : s;
        }
    }
}
