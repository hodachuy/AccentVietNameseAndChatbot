using Accent.Utils;
using BotProject.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;
using System.Web.Script.Serialization;

namespace BotProject.Web
{
    public sealed class AccentService
    {
        private AccentPredictor accent;
      
        private static readonly Lazy<AccentService> singleInstance = new Lazy<AccentService>(() => new AccentService()); //private static Singleton singleInstance = null;  
        private AccentService()
        {
            accent = new AccentPredictor();
            string _path1Gram = PathServer.PathAccent + "news1gram.bin";
            string _path2Gram = PathServer.PathAccent + "news2grams.bin";
            string _path1Statistic = PathServer.PathAccent + "_1Statistic";
            accent.InitNgram2(_path1Gram, _path2Gram, _path1Statistic);
        }
        public static AccentService SingleInstance
        {
            get
            {
                return singleInstance.Value;
            }
        }

        public string GetAccentVN(string text)
        {
            string result = accent.predictAccents(text);
            return ReplaceWordNoCorrect(result);
        }
        public string GetMultiMatchesAccentVN(string text, int nResults)
        {
            return accent.predictAccentsWithMultiMatches(text, nResults, false);
        }

        private string ReplaceWordNoCorrect(string text)
        {
            if (String.IsNullOrEmpty(text))
                return "";

            var listWordNoCorrect = new Dictionary<string, string>()
            {
                {"số hữu","sở hữu"},
                {"đau đáu","đau đầu"},
            };

            foreach (KeyValuePair<string, string> replacement in listWordNoCorrect)
            {
                text = Regex.Replace(text, replacement.Key, replacement.Value);
            }

            return text;
        }


        // Call API SERVER DIGIPRO
        //public string GetAccentVN(string text)
        //{
        //    string result = "";
        //    using (HttpClient client = new HttpClient())
        //    {
        //        client.BaseAddress = new Uri("https://bot.digipro.vn/");
        //        client.DefaultRequestHeaders.Accept.Clear();
        //        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        //        HttpResponseMessage response = new HttpResponseMessage();
        //        try
        //        {
        //            text = Uri.EscapeUriString(text);
        //            string requestUri = "AccentVN/ConvertVN?text=" + text;
        //            response = client.GetAsync(requestUri).Result;
        //        }
        //        catch (Exception ex)
        //        {
        //            return result;
        //        }
        //        if (response.IsSuccessStatusCode)
        //        {
        //            string resultConvert = response.Content.ReadAsStringAsync().Result;

        //            var resultAccent = new JavaScriptSerializer
        //            {
        //                MaxJsonLength = Int32.MaxValue,
        //                RecursionLimit = 100
        //            }.Deserialize<string>(resultConvert);

        //            return resultAccent;
        //        }
        //    }
        //    return result;


        //    //return accent.predictAccents(text);
        //}
        //public string GetMultiMatchesAccentVN(string text, int nResults)
        //{
        //    string result = "";
        //    using (HttpClient client = new HttpClient())
        //    {
        //        client.BaseAddress = new Uri("https://bot.digipro.vn/");
        //        client.DefaultRequestHeaders.Accept.Clear();
        //        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        //        HttpResponseMessage response = new HttpResponseMessage();
        //        try
        //        {
        //            text = Uri.EscapeUriString(text);
        //            string requestUri = "AccentVN/GetMultiMatchesAccentVN?text=" + text;
        //            response = client.GetAsync(requestUri).Result;
        //        }
        //        catch (Exception ex)
        //        {
        //            return result;
        //        }
        //        if (response.IsSuccessStatusCode)
        //        {
        //            result = response.Content.ReadAsStringAsync().Result;
        //            var resultAccent = new JavaScriptSerializer
        //            {
        //                MaxJsonLength = Int32.MaxValue,
        //                RecursionLimit = 100
        //            }.Deserialize<ReponseAccent>(result);
        //            return string.Join(",", resultAccent.ArrItems);
        //        }
        //    }
        //    return result;
        //    //return accent.predictAccentsWithMultiMatches(text, nResults, false);
        //}

        //Tăng performance tạm thời gọi bên web digipro
        public class ReponseAccent
        {
            public string Item { set; get; }
            public List<string> ArrItems { set; get; }
        }
    }
}