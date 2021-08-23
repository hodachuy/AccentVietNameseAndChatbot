using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accent.Utils;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Specialized;
using System.Dynamic;
using System.ComponentModel;
using System.Globalization;
using Newtonsoft.Json;
using System.Web;
using System.Security.Cryptography;
using Accent.ConsoleApplication.SmsService;
using System.Xml;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using Quartz;
using Quartz.Impl;
using System.IO;
using System.Web.Script.Serialization;
using System.Threading;
using static System.Console;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;

using ZaloDotNetSDK;

namespace Accent.ConsoleApplication
{
    class Program
    {
        /// <summary>
        /// Run test vietnamese accent
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            

            string fileUrl = @"D:\Bot\BotProject.Web\File\Document\TNKS_MS02.doc";
            //string fileAudio = "https://f1.voice.talk.zdn.vn/4652432487571062114/49eab5d7dac7319968d6.amr";
            string fileAudio = "D:\\b3b35609-3fc2-4ef9-a365-48417bd400e9.wav";
            using (HttpClient client = new HttpClient())
            {
                //client.BaseAddress = new Uri("https://api.fpt.ai/hmi/asr/general");
                //client.DefaultRequestHeaders.Accept.Clear();
                //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("api-key", "wMgI3aH5BUD06hkRMr1i5IBr4l3guzM8");
                //client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Taco2) Gecko/20100101");

                WebClient web = new WebClient();
                //byte[] byteArray = web.DownloadData(fileAudio);
                byte[] byteArray = File.ReadAllBytes(fileAudio);
                ByteArrayContent bytesContent = new ByteArrayContent(byteArray);
                var response = client.PostAsync("https://api.fpt.ai/hmi/asr/general", bytesContent).Result;
                if (response.IsSuccessStatusCode)
                {
                    string result = response.Content.ReadAsStringAsync().Result;
                    //BotLog.Info("ConvertSpeechToText Result: " + result);
                    result = HttpUtility.HtmlDecode(result);
                    dynamic stuff = JsonConvert.DeserializeObject(result);
                    string status = stuff.status;
                    string text32 = stuff.hypotheses[0].utterance;

                }
            }


            //var T = Task.Run(() => Tesst());
            //var z54655 = T.Result;

            //ZaloClient client = new ZaloClient("zQptDH_SInZxelyr4zf36lwb_pSQmqjx_zoGJtQaSs3fX8SO1RHfUBgfqr4Dkmfuue7_2a_x9bBTwfqOLzD0Ckxlgr5vxqa_aD6HQ2FDVm2FpfykDSuhNwFfx1OnsoXykjpD30oiDNc-dVSE3eG89xcgysG0a4m_XfE0O0pkGqcguA8MDVe2TQtglHmNyaX2eFE3AGhSEcUreEKX4uac38opxLGRZHCkawciRIA3U3-hhwKJBuWHPvIPsH8SbX16fft871kU87UCYVa17_WaMwhZomq2uXGat4YD6HhAIny");

            //ZaloFile file = new ZaloFile(fileUrl);
            //file.setMediaTypeHeader("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            //JObject result = client.uploadFileForOfficialAccountAPI(file);
            //string zx = result["data"]["token"].ToString();
            //var xcx = zx;
            //var task = Task.Run<string>(async () => await Tesst());
            //string cx = task.Result;

            var multiForm = new MultipartFormDataContent();
            FileStream fs = File.OpenRead(fileUrl);
            string fName = System.IO.Path.GetFileName(fileUrl);

            multiForm.Add(new StreamContent(new MemoryStream(File.ReadAllBytes(fileUrl))), "file", System.IO.Path.GetFileName(fileUrl));

