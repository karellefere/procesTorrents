using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
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
            string torrentHash = "46749E4777E7B14077B8E364E00D669BF401F390";//test only change to args[0]

            if (!string.IsNullOrEmpty(torrentHash))
            {
                api.stopTorrent(torrentHash);
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
        private static LogFile _log;
        private static string _token;
        private static RestCommunicator rest;

        public uTorrentAPI(LogFile log)
        {
            rest = new RestCommunicator(Constants.APIurl, "_utorrent");
            rest.Authenticate(Constants.UserName, Constants.PassWord);
            _log = log;
            _token = getToken();
        }

        public void stopTorrent(string hash)
        {
            NameValueCollection queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["action"] = "stop";
            queryString["hash"] = hash;
            queryString["token"] = _token;

            var response = rest.Send<string>("?" + queryString.ToString(), RestCommunicator.Method.Get);
        }

        public string getFiles(string hash)
        {
            NameValueCollection queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["action"] = "getfiles";
            queryString["hash"] = hash;
            queryString["token"] = _token;

            var response = rest.Send<string>("?" + queryString.ToString(), RestCommunicator.Method.Get);
            var files = response.Response;

            return files;
        }

        private static string getToken()
        {
            var response = rest.Send<string>("token.html", RestCommunicator.Method.Get);
            var xOutput = XDocument.Parse(response.Response);
            var output = xOutput.Descendants("div").First().Value;

            return output;
        }
    }      
}
