using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Styx.Common;
using System.Net;
using System.Collections;
using System.Collections.Specialized;
using System.IO;

namespace com.peec.webservice
{
    class Server : IDisposable
    {


        // Configuration 
        
        Func<NameValueCollection, Hashtable> doParseResult;
        Action<NameValueCollection> checkAccess;
        HttpServer web;

        string imageLocation;

        public Server(int webservicePort, string imageLocation, Action<NameValueCollection> checkAccess, Func<NameValueCollection, Hashtable> doParseResult)
        {
            
            this.doParseResult = doParseResult;
            this.imageLocation = imageLocation;
            this.checkAccess = checkAccess;

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


                Logging.Write(string.Format("Spawning new webserver on http://localhost:{0}.", webservicePort));

            }
            catch (Exception e)
            {

                Logging.Write(string.Format("Error {0} stack: {1}", e.Message, e.StackTrace));
            }
        }

        public void Dispose()
        {
            web.Stop();
            web = null;
        }






        public void OnRequest(HttpListenerContext ctx)
        {
            string result = "";
            byte[] buffer;
            HttpListenerResponse response = ctx.Response;

            // Check access..
            try
            {
                checkAccess(ctx.Request.QueryString);
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
                img = imageLocation + img;

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
                Hashtable res = doParseResult(ctx.Request.QueryString);
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
                result = callback + "(" + result + ")";
                response.ContentType = "application/javascript";
                response.StatusCode = 200; // jsonp must have 200 code :(
            }

            HttpServer.SendResponse(response, result);
        }





    }
}