            //multiForm.Add(new StreamContent(fs), "file", System.IO.Path.GetFileName(fileUrl));
            string fileToken = "";
            using (HttpClient _client = new HttpClient())
            {
                // send request to API
                var url = "https://openapi.zalo.me/v2.0/oa/upload/file?access_token=zQptDH_SInZxelyr4zf36lwb_pSQmqjx_zoGJtQaSs3fX8SO1RHfUBgfqr4Dkmfuue7_2a_x9bBTwfqOLzD0Ckxlgr5vxqa_aD6HQ2FDVm2FpfykDSuhNwFfx1OnsoXykjpD30oiDNc-dVSE3eG89xcgysG0a4m_XfE0O0pkGqcguA8MDVe2TQtglHmNyaX2eFE3AGhSEcUreEKX4uac38opxLGRZHCkawciRIA3U3-hhwKJBuWHPvIPsH8SbX16fft871kU87UCYVa17_WaMwhZomq2uXGat4YD6HhAIny";
                var response = _client.PostAsync(url, multiForm).Result;
                if (response.IsSuccessStatusCode)
                {
                    var result = new JavaScriptSerializer
                    {
                        MaxJsonLength = Int32.MaxValue,
                        RecursionLimit = 100
                    }
                    .Deserialize<dynamic>(response.Content.ReadAsStringAsync().Result);
                    fileToken = result["message"];
                }
            }






            string z32 = "1";
            //string legalCode = "thông tư 62/QĐ-BCDCCTLBHXH abc";

            //string rep = Regex.Replace(legalCode.ToLower(), @"(\d{1,5}\/)?\d{1,5}(\-\w+)?\/([a-zA-ZĐ\-]+)", (m) => {
            //    if (m.Value.Contains("d"))
            //    {
            //        return m.Value.Replace("d", "đ");
            //    }
            //    return m.Value;
            //}, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            string text = "{\"recipient\":{\"user_id\":\"{{senderId}}\"},\"message\":{\"attachment\":{\"type\":\"file\",\"payload\":{\"url\":\"File/Document/Benh-an-Noi-khoa.docx\"}}}}";
            string r1 = "";
            Regex rChaptEnd = new Regex("url\":\"(.+?)}", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Match _chaptEndContent = rChaptEnd.Match(text);
            if (_chaptEndContent.Success)
            {
                r1 = _chaptEndContent.Groups[1].Value;
            }


            string x = "chào bạn";
            string x2 = "chào bạn";
            string x3 = "chao ban";
            bool isUnicode = false;
            if (x.Any(c => c > 255))
            {
                isUnicode = true;
            }
            if (x2.Any(c => c > 255))
            {
                isUnicode = true;
            }
            if (x3.Any(c => c > 255))
            {
                isUnicode = true;
            }

            var z = 1;
            //var lstAIMLBOT = new List<Tuple<Bot, string>>();

            //lstAIMLBOT.Add(new Tuple<Bot, string>(new Bot(), "1"));
            //lstAIMLBOT.Add(new Tuple<Bot, string>(new Bot(), "2"));
            //lstAIMLBOT.Add(new Tuple<Bot, string>(new Bot(), "3"));

            //string botId = "2";
            //for(int i = 0; i < lstAIMLBOT.Count; i ++)
            //{
            //    if(lstAIMLBOT[i].Item2 == botId)
            //    {
            //        var z = "2";
            //    }
            //}
            //var x1 = lstAIMLBOT[0].Item1;
            //var x2 = lstAIMLBOT[1].Item1;
            //var x3 = lstAIMLBOT[2].Item1;


            //LoadBalancer b1 = LoadBalancer.GetLoadBalancer();
            //LoadBalancer b2 = LoadBalancer.GetLoadBalancer();
            //LoadBalancer b3 = LoadBalancer.GetLoadBalancer();
            //LoadBalancer b4 = LoadBalancer.GetLoadBalancer();

            //// Same instance?

            //if (b1 == b2 && b2 == b3 && b3 == b4)
            //{
            //    Console.WriteLine("Same instance\n");
            //}

            //// Load balance 15 server requests
            //LoadBalancer balancer = LoadBalancer.GetLoadBalancer();

            //for (int i = 0; i < 15; i++)
            //{
            //    string server = balancer.Server;
            //    Console.WriteLine("Dispatch Request to: " + server);
            //}

            //// Wait for user

            //Console.ReadKey();


            //AccentPredictor accent = new AccentPredictor();

            //string path1Gram = System.IO.Path.GetFullPath("news1gram.bin");
            //string path2Gram = System.IO.Path.GetFullPath("news2grams.bin");
            //string path1Statistic = System.IO.Path.GetFullPath("_1Statistic");
            //accent.InitNgram2(path1Gram, path2Gram, path1Statistic);

            //Console.OutputEncoding = Encoding.UTF8;
            ////-----Test---- -//
            //Console.WriteLine("Accuary: " + accent.getAccuracy(System.IO.Path.GetFullPath("test.txt")) + "%");

            //while (true)
            //{
            //    Console.InputEncoding = Encoding.Unicode;
            //    Console.OutputEncoding = Encoding.UTF8;
            //    Console.WriteLine("Nhap chuoi :");
            //    string text = Console.ReadLine();
            //    if (text == "exit")
            //    {
            //        break;
            //    }
            //    if (text == "1")
            //    {
            //        accent.InitNgram2(path1Gram, path2Gram, path1Statistic);
            //    }
            //    string results = accent.predictAccentsWithMultiMatches(text, 10);
            //    Console.WriteLine("DS Ket qua : {0}", results);

            //    Console.WriteLine("Ket qua : {0}", accent.predictAccents(text));
            //}
        }

