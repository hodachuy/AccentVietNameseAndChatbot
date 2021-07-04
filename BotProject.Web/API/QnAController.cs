using BotProject.Web.Infrastructure.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using BotProject.Service;
using BotProject.Web.Models;
using BotProject.Model.Models;
using BotProject.Web.Infrastructure.Extensions;
using System.Configuration;
using System.IO;
using System.Text;
using BotProject.Common;
using AutoMapper;
using BotProject.Common.ViewModels;
using Newtonsoft.Json.Linq;

namespace BotProject.Web.API
{
    [RoutePrefix("api/qna")]
    public class QnAController : ApiControllerBase
    {
        private IQnAService _qnaService;
        private IAIMLFileService _aimlService;
        private ApiQnaNLRService _apiNLR;
        private IBotService _botService;
        private ICardService _cardService;
        private string[] _userSayStart = new string[]
        {
            CommonConstants.UserSay_IsStartDefault,
            CommonConstants.UserSay_IsStartFirst,
            CommonConstants.UserSay_IsStartLast,
            CommonConstants.UserSay_IsStartDouble
        };
        public QnAController(IErrorService errorService,
                            IQnAService qnaService,
                            IAIMLFileService aimlService,
                            ICardService cardService,
                            IBotService botService) : base(errorService)
        {
            _qnaService = qnaService;
            _aimlService = aimlService;
            _apiNLR = new ApiQnaNLRService();
            _botService = botService;
            _cardService = cardService;
        }

