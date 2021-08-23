using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Net;
using BotProject.Web.Infrastructure.Log4Net;

namespace BotProject.Web.Infrastructure.Core
{
    public class SpeechReconitionVNService
    {
        public static string ConvertSpeechToText(string fileAudio, bool isLocal = false)
        {
            string result = "";
            using (HttpClient client = new HttpClient())
            {
                string _keySpeechRec = Helper.ReadString("KeySpeechReconition");
                client.DefaultRequestHeaders.Add("api-key", _keySpeechRec);
                WebClient web = new WebClient();
                byte[] byteArray = new byte[] { };
                if (isLocal == false)
                {
                    byteArray = web.DownloadData(fileAudio);
                }
                else
                {
                    byteArray = File.ReadAllBytes(fileAudio);
                }
                ByteArrayContent bytesContent = new ByteArrayContent(byteArray);
                var response = client.PostAsync("https://api.fpt.ai/hmi/asr/general", bytesContent).Result;
                if (response.IsSuccessStatusCode)
                {
                    result = response.Content.ReadAsStringAsync().Result;
                    return HttpUtility.HtmlDecode(result);

                }
            }
            return result;
        }
        public static async Task<string> ConvertSpeechToTextAsync(string fileAudio, bool isLocal = false)
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;
            string _keySpeechRec = Helper.ReadString("KeySpeechReconition");
            string result = "";
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    //client.BaseAddress = new Uri("https://api.fpt.ai/hmi/asr/general");
                    //client.DefaultRequestHeaders.Accept.Clear();
                    //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("api-key", _keySpeechRec);
                    //client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Taco2) Gecko/20100101");
                    WebClient web = new WebClient();
                    byte[] byteArray = new byte[]{ };
                    if (isLocal == false)
                    {
                        byteArray = web.DownloadData(fileAudio);
                    }
                    else
                    {
                        byteArray = File.ReadAllBytes(fileAudio);
                    }
                    
                    ByteArrayContent bytesContent = new ByteArrayContent(byteArray);
                    var response = await client.PostAsync("https://api.fpt.ai/hmi/asr/general", bytesContent);
                    if (response.IsSuccessStatusCode)
                    {
                        result = response.Content.ReadAsStringAsync().Result;
                        //BotLog.Info("ConvertSpeechToText Result: " + result);
                        return HttpUtility.HtmlDecode(result);                       
                    }
                }
            }
            catch(Exception ex)
            {
                BotLog.Error(ex.Message);
            }
            return result;
        }
    }
}