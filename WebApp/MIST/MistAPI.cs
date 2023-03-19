using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;



namespace GCS
{
    [Serializable]
    public class MistAPI
    {
        public string EndPoint { get; set; }
      

        public MistAPI()
        {

        }

        public void SaveConfig()
        {
            StringBuilder json = new StringBuilder();
            json.Append("{\"save\":\"");
            json.Append(" ");
            json.Append("\"}");
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            dynamic test = serializer.Deserialize<object>(json.ToString());
            string junk = serializer.Serialize(test);
             Post(junk);
        }


        public bool DeleteStream(string name)
        {
            StringBuilder json = new StringBuilder();
            json.Append("{\"deletestream\":\"");
            json.Append(name);
            json.Append("\"}");
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            dynamic test = serializer.Deserialize<object>(json.ToString());
            string junk = serializer.Serialize(test);

            var result = Post(junk);
            dynamic[] arr = JObject.Parse(result)["log"].ToObject<dynamic[]>();
            return arr[arr.Length - 1].Root.ToString().Contains("Deleted stream " + name);
        }

        public bool AddStream(string name, string source)
        {
            
            StringBuilder json = new StringBuilder();
            json.Append("{");
            json.Append("\"addstream\": {\"");
            json.Append(name);
            json.Append("\": {\"source\":\"");
            json.Append(source);
            json.Append("\"}}}");
            
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            dynamic test = serializer.Deserialize<object>(json.ToString());
            string entry = string.Empty;
            bool rtn = true;

            var result = Post(json.ToString());
            return rtn;  
        }

        public dynamic GetStreams()
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string json = @"{""capabilities"": true}";
            var result = Post(json);

            dynamic item = serializer.Deserialize<object>(result);
            var streams = item["streams"];
            return streams;

        }

        public string Post(string json)
        {
            string rtn = string.Empty;
            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(EndPoint);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                httpWebRequest.Timeout = 1000 * 60 * 5;
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write("command=");
                    streamWriter.Write(json);
                }
                
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    rtn = streamReader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Mist POST API: " + ex.Message);
            }

            return rtn;
        }
    }

    [Serializable]
    public class MistStream
    {
        public string Error { get; set; }
        public string Name { get; set; }
        public int Online { get; set; }
        public string Source { get; set; }
    }

}