        [Route("create")]
        [HttpPost]
        public HttpResponseMessage Create(HttpRequestMessage request, FormQnACommonViewModel formQnAVm)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                bool IsCreated = false;
                if (formQnAVm != null && formQnAVm.QuestionGroupViewModels != null
                                    && formQnAVm.QuestionGroupViewModels.Count() != 0)
                {
                    var formQnaDb = _qnaService.GetFormQnAnswerById(formQnAVm.FormQuestionAnswerID);
                    formQnaDb.Status = formQnAVm.Status;
                    _qnaService.UpdateFormQuestionAnswer(formQnaDb);
                    _qnaService.Save();

                    var lstQuesGroupVm = formQnAVm.QuestionGroupViewModels;
                    // nếu trường hợp cập nhật chỉ thêm các question group với id rỗng
                    if (formQnAVm.TypeAction == Common.CommonConstants.UpdateQnA)
                    {
                        //lstQuesGroupVm = formQnAVm.QuestionGroupViewModels.Where(x => x.ID == 0);
                        var lstQuesGroupDb = _qnaService.GetListQuestionGroupByFormQnAnswerID(formQnAVm.FormQuestionAnswerID).ToList();
                        if (lstQuesGroupDb != null && lstQuesGroupDb.Count() != 0)
                        {
                            foreach (var quesGroup in lstQuesGroupDb)
                            {
                                _qnaService.DeleteQuesByQuestionGroup(quesGroup.ID);
                                _qnaService.DeleteAnswerByQuestionGroup(quesGroup.ID);
                            }
                            _qnaService.DeleteMultiQuestionGroupByFormID(formQnAVm.FormQuestionAnswerID);
                            _qnaService.Save();

                            // delete all knowledge base qna mongoDb
                            _apiNLR.DeleteAllKnowledgeByFormId(formQnAVm.FormQuestionAnswerID);
                        }
                    }
                    if (lstQuesGroupVm != null && lstQuesGroupVm.Count() != 0)
                    {
                        foreach (var itemQGroup in lstQuesGroupVm)
                        {
                            try
                            {
                                QuestionGroup qGroupDb = new QuestionGroup();
                                qGroupDb.UpdateQuestionGroup(itemQGroup);
                                _qnaService.AddQuesGroup(qGroupDb);
                                _qnaService.Save();
                                if (itemQGroup.QnAViewModel.QuestionViewModels != null &&
                                        itemQGroup.QnAViewModel.QuestionViewModels.Count() != 0)
                                {
                                    var lstQuestion = itemQGroup.QnAViewModel.QuestionViewModels;
                                    foreach (var itemQues in lstQuestion)
                                    {
                                        string code = qGroupDb.ID + Guid.NewGuid().ToString();
                                        if (itemQues.IsThatStar == false)
                                        {
                                            Question quesDb = new Question();
                                            quesDb.UpdateQuestion(itemQues);
                                            quesDb.CodeSymbol = code;
                                            quesDb.QuestionGroupID = qGroupDb.ID;
                                            _qnaService.AddQuestion(quesDb);
                                            _qnaService.Save();
                                            string resultAPI = _apiNLR.AddKnowledgeQuestion(formQnAVm.BotID, formQnAVm.FormQuestionAnswerID, quesDb.ID, itemQues.ContentText, itemQues.Target);
                                            if (!String.IsNullOrEmpty(resultAPI))
                                            {
                                                quesDb.IsSendAPI = true;
                                                _qnaService.UpdateQuestion(quesDb);
                                                _qnaService.Save();
                                            }
                                        }
                                        else
                                        {
                                            //content
                                            Question quesDb = new Question();
                                            quesDb.UpdateQuestion(itemQues);
                                            quesDb.CodeSymbol = code;
                                            quesDb.QuestionGroupID = qGroupDb.ID;
                                            _qnaService.AddQuestion(quesDb);
                                            _qnaService.Save();
                                            string resultAPI = _apiNLR.AddKnowledgeQuestion(formQnAVm.BotID, formQnAVm.FormQuestionAnswerID, quesDb.ID, itemQues.ContentText, itemQues.Target);
                                            if (!String.IsNullOrEmpty(resultAPI))
                                            {
                                                quesDb.IsSendAPI = true;
                                                _qnaService.UpdateQuestion(quesDb);
                                                _qnaService.Save();
                                            }

                                            //// is that star
                                            //// content *
                                            //Question quesDbStar = new Question();
                                            //quesDbStar.UpdateQuestionIsStar(itemQues);
                                            //quesDbStar.CodeSymbol = code;
                                            //quesDbStar.QuestionGroupID = qGroupDb.ID;
                                            //_qnaService.AddQuestion(quesDbStar);


                                            ////* content *
                                            //Question quesDbStar2 = new Question();
                                            //quesDbStar2.UpdateQuestionIsStar2(itemQues);
                                            //quesDbStar2.CodeSymbol = code;
                                            //quesDbStar2.QuestionGroupID = qGroupDb.ID;
                                            //_qnaService.AddQuestion(quesDbStar2);

                                            //// * content
                                            //Question quesDbStar3 = new Question();
                                            //quesDbStar3.UpdateQuestionIsStar3(itemQues);
                                            //quesDbStar3.CodeSymbol = code;
                                            //quesDbStar3.QuestionGroupID = qGroupDb.ID;
                                            //_qnaService.AddQuestion(quesDbStar3);

                                            //_qnaService.Save();
                                        }
                                    }
                                    _qnaService.Save();
                                }
                                if (itemQGroup.QnAViewModel.AnswerViewModels != null &&
                                        itemQGroup.QnAViewModel.AnswerViewModels.Count() != 0)
                                {
                                    var lstAnswer = itemQGroup.QnAViewModel.AnswerViewModels;
                                    foreach (var itemAns in lstAnswer)
                                    {
                                        Answer ansDb = new Answer();
                                        ansDb.UpdateAnswer(itemAns);
                                        ansDb.QuestionGroupID = qGroupDb.ID;
                                        _qnaService.AddAnswer(ansDb);
                                    }
                                    _qnaService.Save();
                                }
                                IsCreated = true;
                            }
                            catch (Exception ex)
                            {
                                IsCreated = false;
                                response = request.CreateResponse(HttpStatusCode.BadGateway, IsCreated);
                            }
                        }
                    }
                }
                response = request.CreateResponse(HttpStatusCode.OK, IsCreated);
                return response;
            });
        }

        [Route("createSingleRowQnA")]
        [HttpPost]
        public HttpResponseMessage CreateSingleRowQnaVm(HttpRequestMessage request, FormQnACommonViewModel formQnAVm)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                bool IsCreated = false;
                if (formQnAVm != null && formQnAVm.QuestionGroupViewModels != null && formQnAVm.QuestionGroupViewModels.Count() != 0)
                {
                    var lstQuesGroupVm = formQnAVm.QuestionGroupViewModels.ToList();
                    // Check nội dung kịch bản đã tồn tại
                    var lstQuestionCheck = lstQuesGroupVm[0].QnAViewModel.QuestionViewModels;
                    if (lstQuestionCheck.Count() != 0)
                    {
                        foreach(var itemQuesCheck in lstQuestionCheck)
                        {
                            var rsCheckQuesExist = _qnaService.CheckExitsQuestionScriptByBotID(itemQuesCheck.ContentText, formQnAVm.BotID, lstQuesGroupVm[0].ID);
                            if(rsCheckQuesExist == 0)
                            {
                                IsCreated = false;
                                response = request.CreateResponse(HttpStatusCode.OK, new {status = IsCreated, msg = "Nội dung "+ "'"+ itemQuesCheck.ContentText + "'" + " đã được nhập trước đó" });
                                return response;
                            }
                        }
                    }


                    // nếu trường hợp cập nhật chỉ thêm các question group với id rỗng
                    if (formQnAVm.TypeAction == Common.CommonConstants.UpdateQnA)
                    {
                        var lstQues = _qnaService.GetListQuestionByGroupID(formQnAVm.QuestionGroupViewModels.ToList()[0].ID);
                        if(lstQues != null && lstQues.Count() != 0)
                        {
                            foreach (var quesItem in lstQues)
                            {
                                _apiNLR.DeleteQuestion(quesItem.ID);
                            }
                        }
                        //var lstAnswer = _qnaService.GetListAnswerByGroupID(formQnAVm.QuestionGroupViewModels.ToList()[0].ID);
                        _qnaService.DeleteQuesByQuestionGroup(formQnAVm.QuestionGroupViewModels.ToList()[0].ID);
                        _qnaService.DeleteAnswerByQuestionGroup(formQnAVm.QuestionGroupViewModels.ToList()[0].ID);
                        _qnaService.Save();
                    }

                    if (lstQuesGroupVm != null && lstQuesGroupVm.Count() != 0)
                    {
                        foreach (var itemQGroup in lstQuesGroupVm)
                        {
                            try
                            {
                                int qGroupID = itemQGroup.ID;
                                QuestionGroup questionGroup = new QuestionGroup();
                                questionGroup = _qnaService.GetQuestionGroupById(qGroupID);
                                questionGroup.IsKeyword = itemQGroup.IsKeyWord;
                                _qnaService.UpdateQuesGroup(questionGroup);
                                _qnaService.Save();

                                if (itemQGroup.QnAViewModel.QuestionViewModels != null && itemQGroup.QnAViewModel.QuestionViewModels.Count() != 0
                                    && itemQGroup.QnAViewModel.AnswerViewModels != null && itemQGroup.QnAViewModel.AnswerViewModels.Count() != 0)
                                {
                                    if (itemQGroup.QnAViewModel.QuestionViewModels != null &&
                                        itemQGroup.QnAViewModel.QuestionViewModels.Count() != 0)
                                    {
                                        var lstQuestion = itemQGroup.QnAViewModel.QuestionViewModels;
                                        foreach (var itemQues in lstQuestion)
                                        {
                                            string code = qGroupID + Guid.NewGuid().ToString();
                                            if (itemQues.IsThatStar == false)
                                            {
                                                Question quesDb = new Question();
                                                quesDb.UpdateQuestion(itemQues);
                                                quesDb.CodeSymbol = code;
                                                quesDb.QuestionGroupID = qGroupID;
                                                _qnaService.AddQuestion(quesDb);
                                                _qnaService.Save();
                                                string resultAPI = _apiNLR.AddKnowledgeQuestion(formQnAVm.BotID, formQnAVm.FormQuestionAnswerID, quesDb.ID, itemQues.ContentText, itemQues.Target);
                                                if (!String.IsNullOrEmpty(resultAPI))
                                                {
                                                    quesDb.IsSendAPI = true;
                                                    _qnaService.UpdateQuestion(quesDb);
                                                    _qnaService.Save();
                                                }
                                            }
                                            else
                                            {
                                                //content
                                                Question quesDb = new Question();
                                                quesDb.UpdateQuestion(itemQues);
                                                quesDb.CodeSymbol = code;
                                                quesDb.QuestionGroupID = qGroupID;
                                                _qnaService.AddQuestion(quesDb);
                                                _qnaService.Save();
                                                string resultAPI = _apiNLR.AddKnowledgeQuestion(formQnAVm.BotID, formQnAVm.FormQuestionAnswerID, quesDb.ID, itemQues.ContentText, itemQues.Target);
                                                if (!String.IsNullOrEmpty(resultAPI))
                                                {
                                                    quesDb.IsSendAPI = true;
                                                    _qnaService.UpdateQuestion(quesDb);
                                                    _qnaService.Save();
                                                }
                                            }
                                        }
                                        _qnaService.Save();
                                    }
                                    if (itemQGroup.QnAViewModel.AnswerViewModels != null &&
                                            itemQGroup.QnAViewModel.AnswerViewModels.Count() != 0)
                                    {
                                        var lstAnswer = itemQGroup.QnAViewModel.AnswerViewModels;
                                        foreach (var itemAns in lstAnswer)
                                        {
                                            Answer ansDb = new Answer();
                                            ansDb.UpdateAnswer(itemAns);
                                            ansDb.QuestionGroupID = qGroupID;
                                            _qnaService.AddAnswer(ansDb);
                                        }
                                        _qnaService.Save();
                                    }
                                    IsCreated = true;
                                }
                                else
                                {
                                    IsCreated = false;
                                    response = request.CreateResponse(HttpStatusCode.BadGateway, new { status = IsCreated, msg = "" });
                                    return response;
                                }
                                
                            }
                            catch (Exception ex)
                            {
                                IsCreated = false;
                                response = request.CreateResponse(HttpStatusCode.BadGateway, new { status = IsCreated, msg = "" });
                                return response;
                            }
                        }
                    }
                    else
                    {
                        IsCreated = false;
                        response = request.CreateResponse(HttpStatusCode.BadGateway, new { status = IsCreated, msg = "" });
                        return response;
                    }
                }
                IsCreated = true;
                response = request.CreateResponse(HttpStatusCode.OK, new { status = IsCreated, msg = "Thành công" });
                return response;
            });
        }

        [Route("getaimlqna")]
        [HttpGet]
        public HttpResponseMessage GetAimlQnA(HttpRequestMessage request, int formQnaID, string formAlias, string userID, int botID, bool status = false)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;

                var formQnaDb = _qnaService.GetFormQnAnswerById(formQnaID);
                formQnaDb.Status = status;
                _qnaService.UpdateFormQuestionAnswer(formQnaDb);
                _qnaService.Save();

                var botDb = _botService.GetByID(botID);
                botDb.UpdatedDate = DateTime.Now;
                _botService.Update(botDb);
                _botService.Save();

                var lstQnaGroup = _qnaService.GetListQuesGroupToAimlByFormQnAnswerID(formQnaID).ToList();
                bool IsAiml = false;
                string msg = "";
                // open file bot aiml
                //string pathFolderAIML = ConfigurationManager.AppSettings["AIMLPath"].ToString() + "User_" + userID + "_BotID_" + botID;
                string pathFolderAIML = PathServer.PathAIML;
                string nameFolderAIML = "User_" + userID + "_BotID_" + botID + "\\formQnA_ID_" + formQnaID + "_" + formAlias + ".aiml";
                string pathString = System.IO.Path.Combine(pathFolderAIML, nameFolderAIML);

                var aimlDb = _aimlService.GetByFormId(formQnaID);
                StringBuilder sbFormDb = new StringBuilder();
                //if (System.IO.File.Exists(pathString))
                //{
                //File.WriteAllText(pathString, String.Empty);
                try
                {
                    //StreamWriter sw = new StreamWriter(pathString, true, Encoding.UTF8);
                    //////sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                    ////sw.WriteLine("<aiml>");
                    //sw.WriteLine("<category>");
                    //sw.WriteLine("<pattern>*</pattern>");
                    //sw.WriteLine("<template>");
                    //sw.WriteLine("<random>");
                    //sw.WriteLine("<li> NOT_MATCH_01 </li>");
                    //sw.WriteLine("<li> NOT_MATCH_02 </li>");
                    //sw.WriteLine("<li> NOT_MATCH_03 </li>");
                    //sw.WriteLine("<li> NOT_MATCH_04 </li>");
                    //sw.WriteLine("<li> NOT_MATCH_05 </li>");
                    //sw.WriteLine("<li> NOT_MATCH_06 </li>");
                    //sw.WriteLine("</random>");
                    //sw.WriteLine("</template>");
                    //sw.WriteLine("</category>");

                    sbFormDb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                    sbFormDb.AppendLine("<aiml>");
                    sbFormDb.AppendLine("<category>");
                    sbFormDb.AppendLine("<pattern>*</pattern>");
                    sbFormDb.AppendLine("<template>");
                    sbFormDb.AppendLine("<random>");
                    sbFormDb.AppendLine("<li> NOT_MATCH_01 </li>");
                    sbFormDb.AppendLine("<li> NOT_MATCH_02 </li>");
                    sbFormDb.AppendLine("<li> NOT_MATCH_03 </li>");
                    sbFormDb.AppendLine("<li> NOT_MATCH_04 </li>");
                    sbFormDb.AppendLine("<li> NOT_MATCH_05 </li>");
                    sbFormDb.AppendLine("<li> NOT_MATCH_06 </li>");
                    sbFormDb.AppendLine("</random>");
                    sbFormDb.AppendLine("</template>");
                    sbFormDb.AppendLine("</category>");
                    if (lstQnaGroup != null && lstQnaGroup.Count() != 0)
                    {
                        int total = lstQnaGroup.Count();
                        for (int indexQGroup = 0; indexQGroup < total; indexQGroup++)
                        {
                            
                            var itemQGroup = lstQnaGroup[indexQGroup];
                            var lstAnswer = itemQGroup.Answers.ToList();
                            var lstQuestion = itemQGroup.Questions.ToList();
                            var isKeyword = itemQGroup.IsKeyword ?? false;
                            if (lstQuestion.Count() == 0 && lstAnswer.Count() == 0)
                            {
                                IsAiml = false;
                                response = request.CreateResponse(HttpStatusCode.OK, new { res = IsAiml, msg = "Dữ liệu chưa được lưu hoặc để trống" } );
                                return response;
                            }
                            string postbackAnswer = String.Empty;
                            if (lstAnswer.Count() != 0 && lstAnswer.Count() > 1)
                            {
                                StringBuilder sb = new StringBuilder();

                                int totalAnswer = lstAnswer.Count();
                                postbackAnswer = "postback_answer_" + itemQGroup.ID;
                                sb.AppendLine("<category>");
                                sb.AppendLine("<pattern>" + postbackAnswer + "</pattern>");
                                sb.AppendLine("<template>");
                                sb.AppendLine("<random>");
                                for (int indexA = 0; indexA < totalAnswer; indexA++)
                                {
                                    var itemAnswer = lstAnswer[indexA];
                                    if (!String.IsNullOrEmpty(itemAnswer.ContentText))
                                    {
                                        sb.AppendLine("<li>" + itemAnswer.ContentText + "</li>");
                                    }
                                    else
                                    {
                                        sb.AppendLine("<li>" + itemAnswer.CardPayload + "</li>");
                                    }
                                }
                                sb.AppendLine("</random>");
                                sb.AppendLine("</template>");
                                sb.AppendLine("</category>");
                                //sw.WriteLine(sb.ToString());
                                sbFormDb.AppendLine(sb.ToString());
                            }
                            else
                            {
                                if (!String.IsNullOrEmpty(lstAnswer[0].ContentText))
                                {
                                    postbackAnswer = lstAnswer[0].ContentText;
                                }
                                else
                                {
                                    postbackAnswer = lstAnswer[0].CardPayload;
                                }
                            }
                            if (lstQuestion.Count != 0)
                            {
                                foreach(var item in lstQuestion)
                                {
                                    if(isKeyword == false) { // nếu không chọn dấu tích ở keyword thì k thêm *
                                        var itemQ = item;
                                        string patternText  = itemQ.ContentText.ToUpper();
                                        string tempAnswer = postbackAnswer;
                                        if (postbackAnswer.Contains("postback"))
                                        {
                                            tempAnswer = "<srai>" + postbackAnswer + "</srai>";
                                        }

                                        sbFormDb.AppendLine("<category>");
                                        sbFormDb.AppendLine("<pattern>" + patternText + "</pattern>");
                                        sbFormDb.AppendLine("<template>" + tempAnswer + "</template>");
                                        sbFormDb.AppendLine("</category>");
                                    }
                                    else
                                    {
                                        for (int indexQ = 0; indexQ < _userSayStart.Length; indexQ++)
                                        {
                                            var itemQ = item;
                                            string patternText = "";
                                            string tempAnswer = postbackAnswer;
                                            if (postbackAnswer.Contains("postback"))
                                            {
                                                tempAnswer = "<srai>" + postbackAnswer + "</srai>";
                                            }
                                            //sw.WriteLine("<category>");
                                            //sw.WriteLine("<pattern>"+itemQ.ContentText.ToUpper()+"</pattern>");
                                            //sw.WriteLine("<template>"+ tempAnswer + "</template>");
                                            //sw.WriteLine("</category>");
                                            if (_userSayStart[indexQ] == CommonConstants.UserSay_IsStartDefault)
                                            {
                                                patternText = itemQ.ContentText.ToUpper();
                                            }
                                            if (_userSayStart[indexQ] == CommonConstants.UserSay_IsStartFirst)
                                            {
                                                patternText = "* " + itemQ.ContentText.ToUpper();
                                            }
                                            if (_userSayStart[indexQ] == CommonConstants.UserSay_IsStartLast)
                                            {
                                                patternText = itemQ.ContentText.ToUpper() + " *";
                                            }
                                            if (_userSayStart[indexQ] == CommonConstants.UserSay_IsStartDouble)
                                            {
                                                patternText = "* " + itemQ.ContentText.ToUpper() + " *";
                                            }
                                            sbFormDb.AppendLine("<category>");
                                            sbFormDb.AppendLine("<pattern>" + patternText + "</pattern>");
                                            sbFormDb.AppendLine("<template>" + tempAnswer + "</template>");
                                            sbFormDb.AppendLine("</category>");
                                        }
                                    }
                                    
                                }                               
                            }
                        }
                    }
                    //sw.WriteLine("</aiml>");
                    sbFormDb.AppendLine("</aiml>");
                    //sw.Close();
                    IsAiml = true;

                    if (aimlDb == null)
                    {
                        AIMLFile aimlFileDb = new AIMLFile();
                        aimlFileDb.UserID = userID;
                        aimlFileDb.BotID = botID;
                        aimlFileDb.Src = nameFolderAIML;
                        aimlFileDb.FormQnAnswerID = formQnaID;
                        aimlFileDb.Extension = "aiml";
                        aimlFileDb.Name = "Form-" + formAlias;
                        aimlFileDb.Status = status;
                        aimlFileDb.Content = sbFormDb.ToString();
                        _aimlService.Create(aimlFileDb);
                        _aimlService.Save();
                    }
                    else
                    {
                        aimlDb.Content = sbFormDb.ToString();
                        aimlDb.Status = status;
                        _aimlService.Update(aimlDb);
                        _aimlService.Save();
                    }
                }
                catch (Exception ex)
                {
                    IsAiml = false;
                    response = request.CreateResponse(HttpStatusCode.OK, new { res = IsAiml, msg = "Lỗi hệ thống" });
                    return response;
                }
                finally
                {

                }

                // Tạo file AIML2Graphmaste

                
                response = request.CreateResponse(HttpStatusCode.OK, new { res = IsAiml, msg = "Thành công" });
                return response;
            });
        }

        [Route("getqnabyformid")]
        [HttpGet]
        public HttpResponseMessage GetQnAnswerByFormId(HttpRequestMessage request, int formQnaID)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                var lstBotQna = _qnaService.GetListQuestionGroupByFormQnAnswerID(formQnaID).OrderBy(x => x.Index).ToList();
                response = request.CreateResponse(HttpStatusCode.OK, lstBotQna);
                return response;
            });
        }
        [Route("getQnaByFormIdPagination")]
        [HttpGet]
        public HttpResponseMessage GetQnAnswerByFormIdPagination(HttpRequestMessage request, int formQnaID, int page, int pageSize = 5)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                int totalRow = 0;
                var lstQuesGroup = _qnaService.GetListQuestionGroupByFormQnAnswerPagination(formQnaID, page, pageSize).ToList(); 
                var lstQuesGroupVm = Mapper.Map<IEnumerable<StoreProcQuesGroupViewModel>, IEnumerable<QuestionGroupViewModel>>(lstQuesGroup);

                if (lstQuesGroupVm.Count() != 0)
                {
                    totalRow = lstQuesGroup[0].Total;

                    foreach (var item in lstQuesGroupVm)
                    {
                        item.QnAViewModel = new QnAViewModel();
                        var lstQuestionDb = _qnaService.GetListQuestionByGroupID(item.ID);
                        item.QnAViewModel.QuestionViewModels = Mapper.Map<IEnumerable<Question>, IEnumerable<QuestionViewModel>>(lstQuestionDb);
                        var lstAnswerDb = _qnaService.GetListAnswerByGroupID(item.ID);
                        item.QnAViewModel.AnswerViewModels = Mapper.Map<IEnumerable<Answer>, IEnumerable<AnswerViewModel>>(lstAnswerDb);
                        if (item.QnAViewModel.AnswerViewModels != null && item.QnAViewModel.AnswerViewModels.Any())
                        {
                            foreach(var answer in item.QnAViewModel.AnswerViewModels)
                            {
                                if(answer.CardID != null)
                                {
                                    answer.CardName = _cardService.GetCardByPattern("postback_card_" + answer.CardID).Name;
                                }
                            }
                        }
                    }
                }
                var paginationSet = new PaginationSet<QuestionGroupViewModel>()
                {
                    Items = lstQuesGroupVm,
                    Page = page,
                    TotalCount = totalRow,
                    MaxPage = pageSize,
                    TotalPages = (int)Math.Ceiling((decimal)totalRow / pageSize)
                };
                response = request.CreateResponse(HttpStatusCode.OK, paginationSet);
                return response;
            });
        }

        [Route("deleteques")]
        [HttpPost]
        public HttpResponseMessage DeleteQuestion(HttpRequestMessage request, int questionID)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                var ques = _qnaService.DeleteQuestion(questionID);
                _qnaService.Save();
                response = request.CreateResponse(HttpStatusCode.Created, ques);
                return response;
            });
        }


        [Route("deleteanswer")]
        [HttpPost]
        public HttpResponseMessage DeleteAnswer(HttpRequestMessage request, int answerID)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                var answer = _qnaService.DeleteAnswer(answerID);
                _qnaService.Save();
                response = request.CreateResponse(HttpStatusCode.Created, answer);
                return response;
            });
        }
        
        [Route("addQuesGroup")]
        [HttpPost]
        public HttpResponseMessage AddQuesGroup(HttpRequestMessage request, QuestionGroupViewModel qGroupVm)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                QuestionGroup qGroupDb = new QuestionGroup();
                qGroupDb.UpdateQuestionGroup(qGroupVm);
                _qnaService.AddQuesGroup(qGroupDb);
                _qnaService.Save();
                response = request.CreateResponse(HttpStatusCode.Created, qGroupDb);
                return response;
            });

        }

        [Route("deletequesgroup")]
        [HttpPost]
        public HttpResponseMessage DeleteQuesGroup(HttpRequestMessage request, JObject jsonData)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                dynamic json = jsonData;
                int qGroupID = json.qGroupID;
                var qGroup = _qnaService.DeleteQuestionGroup(qGroupID);
                _qnaService.Save();
                response = request.CreateResponse(HttpStatusCode.Created, qGroup);
                return response;
            });
        }


        [Route("addUpdateQuestion")]
        [HttpPost]
        public HttpResponseMessage AddUpdateQuestion(HttpRequestMessage request, QuestionViewModel quesVm)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                Question quesDb = new Question();
                if (quesVm.ID == 0)
                {
                    quesDb.UpdateQuestion(quesVm);
                    quesDb.QuestionGroupID = quesVm.QuestionGroupID;
                    quesDb.IsSendAPI = false;
                    _qnaService.AddQuestion(quesDb);
                }
                else
                {
                    quesDb = _qnaService.GetByQuestionId(quesVm.ID);
                    quesDb.ContentText = quesVm.ContentText;
                    _qnaService.UpdateQuestion(quesDb);
                }
                _qnaService.Save();
                response = request.CreateResponse(HttpStatusCode.Created, quesDb);
                return response;
            });
        }

        [Route("addUpdateAnswer")]
        [HttpPost]
        public HttpResponseMessage AddUpdateAnswer(HttpRequestMessage request, AnswerViewModel answerVm)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                Answer answerDb = new Answer();
                if(answerVm.ID == 0)
                {
                    answerDb.UpdateAnswer(answerVm);
                    _qnaService.AddAnswer(answerDb);
                }
                else
                {
                    answerDb = _qnaService.GetByAnswerId(answerVm.ID);
                    answerDb.ContentText = answerVm.ContentText;
                    answerDb.CardPayload = answerVm.CardPayload;
                    answerDb.CardID = answerVm.CardID;
                    _qnaService.UpdateAnswer(answerDb);
                }
                _qnaService.Save();
                response = request.CreateResponse(HttpStatusCode.Created, answerDb);
                return response;
            });
        }


        //[Route("updatequestion")]
        //[HttpPost]
        //public HttpResponseMessage UpdateQuestion(HttpRequestMessage request, QuestionViewModel quesVm)
        //{
        //    return CreateHttpResponse(request, () =>
        //    {
        //        HttpResponseMessage response = null;
        //        if (!ModelState.IsValid)
        //        {
        //            request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
        //        }
        //        else
        //        {
        //            try
        //            {
        //                var _lstQuesUpdate = _qnaService.GetListQuesCodeSymbol(quesVm.CodeSymbol).ToList();
        //                if (_lstQuesUpdate.Count != 0)
        //                {
        //                    foreach (var item in _lstQuesUpdate)
        //                    {
        //                        if (item.IsThatStar == false)
        //                        {
        //                            item.ContentText = quesVm.ContentText.Trim();
        //                        }
        //                        else
        //                        {
        //                            item.ContentText = quesVm.ContentText.Trim() + " *";
        //                        }
        //                        _qnaService.UpdateQuestion(item);
        //                    }
        //                    _qnaService.Save();
        //                }

        //                response = request.CreateResponse(HttpStatusCode.OK, "Success");
        //            }
        //            catch (Exception ex)
        //            {
        //                response = request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
        //            }
        //        }
        //        return response;
        //    });
        //}

     
    }
}
