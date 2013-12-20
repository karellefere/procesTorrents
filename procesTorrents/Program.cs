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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace processTorrents
{
    class Program
    {
        static void Main(string[] args)
        {
            LogFile log = new LogFile();
            string torrentHash = args[0].ToString();

            if (!string.IsNullOrEmpty(torrentHash))
            {
                uTorrentAPI api = new uTorrentAPI(log, torrentHash);

                var label = api.GetLabel();
                if (label == "sick" || label == "couchpotato")
                {
                    api.StopTorrent();
                    var files = api.GetFiles();
                    var folder = api.GetFolder();
                    foreach (var f in files)
                    {
                        string sourceFile = System.IO.Path.Combine(folder, f);
                        string destFile = System.IO.Path.Combine(Constants.basedir + @"\" + label, f);
                        File.Move(sourceFile, destFile);
                    }
                }
                else
                {
                    log.Write("Do nothing (label: " + label + ")");//wrong label
                }
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
        private string _token;
        private static RestCommunicator rest;
        private string _hash;
        private List<JToken> _torrents;

        public uTorrentAPI(LogFile log, string hash)
        {
            rest = new RestCommunicator(Constants.APIurl, "_utorrent");
            rest.Authenticate(Constants.UserName, Constants.PassWord);
            _log = log;
            _hash = hash;
            _token = GetToken();
            _torrents = GetTorrents(_hash, _token);
        }

        public void StopTorrent()
        {
            NameValueCollection queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["action"] = "stop";
            queryString["hash"] = _hash;
            queryString["token"] = _token;

            var response = rest.Send<string>("?" + queryString.ToString(), RestCommunicator.Method.Get);
        }

        private static List<JToken> GetTorrents(string hash, string token)
        {
            NameValueCollection queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["list"] = "1";
            queryString["hash"] = hash;
            queryString["token"] = token;

            var response = rest.Send<string>("?" + queryString.ToString(), RestCommunicator.Method.Get);

            var o = JObject.Parse(response.Response);
            var JSONTorrents = o["torrents"].ToList();

            return JSONTorrents;
        }

        public string GetFolder()
        {
            var folder = "";
            var JSONTorrents = GetTorrents(_hash, _token);
            foreach (var t in JSONTorrents)
            {
                //check if right torrent
                if (t[0].ToString() == _hash)
                {
                    //get label of torrent
                    folder = t[26].ToString();
                    break;
                }
            }
            return folder;
        }

        public string GetLabel()
        {
            var label = "";
            var JSONTorrents = GetTorrents(_hash, _token);
            foreach (var t in JSONTorrents)
            {
                //check if right torrent
                if (t[0].ToString() == _hash)
                {
                    //get label of torrent
                    label = t[11].ToString();
                    break;
                }
            }
            return label;
        }

        public List<string> GetFiles()
        {
            string[] ignore = { "sample" };
            string[] extensions = { ".mkv", ".avi", ".divx", ".xvid", ".mp4", ".m4v", ".mov", ".mpg", ".mpeg" };
            var files = new List<string>();

            NameValueCollection queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["action"] = "getfiles";
            queryString["hash"] = _hash;
            queryString["token"] = _token;

            var response = rest.Send<string>("?" + queryString.ToString(), RestCommunicator.Method.Get);
            
            var o = JObject.Parse(response.Response);
            var JSONhash = o["files"][0].ToString();
            var JSONfiles = o["files"][1];
            foreach (var f in JSONfiles)
            {
                //check if file contains right extension and doesn't contain ignore things
                if (extensions.Any(f[0].ToString().Contains) && !ignore.Any(f[0].ToString().Contains))
                {
                    //check if fully downloaded
                    if (f[1].ToString() == f[2].ToString())
                    {
                        files.Add(f[0].ToString());
                    }
                }
            }

            return files;
        }

        private static string GetToken()
        {
            var response = rest.Send<string>("token.html", RestCommunicator.Method.Get);
            var xOutput = XDocument.Parse(response.Response);
            var output = xOutput.Descendants("div").First().Value;

            return output;
        }
    }      
}
