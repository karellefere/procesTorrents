using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Net;
using System.IO;
namespace processTorrents
{
    /// <summary>
    /// Summary description for RestCommunicator
    /// </summary>
    public class RestCommunicator
    {
        public enum Method
        {
            Get = 0,
            Post = 1,
            Put = 2,
            Patch = 3,
            /// <summary>
            ///  Hyper Text Coffee Pot Control Protocol (http://www.ietf.org/rfc/rfc2324.txt)
            /// </summary>
            Brew = 4
        }

        public string BaseURL { get; private set; }
        public string SaveModule { get; private set; }
        public Dictionary<string, string> headers;
        private string username;
        private string password;
        private string content_type = "";

        private CookieContainer cookieContainer;

        public RestCommunicator(string baseURL, string saveModule)
        {
            BaseURL = baseURL;
            SaveModule = saveModule;
            cookieContainer = new CookieContainer();
            headers = new Dictionary<string, string>();
        }

        public RestResponse<TOut> Send<TIn, TOut>(TIn message, string urlActionPart, Method method)
            where TIn : class
            where TOut : class
        {
            string toUrl = BaseURL;
            //join urlActionPart to BaseURL
            if (!string.IsNullOrEmpty(urlActionPart))
            {
                if (!BaseURL.EndsWith("/"))
                    toUrl += "/" + urlActionPart;
                else
                    toUrl += urlActionPart;
            }

            //add message to url
            if (method == Method.Get)
            {
                if (message != null)
                {
                    var sMessage = Serialize(message);
                    var seperator = toUrl.Contains('?') ? "&" : "?";
                    toUrl += seperator + sMessage;
                }
            }

            var request = (HttpWebRequest)WebRequest.Create(toUrl);
            request.CookieContainer = cookieContainer;

            // add credentials
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var credCache = new CredentialCache();

                credCache.Add(request.RequestUri, "Basic", new NetworkCredential(username, password));
                request.Credentials = credCache;
            }

            // add headers
            foreach (var header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            
            if (method == Method.Get)
            {
                //save url for get
            }
            else
            {
                //set correct message for post
                switch (method)
                {
                    case Method.Post:
                        request.Method = "POST";
                        break;
                    case Method.Put:
                        request.Method = "PUT";
                        break;
                    case Method.Patch:
                        request.Method = "PATCH";
                        break;
                    case Method.Brew:
                        // http://www.ietf.org/rfc/rfc2324.txt
                        request.Method = "BREW";
                        break;
                }
                string sMessage;
                //if message is string, set content to form
                if (message is string)
                {
                    sMessage = (string)(object)message;
                    request.ContentType = "application/x-www-form-urlencoded";
                }
                else
                {
                    //else set it to xml
                    sMessage = Serialize(message);
                    request.ContentType = "text/xml";
                }

                using (var rStream = request.GetRequestStream())
                using (var rwStream = new StreamWriter(rStream))
                {
                    rwStream.Write(sMessage);
                    rwStream.Close();
                    rStream.Close();
                }
            }

            request.Accept = "application/xml, */*";

            if (!string.IsNullOrEmpty(content_type))
            {
                request.ContentType = content_type;
            }

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    //parse success-response
                    return ParseResponse<TOut>(response);
                }
            }
            catch (WebException ex)
            {
                //try to parse error-response
                var response = ex.Response as HttpWebResponse;
                if (response != null)
                {
                    return ParseResponse<TOut>(response);
                }
                throw;
            }
        }

        private RestResponse<T> ParseResponse<T>(HttpWebResponse response)
            where T : class
        {
            using (var str = response.GetResponseStream())
            using (var strstr = new StreamReader(str))
            {
                var sMessage = strstr.ReadToEnd();
                try
                {
                    return new RestResponse<T>(response.StatusCode, Deserialize<T>(sMessage));
                }
                catch (Exception ex)
                {
                    //deserializing failed, throw exception with raw response (for easier debugging)
                    throw new RestException(sMessage, response.StatusCode, ex);
                }
            }
        }


        public RestResponse<TOut> Send<TOut>(string urlActionPart, Method method)
            where TOut : class
        {
            return Send<object, TOut>(null, urlActionPart, method);
        }

        public HttpStatusCode Send<TIn>(TIn message, string urlActionPart, Method method)
            where TIn : class
        {
            return Send<TIn, string>(message, urlActionPart, method).StatusCode;
        }

        private string Serialize<T>(T message)
            where T : class
        {
            if (message is string)
            {
                return (string)(object)message;
            }

            if (message is System.Xml.XmlDocument)
            {
                return ((System.Xml.XmlDocument)(object)message).OuterXml;
            }

            if (message is System.Xml.Linq.XDocument)
            {
                return ((System.Xml.Linq.XDocument)(object)message).ToString();
            }

            var f = new System.Xml.Serialization.XmlSerializer(message.GetType());

            var xml = new System.Text.StringBuilder(1024);
            using (var wr = System.Xml.XmlWriter.Create(xml))
            {
                f.Serialize(wr, message);
                wr.Close();
            }
            return xml.ToString();
        }

        private T Deserialize<T>(string message)
            where T : class
        {
            if ("" is T)
            {
                return (T)(object)message;
            }

            var t = typeof(T);

            if (t == typeof(System.Xml.XmlDocument))
            {
                var x = new System.Xml.XmlDocument();
                x.LoadXml(message);
                return (T)(object)x;
            }

            if (t == typeof(System.Xml.Linq.XDocument))
            {
                return (T)(object)System.Xml.Linq.XDocument.Parse(message);
            }

            var f = new System.Xml.Serialization.XmlSerializer(t);
            using (var reader = new StringReader(message))
            {
                return (T)f.Deserialize(reader);
            }
        }

        public void setContentType(string content_type)
        {
            this.content_type = content_type;
        }

        public void Authenticate(string username, string password)
        {
            this.username = username;
            this.password = password;
        }

        public class RestResponse<T>
            where T : class
        {
            public HttpStatusCode StatusCode { get; private set; }
            public T Response { get; private set; }

            public RestResponse(HttpStatusCode statusCode, T response)
            {
                StatusCode = statusCode;
                Response = response;
            }
        }

        public class RestException : Exception
        {
            public string RawResponse { get; private set; }
            public HttpStatusCode StatusCode { get; private set; }

            public RestException(string rawResponse, HttpStatusCode statusCode, Exception baseException)
                : base("Error deserializing REST response", baseException)
            {
                RawResponse = rawResponse;
                StatusCode = statusCode;
            }
        }
    }
}


