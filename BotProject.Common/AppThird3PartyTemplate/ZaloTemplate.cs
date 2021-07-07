using BotProject.Common.ViewModels;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BotProject.Common.AppThird3PartyTemplate
{
    /*
     * Lưu ý:
     * Template Zalo không có button quickreply như template Facebook,
     * ui card sẽ chuyển button quickreply tới button nằm trong thẻ
     * số button tối đa là 5
     * https://developers.zalo.me/docs/api/official-account-api/phu-luc/cau-truc-cua-tham-so-buttons-post-3965
     */
    public class ZaloTemplate
    {
        public static JObject GetMessageTemplateText(string text, string sender)
        {
            return JObject.FromObject(
                 new
                 {
                     recipient = new { user_id = sender },
                     message = new { text = text },
                 });
        }
        public static JObject GetMessageTemplateImage(string urlImage, string sender)
        {
            return JObject.FromObject(
                 new
                 {
                     recipient = new { user_id = sender },
                     message = new
                     {
                         text = "Hình ảnh",
                         attachment = new
                         {
                             type = "template",
                             payload = new
                             {
                                 template_type = "media",
                                 elements = new[]
                                 {
                                     new
                                     {
                                        media_type = "image",
                                        url = urlImage
                                     }
                                 }
                             }
                         }
                     },
                 });
        }

        public static JObject GetMessageTemplateFile(string token, string sender)
        {
            return JObject.FromObject(
                 new
                 {
                     recipient = new { user_id = sender },
                     message = new
                     {
                         attachment = new
                         {
                             type = "file",
                             payload = new
                             {
                                 token = token
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
                         recipient = new { user_id = sender },
                         message = new
                         {
                             text = text,
                             attachment = new
                             {
                                 type = "template",
                                 payload = new
                                 {
                                     template_type = "button",
                                     buttons = new[]
                                     {
                                         new
                                         {
                                            type = "oa.query.hide",
                                            title = titleQuickReply,
                                            payload = patternQuickReply
                                         },
                                         new
                                         {
                                            type = "oa.query.hide",
                                            title = titleQuickReply2,
                                            payload = patternQuickReply2
                                         }
                                     }
                                 }
                             }
                         },
                     });
            }
            return JObject.FromObject(
                     new
                     {
                         recipient = new { user_id = sender },
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
                         recipient = new { user_id = sender },
                         message = new
                         {
                             text = text,
                             attachment = new
                             {
                                 type = "template",
                                 payload = new
                                 {
                                     template_type = "button",
                                     buttons = new[]
                                     {
                                         new
                                         {
                                            type = "oa.query.hide",
                                            title = titleQuickReply,
                                            payload = patternQuickReply
                                         }
                                     }
                                 }
                             }
                         },
                     });
            }
            return JObject.FromObject(
                     new
                     {
                         recipient = new { user_id = sender },
                         message = new
                         {
                             text = text
                         },
                     });

        }

        public static JObject GetMessageTemplateTextAndButtonLink(string text, string sender, string url, string titleQuickReply)
        {
            if (!String.IsNullOrEmpty(url) && !String.IsNullOrEmpty(titleQuickReply))
            {
                return JObject.FromObject(
                     new
                     {
                         recipient = new { user_id = sender },
                         message = new
                         {
                             text = text,
                             attachment = new
                             {
                                 type = "template",
                                 payload = new
                                 {
                                     template_type = "button",
                                     buttons = new[]
                                     {
                                         new
                                         {
                                            type = "oa.open.url",
                                            title = titleQuickReply,
                                            payload = url
                                         }
                                     }
                                 }
                             }
                         },
                     });
            }
            return JObject.FromObject(
                     new
                     {
                         recipient = new { user_id = sender },
                         message = new
                         {
                             text = text
                         },
                     });

        }

        public static Object GetMessageTemplateGenericByList(string sender, List<SearchNlpQnAViewModel> lstSearchNLP,string urlDetail = "")
        {
            return JObject.FromObject(
              new
              {
                  recipient = new { user_id = sender },
                  message = new
                  {
                      attachment = new
                      {
                          type = "template",
                          payload = new
                          {
                              template_type = "list",
                              elements = from q in lstSearchNLP
                                         select new
                                         {
                                             title = q.question.Substring(0, 60) + "...",
                                             subtitle = "FAQs",
                                             image_url = ConfigHelper.ReadString("Domain") + "assets/images/whatsaquestion.jpg",
                                             default_action = new
                                             {
                                                 type = "oa.open.url",
                                                 url = (String.IsNullOrEmpty(urlDetail) == true ? ConfigHelper.ReadString("Domain") + "home/faq/" + q.id + "" : urlDetail + "-" + q.id + ".html"),
                                             }
                                         }
                          }
                      }
                  },
              });
        }

        public static Object GetMessageTemplateGenericByListMed(string sender, List<SearchSymptomViewModel> lstSearchNLP)
        {
            return JObject.FromObject(
              new
              {
                  recipient = new { user_id = sender },
                  message = new
                  {
                      attachment = new
                      {
                          type = "template",
                          payload = new
                          {
                              template_type = "list",
                              elements = from q in lstSearchNLP
                                         select new
                                         {
                                             title = q.description.Substring(0, 60) + "...",
                                             subtitle = "FAQs",
                                             image_url = ConfigHelper.ReadString("Domain") + "assets/images/medical-logo.jpg",
                                             default_action = new
                                             {
                                                 type = "oa.open.url",
                                                 url = ConfigHelper.ReadString("Domain") + "home/FaqMedSymptoms/" + q.id + "",
                                             }
                                         }
                          }
                      }
                  },
              });
        }

        public static Object GetMessageTemplateGenericByListLegal(string sender, List<LegalApiModel> lstLegalAPI)
        {
            return JObject.FromObject(
              new
              {
                  recipient = new { user_id = sender },
                  message = new
                  {
                      attachment = new
                      {
                          type = "template",
                          payload = new
                          {
                              template_type = "list",
                              elements = from q in lstLegalAPI
                                         select new
                                         {
                                             title = (q.Title.Length > 60 ? q.Title.Substring(0, 60) + "..." : q.Title),
                                             subtitle = "Văn bản luật",
                                             image_url = ConfigHelper.ReadString("Domain") + "assets/images/faq_legal.png",
                                             default_action = new
                                             {
                                                 type = "oa.open.url",
                                                 url = q.Url
                                             }
                                         }
                          }
                      }
                  },
              });
        }

    }
}