        public static async Task<string> Tesst()
        {
            string Seshat_URL = "https://openapi.zalo.me/v2.0/oa/upload/file?access_token=zQptDH_SInZxelyr4zf36lwb_pSQmqjx_zoGJtQaSs3fX8SO1RHfUBgfqr4Dkmfuue7_2a_x9bBTwfqOLzD0Ckxlgr5vxqa_aD6HQ2FDVm2FpfykDSuhNwFfx1OnsoXykjpD30oiDNc-dVSE3eG89xcgysG0a4m_XfE0O0pkGqcguA8MDVe2TQtglHmNyaX2eFE3AGhSEcUreEKX4uac38opxLGRZHCkawciRIA3U3-hhwKJBuWHPvIPsH8SbX16fft871kU87UCYVa17_WaMwhZomq2uXGat4YD6HhAIny";
            string fileUrl = @"D:\Bot\BotProject.Web\File\Document\TNKS_MS02.doc";

            using (var httpClient = new HttpClient())
            {
                using (var form = new MultipartFormDataContent())
                {
                    using (var fs = File.OpenRead(fileUrl))
                    {
                        using (var streamContent = new StreamContent(fs))
                        {
                            using (var fileContent = new ByteArrayContent(await streamContent.ReadAsByteArrayAsync()))
                            {
                                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/msword");
                                // "file" parameter name should be the same as the server side input parameter name
                                form.Add(fileContent, "file", System.IO.Path.GetFileName(fileUrl));
                                HttpResponseMessage response = await httpClient.PostAsync(Seshat_URL, form);
                                if (response.IsSuccessStatusCode)
                                {
                                    var result = new JavaScriptSerializer
                                    {
                                        MaxJsonLength = Int32.MaxValue,
                                        RecursionLimit = 100
                                    }
                                    .Deserialize<dynamic>(response.Content.ReadAsStringAsync().Result);
                                    return result["message"];
                                }
                            }
                        }
                    }
                }
            }
            return "";
        }

        class LoadBalancer
        {
            private static LoadBalancer _instance;
            private List<string> _servers = new List<string>();
            private Random _random = new Random();

            // Lock synchronization object

            private static object syncLock = new object();

            // Constructor (protected)

            protected LoadBalancer()
            {
                // List of available servers
                _servers.Add("ServerI");
                _servers.Add("ServerII");
                _servers.Add("ServerIII");
                _servers.Add("ServerIV");
                _servers.Add("ServerV");
            }

            public static LoadBalancer GetLoadBalancer()
            {
                // Support multithreaded applications through

                // 'Double checked locking' pattern which (once

                // the instance exists) avoids locking each

                // time the method is invoked

                if (_instance == null)
                {
                    lock (syncLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new LoadBalancer();
                        }
                    }
                }

                return _instance;
            }

            // Simple, but effective random load balancer
            public string Server
            {
                get
                {
                    int r = _random.Next(_servers.Count);
                    return _servers[r].ToString();
                }
            }
        }
    }
}
