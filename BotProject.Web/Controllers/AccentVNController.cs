using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace BotProject.Web.Controllers
{
    [AllowCrossSite]
    public class AccentVNController : Controller
    {
        private AccentService _accentService;

        public AccentVNController()
        {
            _accentService = AccentService.SingleInstance;
        }
        // GET: AccentVN
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text">Params input phải encode url System.Web.HttpUtility.UrlEncode(text)</param>
        /// <returns></returns>
        public JsonResult ConvertVN(string text)
        {
            string textVN = "";
            if (!String.IsNullOrEmpty(text))
                text = Regex.Replace(HttpUtility.HtmlDecode(text), @"<(.|\n)*?>", "");
            try
            {
                textVN = _accentService.GetAccentVN(text);
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                return Json(message, JsonRequestBehavior.AllowGet);
            }
            return Json(textVN, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetMultiMatchesAccentVN(string text)
        {
            string[] ArrItems;
            if (!String.IsNullOrEmpty(text))
                text = Regex.Replace(HttpUtility.HtmlDecode(text), @"<(.|\n)*?>", "");
            try
            {
                string arrTextVN = _accentService.GetMultiMatchesAccentVN(text, 20);
                ArrItems = arrTextVN.Split(',').Distinct().ToArray();

            }
            catch (Exception ex)
            {
                string message = ex.Message;
                return Json(message, JsonRequestBehavior.AllowGet);
            }
            return Json(ArrItems, JsonRequestBehavior.AllowGet);
        }
    }
}