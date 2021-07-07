using BotProject.Common.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

namespace BotProject.Web.Infrastructure.Core
{
    public class ApiQnaNLRService
    {
        private readonly string UrlAPI = Helper.ReadString("UrlAPI");
        private readonly string KeyAPI = Helper.ReadString("KeyAPI");

        private string apiAddQA = "/api/qa_for_all/pair_add";
        private string apiUpdateQA = "/api/qa_for_all/pair_update";
        private string apiDetailQA = "/api/qa_for_all/pair_get";
        private string apiRelateQA = "/api/qa_for_all/get_related_pair";

        /// <summary>
        /// API KNOWLEDGE BASE CHATBOT - BÀI TOÁN PHÂN LỚP
        /// </summary>
        private string apiKnowledgeBaseAddQA = "/api/chatbot/bot_add";
        private string apiKnowledgeBaseDeleteQA = "/api/chatbot/bot_delete_formqnaid";
        private string apiKnowledgeBaseDeleteSingleQA = "/api/chatbot/bot_delete";
        private string apiKnowledgeBasePrecidictTextClass = "/api/chatbot/text_class";

        /// <summary>
        /// API LẤY THÔNG TIN TRIỆU CHỨNG Y TẾ
        /// </summary>
        private string apiGetSymptoms = "/api/med_sym/get_related";
        private string apiAddSymptoms = "/api/med_sym/add";
        private string apiUpdateSymptoms = "/api/med_sym/update";
        private string apiDeleteSymptoms = "/api/med_sym/delete";


        /// <summary>
        /// API LẤY ĐIỀU LUẬT LIÊN QUAN
        /// </summary>
        private string apiRelateArticle = "/api/get_related_articles";



        /// <summary>
        /// Gọi api chung
        /// </summary>
        /// <param name="NameFuncAPI"></param>
        /// <param name="T"></param>
        /// <param name="Type">
        ///     Add, Update, Delete, Get
        /// </param>
        /// <returns></returns>
        protected string ApiAddUpdateQA(string NameFuncAPI, object T, string Type = "Post")
        {
            string result = null;
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(UrlAPI);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("x-api-key", KeyAPI);
                HttpResponseMessage response = new HttpResponseMessage();
                string json = JsonConvert.SerializeObject(T);
                StringContent httpContent = new StringContent(json, UnicodeEncoding.UTF8, "application/json");
                try
                {
                    switch (Type)
                    {
                        case "Post":
                            response = client.PostAsync(NameFuncAPI, httpContent).Result;
                            break;
                        case "Put":
                            response = client.PutAsJsonAsync(NameFuncAPI, httpContent).Result;
                            break;
                        case "Get":
                            string requestUri = NameFuncAPI;
                            response = client.GetAsync(requestUri).Result;
                            break;
                        case "Delete":
                            string requestUriDelete = NameFuncAPI;
                            response = client.DeleteAsync(requestUriDelete).Result;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    //LMSLog.Error("ApiAddUpdateQA exception. Error message:\r\n", ex);
                    return null;
                }
                if (response.IsSuccessStatusCode)
                {
                    result = response.Content.ReadAsStringAsync().Result;
                }
            }
            return result;
        }

        public string AddQues(string QuesID, string QuestionContent, string AnswerContent, string AreaId, string QuestionHtml, string AnswerHtml, string BotId)
        {
            var param = new { id = QuesID, question = QuestionContent, answer = AnswerContent, field = AreaId, botid = BotId, question_html = QuestionHtml, answer_html = AnswerHtml };
            return ApiAddUpdateQA(apiAddQA, param, "Post");
        }
        public string UpdateQues(string QuesID, string QuestionContent, string AnswerContent, string AreaId, string QuestionHtml, string AnswerHtml, string BotId)
        {
            var param = new { id = QuesID, question = QuestionContent, answer = AnswerContent, field = AreaId, botid = BotId, question_html = QuestionHtml, answer_html = AnswerHtml };
            return ApiAddUpdateQA(apiUpdateQA, param, "Put");
        }

        public string GetRelatedPair(string question, string field, string number, string botId)
        {
            var param = new
            {
                question = question,
                number = number,
                field = field,
                botid = botId
            };
            return ApiAddUpdateQA(apiRelateQA, param, "Post");
        }

