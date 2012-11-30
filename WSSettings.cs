using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Styx.Helpers;
using Styx.Common;
using Styx;
using DefaultValue = Styx.Helpers.DefaultValueAttribute;

namespace com.peec.webservice
{
    class WSSettings : Settings
    {
        private static WSSettings _instance;
        public static WSSettings Instance { get { return _instance ?? (_instance = new WSSettings()); } }

        public WSSettings()
            : base(Path.Combine(Path.Combine(Styx.Helpers.GlobalSettings.SettingsDirectory, "Settings"), string.Format("WSSettings_{0}.xml", StyxWoW.Me.Name)))
        {

        }

        
        [Setting]
        [Category("General")]
        [DisplayName("API Key")]
        [DefaultValue("yours33cretk333yyy")]
        [Description("This is used as your secret key, set it to what you want.")]
        public string apikey { get; set; }



        [Setting]
        [Category("Webserver")]
        [DefaultValue(true)]
        [DisplayName("Enable webserver")]
        [Description("Allows people with your secret key to send commands to your bot. With the Webserver Url/Port + api key.")]
        public Boolean enableWebserver { get; set; }

        [Setting]
        [Category("Webserver")]
        [DisplayName("Webserver PORT")]
        [DefaultValue(9099)]
        public int webport { get; set; }



    }
}
