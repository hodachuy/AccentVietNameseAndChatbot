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
        private readonly static string _keySpeechRec = Helper.ReadString("KeySpeechReconition");

        public static async Task<string> ConvertSpeechToText(string fileAudio)
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;

            string result = null;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://api.openfpt.vn/");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("api_key", _keySpeechRec);
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Taco2) Gecko/20100101");

                    WebClient web = new WebClient();
                    byte[] byteArray = web.DownloadData(fileAudio);
                    //byte[] byteArray = File.ReadAllBytes(fileAudio);
                    ByteArrayContent bytesContent = new ByteArrayContent(byteArray);
                    var response = await client.PostAsync("fsr", bytesContent);
                    if (response.IsSuccessStatusCode)
                    {
                        result = response.Content.ReadAsStringAsync().Result;
                        //BotLog.Error("ConvertSpeechToText Result: " + result);
                        result = HttpUtility.HtmlDecode(result);                       
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