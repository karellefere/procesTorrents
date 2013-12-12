using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Net;
using System.Xml.Linq;

namespace processTorrents
{
    class Program
    {
        static void Main(string[] args)
        {
            LogFile log = new LogFile();
            uTorrentAPI api = new uTorrentAPI(log);
            string torrentHash = "2A0C69B206B3C69118F2957153A03B208114EC2C";//test only change to args[0]

            if (!string.IsNullOrEmpty(torrentHash))
            {
                var files = api.getFiles(torrentHash);



            }
            else
            {
                log.Write("hash parameter empty");
            }

            log.Close();
        }
    }

    class uTorrentAPI
    {
        private LogFile _log;

        public uTorrentAPI(LogFile log)
        {
            _log = log;
        }

        public string getFiles(string hash)
        {
            var files = "";
            NameValueCollection queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["action"] = "getfiles";
            queryString["hash"] = hash;
            queryString["token"] = getToken(_log);

            var requestURI = Constants.APIurl + "?" + queryString.ToString();



            var req = (HttpWebRequest)WebRequest.Create(requestURI);
            req.Method = "GET";
            req.Credentials = new NetworkCredential(Constants.UserName, Constants.PassWord);

            files = getOutput(req, _log);

            return files;
        }

        private static string getToken(LogFile log)
        {
            var req = (HttpWebRequest)WebRequest.Create(Constants.APIurl + "token.html");
            req.Method = "GET";
            req.Credentials = new NetworkCredential(Constants.UserName, Constants.PassWord);
            req.ContentLength = 0;

            var output = getOutput(req, log);
            var xOutput = XDocument.Parse(output);
            output = xOutput.Descendants("div").First().Value;

            return output;
        }

        private static string getOutput(HttpWebRequest req, LogFile log)
        {
            string output = "";
            try
            {
                using (var response = req.GetResponse())
                {
                    using (var stream = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(1252)))
                    {
                        output = stream.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                log.Write("something went wrong while reading output");
                log.Write(ex.Message);
            }
            return output;
        }
    }

    class LogFile
    {
        private StreamWriter _logFile;

        public LogFile()
        {
            _logFile = new StreamWriter("processTorrents.log", true);
        }

        public void Write(string message)
        {
            _logFile.WriteLine(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + " : " + message);
        }

        public void Close()
        {
            _logFile.Close();
        }
    }

    class Constants
    {
        public const string APIurl = "http://localhost:8088/gui/";
        public const string UserName = "karel";
        public const string PassWord = "enadkphn";
    }
}