        // Function ADD KNOWLEDGE BASE - TEXT CLASSIFICATION
        public string AddKnowledgeQuestion(int botId, int formQnaId, int quesId, string question, string target)
        {
            var param = new
            {
                botid = botId,
                formQnAid = formQnaId,
                id = quesId,
                feature = question,
                target = target,
            };
            return ApiAddUpdateQA(apiKnowledgeBaseAddQA, param, "Post");
        }
        public string DeleteAllKnowledgeByFormId(int formQnaId)
        {
            string urlDelete = apiKnowledgeBaseDeleteQA + "?formQnAid=" + formQnaId;
            return ApiAddUpdateQA(urlDelete, null, "Delete");
        }
        public string DeleteQuestion(int quesId)
        {
            string urlDelete = apiKnowledgeBaseDeleteSingleQA + "?id=" + quesId;
            return ApiAddUpdateQA(urlDelete, null, "Delete");
        }
        public string GetPrecidictTextClass(string text, int botId)
        {
            var param = new
            {
                input = text,
                botid = botId
            };
            return ApiAddUpdateQA(apiKnowledgeBasePrecidictTextClass, param, "Post");
        }

        #region API TRIỆU TRỨNG Y TẾ
        public string GetListSymptoms(string description, int number)
        {
            var param = new
            {
                name_des = description,
                type = "med_sym",
                number = number
            };
            string responseString = ApiAddUpdateQA(apiGetSymptoms, param, "Post");
            return responseString;
        }
        public string AddSymptoms(string symptomsID,
                                  string symptomsName,
                                  string description,
                                  string cause,
                                  string treatment,
                                  string advice,
                                  string symptoms,
                                  string predict,
                                  string protect,
                                  string doctorCanDo)
        {
            var param = new
            {
                id = symptomsID,
                name = symptomsName,
                description = description,
                cause = cause,
                treatment = treatment,
                advice = advice,
                symptom = symptoms,
                predict = predict,
                protect = protect,
                doctorcando = doctorCanDo,
                type = "med_sym"
            };
            return ApiAddUpdateQA(apiAddSymptoms, param, "Post");
        }
        public string UpdateSymptoms(string symptomsID,
                                     string symptomsName,
                                     string description,
                                     string cause,
                                     string treatment,
                                     string advice,
                                     string symptoms,
                                     string predict,
                                     string protect,
                                     string doctorCanDo)
        {
            var param = new
            {
                id = symptomsID,
                name = symptomsName,
                description = description,
                cause = cause,
                treatment = treatment,
                advice = advice,
                symptom = symptoms,
                predict = predict,
                protect = protect,
                doctorcando = doctorCanDo,
                type = "med_sym"
            };
            return ApiAddUpdateQA(apiUpdateSymptoms, param, "Put");
        }
        public string DeleteSymptoms(int symptomsId)
        {
            string urlDelete = apiDeleteSymptoms + "?id=" + symptomsId + "&type=med_sym";
            return ApiAddUpdateQA(urlDelete, null, "Delete");
        }
        #endregion

        #region Lấy điều luật liên quan
        public List<SearchLawArticleViewModel> GetObjectRelatedArticle(string QuestionContent, string number)
        {
            var _lstArt = new List<SearchLawArticleViewModel>();
            var param = new
            {
                content = QuestionContent,
                number = number == null ? "5" : number,
            };
            string responseString = ApiAddUpdateQA(apiRelateArticle, param, "Post");
            if (responseString != null)
            {
                _lstArt = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue, RecursionLimit = 100 }.Deserialize<List<SearchLawArticleViewModel>>(responseString);
                if (_lstArt.Count() != 0)
                {
                    foreach (var item in _lstArt)
                    {
                        item.html = HttpUtility.HtmlDecode(item.html);
                    }
                }
            }
            return _lstArt;
        }


        public List<LegalVm> GetRelatesLegalAndArticle(string content)
        {
            var lstLegalVm = new List<LegalVm>();
            var param = new
            {
                keyword = content,
            };

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://trogiupluat.vn");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage response = new HttpResponseMessage();
                string json = JsonConvert.SerializeObject(param);
                StringContent httpContent = new StringContent(json, UnicodeEncoding.UTF8, "application/json");
                response = client.PostAsync("/LegalDoc/SearchRealtedArticle", httpContent).Result;

                if (response.IsSuccessStatusCode)
                {
                    string responseString = response.Content.ReadAsStringAsync().Result;

                    var result = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue, RecursionLimit = 100 }.Deserialize<SearchLegalViewModel>(responseString);
                    lstLegalVm = result.data;
                }
            }
            return lstLegalVm;
        }
        #endregion
    }
}