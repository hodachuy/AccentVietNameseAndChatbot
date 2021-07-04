using AIMLbot.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace AIML.BOT.Utils
{
    public class Render
    {
        private string _color = "";
        private string _srcImageBot = "";
        private TagHtml _tagHtml;
        public Render(string color, string srcImageBot)
        {
            _color = color;
            _srcImageBot = srcImageBot;

            _tagHtml = new TagHtml();
            _tagHtml.TotalBtnPostback = 0;
            _tagHtml.TotalCarousel = 0;
        }
        public TagHtml RenderTagToHtml(string tagName, string outerTagContent, string innerTagContent)
        {
            StringBuilder sb = new StringBuilder();
            string dataText = "";
            switch (tagName)
            {
                case "button":
                    if (outerTagContent.Contains("<postback>"))
                    {
                        string dataPostback = new Regex("<postback>(.*)</postback>", RegexOptions.IgnoreCase).Match(outerTagContent).Groups[1].Value;
                        dataText = new Regex("<text>(.*)</text>", RegexOptions.IgnoreCase).Match(outerTagContent).Groups[1].Value;
                        sb.AppendLine(" <div class=\"_2zgz _2zgz_postback\">");
                        sb.AppendLine("      <div class=\"_4bqf _6biq _6bir\" tabindex=\"0\" role=\"button\" data-postback =\"" + dataPostback + "\" style=\"border-color:" + _color + " color:" + _color + "\">" + dataText + "</div>");
                        sb.AppendLine(" </div>");
                        _tagHtml.Body = "";
                        _tagHtml.ButtonPostback = sb.ToString();
                    }
                    else if (outerTagContent.Contains("<url>"))
                    {
                        dataText = new Regex("<text>(.*)</text>", RegexOptions.IgnoreCase).Match(outerTagContent).Groups[1].Value;
                        string dataUrl = new Regex("<url>(.*)</url>", RegexOptions.IgnoreCase).Match(outerTagContent).Groups[1].Value;
                        dataUrl = new Regex("<!\\[CDATA\\[(.+?)]]>", RegexOptions.IgnoreCase).Match(dataUrl).Groups[1].Value;

                        sb.AppendLine("<div class=\"_6ir5\">");
                        sb.AppendLine("     <div class=\"_4bqf _6ir3\">");
                        sb.AppendLine("          <a class=\"_6ir4 _6ir4_url\" target=\"_blank\" href=\"" + dataUrl + "\" style=\"color:" + _color + "\">" + dataText + "</a>");
                        sb.AppendLine("     </div>");
                        sb.AppendLine("</div>");

                        _tagHtml.Body = sb.ToString();
                    }
                    else if (outerTagContent.Contains("<menu>"))
                    {
                        dataText = new Regex("<text>(.*)</text>", RegexOptions.IgnoreCase).Match(outerTagContent).Groups[1].Value;
                        string dataMenu = new Regex("<menu>(.*)</menu>", RegexOptions.IgnoreCase).Match(outerTagContent).Groups[1].Value;
                        //if (_isFlag)
                        //{
                        //	sb.AppendLine("<div class=\"_6isd _6ir5\">");
                        //}else
                        //{
                        //	sb.AppendLine("<div class=\"_6ir5\">");
                        //}
                        sb.AppendLine("<div class=\"_6ir5\">");
                        sb.AppendLine("     <div class=\"_4bqf _6ir3\">");
                        sb.AppendLine("          <a class=\"_6ir4 _6ir4_menu\" data-postback =\"" + dataMenu + "\" href=\"#\" style=\"color:" + _color + "\">" + dataText + "</a>");
                        sb.AppendLine("     </div>");
                        sb.AppendLine("</div>");
                        //_isFlag = false;
                        _tagHtml.Body = sb.ToString();
                    }
                    else if (outerTagContent.Contains("<module>"))
                    {
                        dataText = new Regex("<text>(.*)</text>", RegexOptions.IgnoreCase).Match(outerTagContent).Groups[1].Value;
                        string dataModule = new Regex("<module>(.*)</module>", RegexOptions.IgnoreCase).Match(outerTagContent).Groups[1].Value;
                        //if (_isFlag)
                        //{
                        //	sb.AppendLine("<div class=\"_6isd _6ir5\">");
                        //}else
                        //{
                        //	sb.AppendLine("<div class=\"_6ir5\">");
                        //}
                        sb.AppendLine("<div class=\"_6ir5\">");
                        sb.AppendLine("     <div class=\"_4bqf _6ir3\">");
                        sb.AppendLine("          <a class=\"_6ir4 _6ir4_menu\" data-postback =\"" + dataModule + "\" href=\"#\" style=\"color:" + _color + "\">" + dataText + "</a>");
                        sb.AppendLine("     </div>");
                        sb.AppendLine("</div>");
                        //_isFlag = false;
                        _tagHtml.Body = sb.ToString();
                    }
                    break;
                case "link":
                    dataText = new Regex("<text>(.*)</text>", RegexOptions.IgnoreCase).Match(outerTagContent).Groups[1].Value;
                    string dataLink = new Regex("<url>(.*)</url>", RegexOptions.IgnoreCase).Match(outerTagContent).Groups[1].Value;
                    dataLink = new Regex("<!\\[CDATA\\[(.+?)]]>", RegexOptions.IgnoreCase).Match(dataLink).Groups[1].Value;
                    sb.AppendLine("<div class=\"_6j0y\">");
                    sb.AppendLine("    <a target=\"_blank\" href=\"" + dataLink + "\">");
                    sb.AppendLine("           " + dataText + "");
                    sb.AppendLine("    </a>");
                    sb.AppendLine("</div>");
                    _tagHtml.Body = sb.ToString();
                    break;
                case "image":
                    //Common.ReadString("Domain") host domain lay tu`appconfig cua domain cha truyen` vao`
                    string dataImage = new Regex("<image>(.*)</image>", RegexOptions.IgnoreCase).Match(outerTagContent).Groups[1].Value;
                    sb.AppendLine("<div class=\"_6j0s _6popup_image\" style=\"background-image:url(&quot;" + Common.ReadString("Domain") + dataImage + "&quot;); background-position: center center; height: 150px; width: 100%;\"></div>");
                    _tagHtml.Body = sb.ToString();
                    break;
                case "file":
                    string dataFile = new Regex("<file>(.*)</file>", RegexOptions.IgnoreCase).Match(outerTagContent).Groups[1].Value;
                    string urlFile = Common.ReadString("Domain") + HttpUtility.HtmlDecode(dataFile);
                    string extension = System.IO.Path.GetExtension(urlFile);
                    string iconFile = "<i class=\"fa fa-file\" aria-hidden=\"true\" style=\"color:cornflowerblue;padding-right:10px\"></i>";
                    if (extension == ".doc" || extension == ".docx")
                    {
                        iconFile = "<i class=\"fa fa-file-word\" aria-hidden=\"true\" style=\"color:cornflowerblue;padding-right:10px\"></i>";
                        //iconFile = "<i alt=\"\" class=\"img sp_y5OyqnCywpJ sx_df7216\"></i>";
                    }
                    else if (extension == ".pdf")
                    {
                        iconFile = "<i class=\"fa fa-file-pdf\" aria-hidden=\"true\" style=\"color:red;padding-right:10px\"></i>";
                        //iconFile = "<i alt=\"\" class=\"img sp_y5OyqnCywpJ sx_df7216\"></i>";
                    }
                    //string nameFile = new Regex("(?:[^/][\\d\\w\\.]+)$(?<=(?:.jpg)|(?:.pdf)|(?:.gif)|(?:.jpeg)|(?:.txt)|(?:.doc)|(?:.docx)|(more_extension))", RegexOptions.IgnoreCase).Match(urlFile).Value;
                    string filename = System.IO.Path.GetFileName(urlFile.Split('/').Last());
                    sb.AppendLine("<div class=\"_4xko _4xkr _tmpB\" tabindex=\"0\" role=\"button\" style=\"background-color:" + _color + "; font-family: Segoe UI Light;\"><span>" + iconFile + "<a href='" + urlFile + "' target='_blank' style=\"text-decoration:none\">" + filename + "</a></span></div>");
                    _tagHtml.Body = sb.ToString();
                    break;
                case "title":
                    string dataTitle = new Regex("<title>(.*)</title>", RegexOptions.IgnoreCase).Match(outerTagContent).Groups[1].Value;
                    sb.AppendLine("<div class=\"_6j0t _4ik4 _4ik5 _6j0t_title\" style=\"-webkit-line-clamp: 3;\">");
                    sb.AppendLine("" + dataTitle + "");
                    sb.AppendLine("</div>");
                    _tagHtml.Body = sb.ToString();

                    break;
                case "subtitle":
                    string dataSubTitle = new Regex("<subtitle>(.*)</subtitle>", RegexOptions.IgnoreCase).Match(outerTagContent).Groups[1].Value;
                    sb.Append("<div class=\"_6j0v _6j0v_subtitle\">");
                    sb.AppendLine(" <div class=\"_6j0u _6j0w\">");
                    sb.AppendLine("    " + dataSubTitle + "");
                    sb.AppendLine(" </div>");
                    sb.AppendLine(" <div class=\"_6j0u _6j0x _4ik4 _4ik5\" style=\"-webkit-line-clamp: 2;\">");
                    sb.AppendLine("    <div>");
                    sb.AppendLine("    " + dataSubTitle + "");
                    sb.AppendLine("    </div>");
                    sb.AppendLine(" </div>");
                    sb.AppendLine(" </div>");
                    _tagHtml.Body = sb.ToString();
                    break;
                default:
                    _tagHtml.Body = String.Empty;
                    break;
            }
            if (tagName == "card")
            {
                XmlNode resultNode = AIMLTagHandler.getNode("<node>" + innerTagContent + "</node>");
                if (resultNode.HasChildNodes)
                {
                    StringBuilder sbBtnPostback = new StringBuilder();
                    StringBuilder sbCard = new StringBuilder();
                    sbCard.AppendLine("<div class=\"_6j2i\">");
                    string htmlCard = "";
                    foreach (XmlNode cNode in resultNode.ChildNodes)
                    {
                        htmlCard = RenderTagToHtml(cNode.Name, cNode.OuterXml, "").Body;
                        if (cNode.Name == "title")
                        {
                            htmlCard = "<div class=\"_6j2g\">" + htmlCard;
                        }
                        if (cNode.Name == "link")
                        {
                            htmlCard = htmlCard + "</div>";
                        }
                        if (cNode.Name == "button")
                        {
                            if (cNode.OuterXml.Contains("<postback>"))
                            {
                                _tagHtml.TotalBtnPostback = _tagHtml.TotalBtnPostback + 1;
                            }
                        }
                        sbCard.AppendLine(htmlCard);
                        sbBtnPostback.AppendLine(RenderTagToHtml(cNode.Name, cNode.OuterXml, "").ButtonPostback);
                    }

                    sbCard.AppendLine("</div>");
                    _tagHtml.Body = sbCard.ToString();
                    _tagHtml.ButtonPostback = sbBtnPostback.ToString();
                }
            }
            if (tagName == "carousel")
            {
                XmlNode resultNode = AIMLTagHandler.getNode("<node>" + innerTagContent + "</node>");
                StringBuilder sbCarousel = new StringBuilder();
                if (resultNode.HasChildNodes)
                {
                    foreach (XmlNode cNode in resultNode.ChildNodes)
                    {
                        sbCarousel.AppendLine("<div class=\"_2zgz\"> <div class=\"_6j2h\">" + RenderTagToHtml(cNode.Name, cNode.OuterXml, cNode.InnerXml).Body + "</div></div>");
                        _tagHtml.TotalCarousel = _tagHtml.TotalCarousel + 1;
                    }
                    _tagHtml.Body = sbCarousel.ToString();
                }
            }
            return _tagHtml;
        }



    }
}
