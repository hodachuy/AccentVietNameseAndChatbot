using BotProject.Common.ViewModels;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BotProject.Common.AppThird3PartyTemplate
{
    public class FacebookTemplate
    {
        public static JObject GetMessageSenderAction(string action, string sender)
        {
            return JObject.FromObject(
                 new
                 {
                     recipient = new { id = sender },
                     sender_action = action,
                 });
        }
        public static JObject GetMessageTemplateText(string text, string sender)
        {
            return JObject.FromObject(
                 new
                 {
                     recipient = new { id = sender },
                     message = new { text = text },
                 });
        }

        public static JObject GetMessageTemplateTextAndButtonLink(string text, string sender, string url, string titleButton, string patternQuickReply = "", string titleQuickReply = "")
        {
            return JObject.FromObject(
                 new
                 {
                     recipient = new { id = sender },
                     message = new
                     {
                         attachment = new
                         {
                             type = "template",
                             payload = new
                             {
                                 template_type = "button",
                                 text = text,
                                 buttons = new[]
                                 {
                                     new
                                     {
                                          type = "web_url",
                                          title = titleButton,
                                          url = url,
                                          webview_height_ratio = "full"
                                     }
                                 }
                             }
                         },
                         quick_replies = new[]
                             {
                                 new
                                 {
                                      content_type = "text",
                                      title = titleQuickReply,
                                      payload = patternQuickReply
                                 }
                             }
                     },
                 });
        }
        public static JObject GetMessageTemplateImage(string urlImage, string sender)
        {
            return JObject.FromObject(
                 new
                 {
                     recipient = new { id = sender },
                     message = new
                     {
                         attachment = new
                         {
                             type = "image",
                             payload = new
                             {
                                 url = urlImage,
                                 is_reusable = true
                             }
                         }
                     },
                 });
        }

        public static JObject GetMessageTemplateTextAndQuickReplyMulti(string text,
                                                                       string sender,
                                                                       string patternQuickReply,
                                                                       string titleQuickReply,
                                                                       string patternQuickReply2,
                                                                       string titleQuickReply2)
        {
            if (!String.IsNullOrEmpty(patternQuickReply) && !String.IsNullOrEmpty(titleQuickReply))
            {
                return JObject.FromObject(
                     new
                     {
                         recipient = new { id = sender },
                         message = new
                         {
                             text = text,
                             quick_replies = new[]
                             {
                                 new
                                 {
                                      content_type = "text",
                                      title = titleQuickReply,
                                      payload = patternQuickReply
                                 },
                                  new
                                 {
                                      content_type = "text",
                                      title = titleQuickReply2,
                                      payload = patternQuickReply2
                                 }
                             }
                         },
                     });
            }
            return JObject.FromObject(
                     new
                     {
                         recipient = new { id = sender },
                         message = new
                         {
                             text = text
                         },
                     });

        }

        public static JObject GetMessageTemplateTextAndQuickReply(string text, string sender, string patternQuickReply, string titleQuickReply)
        {
            if (!String.IsNullOrEmpty(patternQuickReply) && !String.IsNullOrEmpty(titleQuickReply))
            {
                return JObject.FromObject(
                     new
                     {
                         recipient = new { id = sender },
                         message = new
                         {
                             text = text,
                             quick_replies = new[]
                             {
                                 new
                                 {
                                      content_type = "text",
                                      title = titleQuickReply,
                                      payload = patternQuickReply
                                 }
                             }
                         },
                     });
            }
            return JObject.FromObject(
                     new
                     {
                         recipient = new { id = sender },
                         message = new
                         {
                             text = text
                         },
                     });

        }

        public static Object GetMessageTemplateGenericByList(string sender, List<SearchNlpQnAViewModel> lstSearchNLP, string urlDetail = "")
        {
            return JObject.FromObject(
              new
              {
                  recipient = new { id = sender },
                  message = new
                  {
                      attachment = new
                      {
                          type = "template",
                          payload = new
                          {
                              template_type = "generic",
                              elements = from q in lstSearchNLP
                                         select new
                                         {
                                             title = q.question.Substring(0, 60) + "...",
                                             item_url = "",
                                             image_url = ConfigHelper.ReadString("Domain") + "assets/images/whatsaquestion.jpg",
                                             subtitle = "FAQs",
                                             buttons = new[]
                                             {
                                                             new
                                                                {
                                                                   type = "web_url",
                                                                   url = (String.IsNullOrEmpty(urlDetail) == true ? ConfigHelper.ReadString("Domain")+"home/faq/"+q.id+"" : urlDetail + "-"+q.id+".html") ,
                                                                   title = "Xem chi tiết"
                                                                },
                                                        }
                                         }
                          }
                      }
                  },
              });
        }
        public static Object GetMessageTemplateGenericByListMed(string sender, List<SearchSymptomViewModel> lstSearchNLP, string urlDetail = "")
        {
            return JObject.FromObject(
              new
              {
                  recipient = new { id = sender },
                  message = new
                  {
                      attachment = new
                      {
                          type = "template",
                          payload = new
                          {
                              template_type = "generic",
                              elements = from q in lstSearchNLP
                                         select new
                                         {
                                             title = q.description.Substring(0, 60) + "...",
                                             item_url = "",
                                             image_url = ConfigHelper.ReadString("Domain") + "assets/images/medical-logo.jpg",
                                             subtitle = "FAQs",
                                             buttons = new[]
                                             {
                                                             new
                                                                {
                                                                   type = "web_url",
                                                                   url = ConfigHelper.ReadString("Domain")+"home/FaqMedSymptoms/"+q.id+"",
                                                                   title = "Xem chi tiết"
                                                                },
                                                        }
                                         }
                          }
                      }
                  },
              });
        }
        public static Object GetMessageTemplateGenericByListLegal(string sender, List<LegalApiModel> lstLegalAPI, string urlDetail = "", string patternQuickReply = "", string titleQuickReply = "")
        {
            return JObject.FromObject(
              new
              {
                  recipient = new { id = sender },
                  message = new
                  {
                      attachment = new
                      {
                          type = "template",
                          payload = new
                          {
                              template_type = "generic",
                              elements = from q in lstLegalAPI
                                         select new
                                         {
                                             title = (q.Title.Length > 60 ? q.Title.Substring(0, 60) + "..." : q.Title),
                                             item_url = q.Url,
                                             image_url = ConfigHelper.ReadString("Domain") + "assets/images/faq_legal.png",
                                             subtitle = "Văn bản luật",
                                             buttons = new[]
                                             {
                                                             new
                                                                {
                                                                   type = "web_url",
                                                                   url =  q.Url,
                                                                   title = "Xem văn bản"
                                                                },
                                                        }
                                         }
                          }
                      },
                      quick_replies = new[]
                             {
                                 new
                                 {
                                      content_type = "text",
                                      title = titleQuickReply,
                                      payload = patternQuickReply
                                 }
                             }
                  },
              });
        }

    }
}