using AIMLbot;
using AutoMapper;
using BotProject.Common;
using BotProject.Common.AppThird3PartyTemplate;
using BotProject.Common.ViewModels;
using BotProject.Model.Models;
using BotProject.Service;
using BotProject.Web.Infrastructure.Core;
using BotProject.Web.Infrastructure.Extensions;
using BotProject.Web.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Script.Serialization;

namespace BotProject.Web.API_Webhook
{
    [RoutePrefix("api/messagev2")]
    public class KioskController : ApiControllerBase
    {
        // Thẻ chat với chuyên viên
        public string _contactAdmin = Helper.ReadString("AdminContact");
        public string _titlePayloadContactAdmin = Helper.ReadString("TitlePayloadAdminContact");

        private const string POSTBACK_MODULE = "POSTBACK_MODULE";
        private const string POSTBACK_CARD = "POSTBACK_CARD";
        private const string POSTBACK_TEXT = "POSTBACK_TEXT";
        private const string POSTBACK_NOT_MATCH = "POSTBACK_NOT_MATCH";

        private readonly Dictionary<string, string> _DICTIONARY_NOT_MATCH = new Dictionary<string, string>()
        {
           {"NOT_MATCH_01", "Xin lỗi,em chưa hiểu ý anh/chị ạ!"},
           {"NOT_MATCH_02", "Anh/chị có thể giải thích thêm được không?"},
           {"NOT_MATCH_03", "Chưa hiểu lắm ạ, anh/chị có thể nói rõ hơn được không ạ?"},
           {"NOT_MATCH_04", "Xin lỗi, Anh/chị có thể giải thích thêm được không?"},
           {"NOT_MATCH_05", "Xin lỗi, Tôi chưa được học để hiểu nội dung này?"},
        };

        // Tin nhắn vắng mặt
        public string _messageAbsent = "";
        public bool _isHaveMessageAbsent = false;

        // Thời gian phản hồi lại sau khoảng timeOut
        public int _timeOut = 60;

        // Domain, API Search Engine
        private readonly string Domain = Helper.ReadString("Domain");
        private readonly string UrlAPI = Helper.ReadString("UrlAPI");
        private readonly string KeyAPI = Helper.ReadString("KeyAPI");
        private string pathAIML = PathServer.PathAIML;
        private string pathSetting = PathServer.PathAIML + "config";

        // Pattern kiểm tra là số
        private const string NumberPattern = @"^\d+$";

        // Nút thoát
        string _patternCardPayloadProactive = "quay về";
        string _titleCardPayloadProactive = "🔙 Quay về";

        // Điều kiện có mở search engine
        bool _isSearchAI = true;

        private string TITLE_PAYLOAD_QUICKREPLY = "";

        // Model user
        ApplicationPlatformUser _plUserDb;


        // BOT PRIVATE CUSTOMIZE
        private const int BOT_Y_TE = 3019;

        // AIML Bot Services
        private AIMLBotService _aimlBotService;

        private AIMLbot.Bot _botService;

        // Services
        private ApiQnaNLRService _apiNLR;
        private IErrorService _errorService;
        //private BotServiceMedical _botService;
        private ISettingService _settingService;
        private ICardService _cardService;
        private IAIMLFileService _aimlFileService;
        private IBotService _botDbService;
        private User _user;
        private Dictionary<string, string> _dicAttributeUser;
        private IApplicationPlatformUserService _appPlatformUser;
        private IAttributeSystemService _attributeService;
        private IHandleModuleServiceService _handleMdService;
        private IHistoryService _historyService;

        //Accent Vietnamese
        private AccentService _accentService;

        public List<string> _lstBotReplyResponse = new List<string>();
        public KioskController(IErrorService errorService,
                                      IBotService botDbService,
                                      ISettingService settingService,
                                      ICardService cardService,
                                      IAIMLFileService aimlFileService,
                                      IApplicationPlatformUserService appPlatformUser,
                                      IAttributeSystemService attributeService,
                                      IHandleModuleServiceService handleMdService,
                                      IHistoryService historyService) : base(errorService)
        {
            _errorService = errorService;
            _aimlBotService = AIMLBotService.AIMLBotInstance;
            _botDbService = botDbService;
            _settingService = settingService;
            _cardService = cardService;
            _aimlFileService = aimlFileService;
            _appPlatformUser = appPlatformUser;
            _attributeService = attributeService;
            _handleMdService = handleMdService;
            _historyService = historyService;
            _apiNLR = new ApiQnaNLRService();
            _plUserDb = new ApplicationPlatformUser();
        }

        [HttpGet]
        [Route("getStarted")]
        public async Task<HttpResponseMessage> GetStarted(HttpRequestMessage request, int botId)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                var tempStarted = _settingService.GetSettingByBotID(botId);
                response = request.CreateResponse(HttpStatusCode.OK, new
                {
                    status = "2",
                    message = "Thành công",
                    data = "postback_card_" + tempStarted.CardID
                });
                return response;
            });
        }

        [HttpPost]
        [Route("receive")]
        public async Task<HttpResponseMessage> Receive(HttpRequestMessage request, Message message)
        {
            return CreateHttpResponse(request, () =>
            {
                System.Diagnostics.Stopwatch timer = new Stopwatch();
                timer.Start();

                HttpResponseMessage response = null;
                string text = message.text;
                string senderId = message.senderId;
                int botId = Int32.Parse(message.botId);
                if (String.IsNullOrEmpty(text))
                {
                    response = request.CreateResponse(HttpStatusCode.OK, new
                    {
                        status = "-1",
                        message = "Vui lòng nhập nội dung",
                        data = new string[] { }
                    });
                    return response;
                }
                if (String.IsNullOrEmpty(message.botId))
                {
                    response = request.CreateResponse(HttpStatusCode.OK, new
                    {
                        status = "-1",
                        message = "Không có thông tin Bot",
                        data = new string[] { }
                    });
                    return response;
                }
                if (String.IsNullOrEmpty(message.senderId))
                {
                    response = request.CreateResponse(HttpStatusCode.OK, new
                    {
                        status = "-1",
                        message = "Không có thông tin người dùng",
                        data = new string[] { }
                    });
                    return response;
                }

                //var botDb = _botDbService.GetByID(botId);
                //string pathFileBinary = ConfigurationManager.AppSettings["AIML2GraphmasterPath"] + "BotID_" + message.botId + ".bin";
                //_botService.loadAIMLFromFileBinary(pathFileBinary);


                string typeRequest = text.Contains("postback") ? "payload_postback" : "text";

                List<string> lstMsgResponse = new List<string>();

                // get list message response
                try
                {
                    // Khởi động lấy "brain" của bot service theo id
                    GetServerAIMLBot(message.botId);

                    // Khởi tạo user theo bot service
                    InitUserByServerAIMLBot(message.senderId);

                    var lstAttribute = _attributeService.GetListAttributePlatform(senderId, botId).ToList();
                    if (lstAttribute.Count() != 0)
                    {
                        _dicAttributeUser = new Dictionary<string, string>();
                        foreach (var attr in lstAttribute)
                        {
                            _dicAttributeUser.Add(attr.AttributeKey, attr.AttributeValue);
                        }
                    }

                    lstMsgResponse = MessageResponse(text, senderId, botId, typeRequest).Result;

                }
                catch (Exception ex)
                {
                    response = request.CreateResponse(HttpStatusCode.OK, new
                    {
                        status = "-1",
                        message = ex.Message.ToString(),
                        data = new string[] { }
                    });
                    return response;
                }
                if (lstMsgResponse.Count() == 0)
                {
                    response = request.CreateResponse(HttpStatusCode.OK, new
                    {
                        status = "3",
                        message = "Không có dữ liệu",
                        data = new string[] { }
                    });
                    return response;
                }
                List<dynamic> lstResult = new List<dynamic>();
                foreach (var msg in lstMsgResponse)
                {
                    var result = JsonConvert.DeserializeObject<dynamic>(msg);
                    lstResult.Add(result);
                }

                timer.Stop();
                TimeSpan timeTaken = timer.Elapsed;
                response = request.CreateResponse(HttpStatusCode.OK, new
                {
                    timetaken = timeTaken.ToString(),
                    status = "2",
                    message = "Thành công",
                    data = lstResult,
                });
                
                return response;
            });
        }
        public void GetServerAIMLBot(string botId)
        {
            _botService = _aimlBotService.GetServerBot(botId);
            string pathAIML2Graphmaster = ConfigurationManager.AppSettings["AIML2GraphmasterPath"] + "BotID_" + botId + ".bin";
            _aimlBotService.LoadGraphmasterFromAIMLBinaryFile(pathAIML2Graphmaster, _botService);
        }
        public void InitUserByServerAIMLBot(string senderId)
        {
            _user = _aimlBotService.loadUserBot(senderId, _botService);
        }
        private async Task<List<string>> MessageResponse(string text, string senderId, int botId, string typeRequest, bool isHavePreviousResponse = false)
        {
            if (!isHavePreviousResponse)
            {
                _lstBotReplyResponse = new List<string>();
            }

            text = HttpUtility.HtmlDecode(text);
            text = Regex.Replace(text, @"<(.|\n)*?>", "").Trim();
            text = Regex.Replace(text, @"\p{Cs}", "").Trim(); //remove emoji

            _plUserDb = _appPlatformUser.GetByUserId(senderId);

            // Input text
            if (typeRequest == CommonConstants.BOT_REQUEST_TEXT)
            {
                // Thêm dấu tiếng việt
                bool isActive = true;
                string textAccentVN = GetPredictAccentVN(text, isActive);

                if (!String.IsNullOrEmpty(textAccentVN))
                {
                    if (textAccentVN != text)
                    {
                        string msg = FacebookTemplate.GetMessageTemplateText("Ý bạn là: " + textAccentVN + "", senderId).ToString();
                        await SendMessage(msg, senderId);
                    }
                    text = textAccentVN;
                }

                if (botId == BOT_Y_TE)
                {
                    AttributePlatformUser attPlUser = new AttributePlatformUser();
                    attPlUser.AttributeKey = "content_message";
                    attPlUser.AttributeValue = text;
                    attPlUser.BotID = botId;
                    attPlUser.UserID = senderId;
                    _dicAttributeUser.Remove("content_message");
                    _dicAttributeUser.Add("content_message", text);

                    AddAttributeDefault(senderId, botId, attPlUser.AttributeKey, attPlUser.AttributeValue);
                }
            }

            // Input postback            
            if (typeRequest == CommonConstants.BOT_REQUEST_PAYLOAD_POSTBACK)
            {
                string[] arrPayloadQuickReply = Regex.Split(text, "-");
                if (arrPayloadQuickReply.Length > 1)
                {
                    TITLE_PAYLOAD_QUICKREPLY = arrPayloadQuickReply[1];
                }
                text = arrPayloadQuickReply[0];
            }


            // Xét điều kiện đi tiếp hay cần lưu giá trị của thẻ đi trước
            if (_plUserDb.IsHaveSetAttributeSystem)
            {
                AttributePlatformUser attPlUser = new AttributePlatformUser();
                attPlUser.AttributeKey = _plUserDb.AttributeName;
                attPlUser.BotID = botId;
                attPlUser.UserID = senderId;
                bool isUpdateAttr = false;
                if (typeRequest == CommonConstants.BOT_REQUEST_TEXT)
                {
                    attPlUser.AttributeValue = text;
                    isUpdateAttr = true;
                }
                if (typeRequest == CommonConstants.BOT_REQUEST_PAYLOAD_POSTBACK)
                {
                    if (!String.IsNullOrEmpty(TITLE_PAYLOAD_QUICKREPLY))
                    {
                        attPlUser.AttributeValue = TITLE_PAYLOAD_QUICKREPLY;
                        isUpdateAttr = true;

                    }
                }
                if (isUpdateAttr)
                {
                    // Kiểm tra giá trị nhập vào theo từng thuộc tính
                    // Bot Y Tế
                    // Case: Tuổi
                    if (attPlUser.AttributeKey == "age")
                    {
                        bool isAge = Regex.Match(text, NumberPattern).Success;
                        if (isAge)
                        {
                            attPlUser.AttributeValue = text;
                        }
                        else
                        {
                            string msg = FacebookTemplate.GetMessageTemplateText("Ký tự phải là số, Anh/chị vui lòng nhập lại độ tuổi", senderId).ToString();
                            await SendMessage(msg, senderId);
                            return await Task.FromResult<List<string>>(_lstBotReplyResponse);
                        }
                    }
                    _dicAttributeUser.Remove(attPlUser.AttributeKey);
                    _dicAttributeUser.Add(attPlUser.AttributeKey, attPlUser.AttributeValue);


                    AddAttributeDefault(senderId, botId, attPlUser.AttributeKey, attPlUser.AttributeValue);

                }

                //hisVm.BotID = botId;
                //hisVm.CreatedDate = DateTime.Now;
                //hisVm.UserSay = text;
                //hisVm.UserName = sender;
                //hisVm.Type = CommonConstants.TYPE_FACEBOOK;
                //hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_004;
                //AddHistory(hisVm);
            }
            if (_plUserDb.PredicateName == "REQUIRE_CLICK_BUTTON_TO_NEXT_CARD")
            {
                if (typeRequest == CommonConstants.BOT_REQUEST_TEXT)
                {
                    string contentRequireClick = FacebookTemplate.GetMessageTemplateText("Anh/chị vui lòng chọn lại thông tin bên dưới", senderId).ToString();
                    await SendMessage(contentRequireClick, senderId);
                    string partternCardRequireClick = _plUserDb.PredicateValue;
                    string templateCardRequireClick = HandlePostbackCard(partternCardRequireClick, botId);
                    await SendMultiMessageTask(templateCardRequireClick, senderId);

                    return await Task.FromResult<List<string>>(_lstBotReplyResponse);

                }
            }
            else if (_plUserDb.PredicateName == "REQUIRE_INPUT_TEXT_TO_NEXT_CARD")
            {
                if (typeRequest == CommonConstants.BOT_REQUEST_TEXT)
                {
                    string partternCardRequireInput = _plUserDb.PredicateValue;
                    string templateCardRequireInput = HandlePostbackCard(partternCardRequireInput, botId);
                    await SendMultiMessageTask(templateCardRequireInput, senderId);
                    return await Task.FromResult<List<string>>(_lstBotReplyResponse);
                }
            }
            else if (_plUserDb.PredicateName == "VERIFY_TEXT_WITH_AREA_BUTTON")
            {
                if (typeRequest == CommonConstants.BOT_REQUEST_TEXT)
                {
                    var cardDb = _cardService.GetCardByPattern(_plUserDb.PredicateValue);
                    if (cardDb == null)
                    {
                        return await Task.FromResult<List<string>>(_lstBotReplyResponse);
                    }
                    string area = cardDb.Name;
                    text = text + " " + area;
                }
            }
            else if (_plUserDb.PredicateName == "POSTBACK_MODULE")
            {
                if (typeRequest == CommonConstants.BOT_REQUEST_TEXT)
                {
                    string postbackModule = _plUserDb.PredicateValue;
                    string templateModule = HandlePostbackModule(postbackModule, text, botId, false);
                    await SendMessage(templateModule, senderId);
                    return await Task.FromResult<List<string>>(_lstBotReplyResponse);
                }
            }

            // print postback card
            if (typeRequest == CommonConstants.BOT_REQUEST_PAYLOAD_POSTBACK)
            {
                string templateCard = HandlePostbackCard(text, botId);
                await SendMultiMessageTask(templateCard, senderId);
                if (_plUserDb.PredicateName == "AUTO_NEXT_CARD")
                {
                    string partternNextCard = _plUserDb.PredicateValue;
                    string templateNextCard = HandlePostbackCard(partternNextCard, botId);
                    await SendMultiMessageTask(templateNextCard, senderId);
                }
                return await Task.FromResult<List<string>>(_lstBotReplyResponse);
            }
            AIMLbot.Result rsAIMLBot = GetBotReplyFromAIMLBot(text);
            ResultBot rsBOT = new ResultBot();
            rsBOT = CheckTypePostbackFromResultBotReply(rsAIMLBot);
            if (rsBOT.Type == POSTBACK_MODULE)
            {
                string templateModule = HandlePostbackModule(rsBOT.PatternPayload, text, botId, true);
                await SendMessage(templateModule, senderId);
            }
            if (rsBOT.Type == POSTBACK_CARD)
            {
                string templateCard = HandlePostbackCard(rsBOT.PatternPayload, botId);
                await SendMultiMessageTask(templateCard, senderId); // print message card
                if (_plUserDb.PredicateName == "AUTO_NEXT_CARD") // print message card kế tiếp nếu có
                {
                    string partternNextCard = _plUserDb.PredicateValue;
                    templateCard = HandlePostbackCard(partternNextCard, botId);
                    await SendMultiMessageTask(templateCard, senderId);
                }
                // Trường hợp bot y tế thẻ tin nhắn xuất ra cuối cùng có chứa các từ dưới sẽ lấy tiếp tin nhắn triệu chứng
                if (botId == BOT_Y_TE)
                {
                    if (templateCard.Contains("Nguyên nhân") || templateCard.Contains("bác sĩ") || templateCard.Contains("Bác sĩ"))
                    {
                        List<string> lstSymptoms = new List<string>();
                        lstSymptoms = GetSymptoms(_dicAttributeUser["content_message"]);
                        if (lstSymptoms.Count() != 0)
                        {
                            foreach (var symp in lstSymptoms)
                            {
                                await SendMessage(symp, senderId);
                            }
                        }
                    }
                }
            }
            if (rsBOT.Type == POSTBACK_NOT_MATCH)
            {
                List<string> lstSymptoms = new List<string>();
                if (botId == BOT_Y_TE)
                {
                    lstSymptoms = GetSymptoms(text);
                    if (lstSymptoms.Count() != 0)
                    {
                        foreach (var symp in lstSymptoms)
                        {
                            await SendMessage(symp, senderId);
                        }
                    }
                }
                List<string> lstFaq = new List<string>();
                lstFaq = GetRelatedQuestion(text, "0", "5", botId.ToString());
                if (lstFaq.Count() != 0)
                {
                    foreach (var faq in lstFaq)
                    {
                        await SendMessage(faq, senderId);
                    }
                }
                if (lstSymptoms.Count() == 0 && lstFaq.Count() == 0)
                {
                    List<string> keyList = new List<string>(_DICTIONARY_NOT_MATCH.Keys);
                    Random rand = new Random();
                    string randomKey = keyList[rand.Next(keyList.Count)];
                    string contentNotFound = _DICTIONARY_NOT_MATCH[randomKey];
                    string templateNotFound = FacebookTemplate.GetMessageTemplateTextAndQuickReply(
                        contentNotFound, senderId, _contactAdmin, _titlePayloadContactAdmin).ToString();
                    await SendMessage(templateNotFound, senderId);
                }
            }
            if (rsBOT.Type == POSTBACK_TEXT)
            {
                string templateText = FacebookTemplate.GetMessageTemplateText(rsBOT.PatternPayload, senderId).ToString();
                await SendMessage(templateText, senderId);
            }

            return await Task.FromResult<List<string>>(_lstBotReplyResponse);
        }

        private AIMLbot.Result GetBotReplyFromAIMLBot(string text)
        {
            AIMLbot.Result aimlBotResult = _aimlBotService.Chat(text, _user, _botService);
            return aimlBotResult;
        }
        private ResultBot CheckTypePostbackFromResultBotReply(AIMLbot.Result rsAIMLBot)
        {
            ResultBot rsBot = new ResultBot();
            string result = "";
            if (rsAIMLBot.OutputSentences != null && rsAIMLBot.OutputSentences.Count() != 0)
            {
                result = rsAIMLBot.OutputSentences[0].ToString().Replace(".", "").Trim();

                string strTempPostback = rsAIMLBot.SubQueries[0].Template;
                // nếu nhập text trả về output là postback
                bool isPostbackCard = Regex.Match(strTempPostback, "<template><srai>postback_card_(\\d+)</srai></template>").Success;
                //trường hợp trả về câu hỏi random chứa postback
                bool isPostbackAnswer = Regex.Match(strTempPostback, "<template><srai>postback_answer_(\\d+)</srai></template>").Success;

                if (result.Contains(CommonConstants.POSTBACK_MODULE))
                {
                    string patternPayloadModule = result;

                    rsBot.Type = POSTBACK_MODULE;
                    rsBot.Total = 1;
                    rsBot.PatternPayload = patternPayloadModule;
                    return rsBot;
                }
                else if (isPostbackCard)
                {
                    strTempPostback = Regex.Replace(strTempPostback, @"<(.|\n)*?>", "").Trim();
                    rsBot.Type = POSTBACK_CARD;
                    rsBot.Total = 1;
                    rsBot.PatternPayload = strTempPostback;
                }
                else if (isPostbackAnswer)
                {
                    if (result.Contains(CommonConstants.PostBackCard))
                    {
                        rsBot.Type = POSTBACK_CARD;
                        rsBot.Total = 1;
                        rsBot.PatternPayload = result;
                    }
                }
                else if (result.Contains("NOT_MATCH"))
                {
                    rsBot.Type = POSTBACK_NOT_MATCH;
                    rsBot.Total = 1;
                    rsBot.PatternPayload = "";
                }
                else
                {
                    rsBot.Type = POSTBACK_TEXT;
                    rsBot.Total = 1;
                    rsBot.PatternPayload = result;
                }
            }
            return rsBot;
        }
        #region Handle condition card and module
        private string HandlePostbackCard(string patternCard, int botId)
        {
            _plUserDb.PredicateName = "";
            _plUserDb.PredicateValue = "";
            _plUserDb.IsHaveSetAttributeSystem = false;
            _plUserDb.AttributeName = "";
            var cardDb = _cardService.GetCardByPattern(patternCard);
            string tempCardFb = cardDb.TemplateJsonFacebook;

            if (cardDb.TemplateJsonFacebook.Contains("module"))
            {
                var rsAIMLBot = GetBotReplyFromAIMLBot(patternCard);
                string patternModule = rsAIMLBot.OutputSentences[0].ToString().Replace(".", "").Trim();
                return HandlePostbackModule(patternModule, patternModule, botId, true);
            }
            if (!String.IsNullOrEmpty(cardDb.AttributeSystemName))
            {
                _plUserDb.IsHaveSetAttributeSystem = true;
                _plUserDb.AttributeName = cardDb.AttributeSystemName;
            }
            if (cardDb.IsHaveCondition)
            {
                _plUserDb.PredicateName = "REQUIRE_CLICK_BUTTON_TO_NEXT_CARD";
                _plUserDb.PredicateValue = patternCard;
            }
            if (cardDb.CardStepID != null && cardDb.IsConditionWithInputText)
            {
                _plUserDb.PredicateName = "REQUIRE_INPUT_TEXT_TO_NEXT_CARD";
                _plUserDb.PredicateValue = CommonConstants.PostBackCard + cardDb.CardStepID;
            }
            if (cardDb.CardStepID != null && cardDb.IsConditionWithInputText == false)
            {
                _plUserDb.PredicateName = "AUTO_NEXT_CARD";
                _plUserDb.PredicateValue = CommonConstants.PostBackCard + cardDb.CardStepID;
            }
            if (cardDb.IsConditionWithAreaButton)
            {
                _plUserDb.PredicateName = "VERIFY_TEXT_WITH_AREA_BUTTON";
            }
            UpdateStatusAppUser(_plUserDb);
            return tempCardFb;
        }
        private string HandlePostbackModule(string postbackModule, string text, int botId, bool isFristRequest)
        {
            string templateHandle = "";
            _plUserDb.IsHaveSetAttributeSystem = false;
            _plUserDb.AttributeName = "";
            _plUserDb.PredicateName = "POSTBACK_MODULE";
            if (postbackModule.Contains(CommonConstants.ModuleSearchAPI))
            {
                string mdSearchId = postbackModule.Replace("postback_module_api_search_", "");
                if (isFristRequest)
                {
                    var handleMdSearch = _handleMdService.HandleIsSearchAPI(postbackModule, mdSearchId, "");
                    templateHandle = handleMdSearch.TemplateJsonFacebook;
                }
                else
                {
                    var handleMdSearch = _handleMdService.HandleIsSearchAPI(text, mdSearchId, "");
                    templateHandle = handleMdSearch.TemplateJsonFacebook;
                }
                _plUserDb.PredicateValue = postbackModule;
            }
            if (postbackModule.Contains(CommonConstants.ModuleAdminContact))
            {
                var handleAdminContact = _handleMdService.HandleIsAdminContact(text, botId);
                templateHandle = handleAdminContact.TemplateJsonFacebook;
                _plUserDb.PredicateValue = postbackModule;
            }

            UpdateStatusAppUser(_plUserDb);
            return templateHandle;
        }
        #endregion

        private ApplicationPlatformUser UpdateStatusAppUser(ApplicationPlatformUser appUserVm)
        {
            _appPlatformUser.Update(appUserVm);
            _appPlatformUser.Save();
            return appUserVm;
        }

        private string HandleMessageSymptomp(string msgJson, string senderId)
        {
            msgJson = msgJson.Replace("{{senderId}}", senderId);
            //msgJson = Regex.Replace(msgJson, "File/", Domain + "File/");
            msgJson = Regex.Replace(msgJson, "<br />", "\\n");
            msgJson = Regex.Replace(msgJson, "<br/>", "\\n");
            msgJson = Regex.Replace(msgJson, @"\\n\\n", "\\n");
            msgJson = Regex.Replace(msgJson, @"\\n\\r\\n", "\\n");
            return msgJson;
        }

        #region Send API Message Facebook
        private async Task SendMultiMessageTask(string templateJson, string sender)
        {
            templateJson = templateJson.Trim();
            if (templateJson.Contains("{{"))
            {
                if (_dicAttributeUser != null && _dicAttributeUser.Count() != 0)
                {
                    foreach (var item in _dicAttributeUser)
                    {
                        string val = String.IsNullOrEmpty(item.Value) == true ? "N/A" : item.Value;
                        templateJson = templateJson.Replace("{{" + item.Key + "}}", val);
                    }
                }
            }
            string[] strArrayJson = Regex.Split(templateJson, "split");
            foreach (var temp in strArrayJson)
            {
                string tempJson = temp;
                await SendMessage(tempJson, sender);
            }
        }

        private async Task SendMessage(string templateJson, string sender)
        {
            if (!String.IsNullOrEmpty(templateJson))
            {
                templateJson = templateJson.Replace("{{senderId}}", sender);
                templateJson = Regex.Replace(templateJson, "File/", Domain + "File/");
                templateJson = Regex.Replace(templateJson, "<br />", "\\n");
                templateJson = Regex.Replace(templateJson, "<br/>", "\\n");
                templateJson = Regex.Replace(templateJson, @"\\n\\n", "\\n");
                templateJson = Regex.Replace(templateJson, @"\\n\\r\\n", "\\n");
                _lstBotReplyResponse.Add(templateJson);

            }
        }

        #endregion

        private void AddAttributeDefault(string userId, int botId, string key, string value)
        {
            AttributePlatformUser attPlatformUser = new AttributePlatformUser();
            attPlatformUser.UserID = userId;
            attPlatformUser.BotID = botId;
            attPlatformUser.AttributeKey = key;
            attPlatformUser.AttributeValue = value;
            attPlatformUser.TypeDevice = "kiosk";
            _attributeService.CreateUpdateAttributePlatform(attPlatformUser);
            _attributeService.Save();
        }

        private void LogError(string message)
        {
            try
            {
                Error error = new Error();
                error.CreatedDate = DateTime.Now;
                error.Message = message;
                _errorService.Create(error);
                _errorService.Save();
            }
            catch (Exception ex)
            {

            }
        }
        private void AddHistory(HistoryViewModel hisVm)
        {
            History hisDb = new History();
            hisDb.UpdateHistory(hisVm);
            hisDb.Type = "kiosk";
            _historyService.Create(hisDb);
            _historyService.Save();
        }

        public class Message
        {
            public string senderId { set; get; }//K2542621755855091
            public string botId { set; get; }
            public string text { set; get; }
        }

        #region Convert AccentVN - Thêm dấu Tiếng việt
        private string GetPredictAccentVN(string text, bool isActive = false)
        {
            string textVN = text;
            if (isActive)
            {
                _accentService = AccentService.SingleInstance;
                textVN = _accentService.GetAccentVN(text);
            }
            return textVN;
        }
        #endregion

        #region --DATA SOURCE API--

        private string apiRelateQA = "/api/qa_for_all/get_related_pair";

        private string ApiAddUpdateQA(string NameFuncAPI, object T, string Type = "Post")
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
                        case "Get":
                            string requestUri = NameFuncAPI + "?" + httpContent;
                            response = client.GetAsync(requestUri).Result;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    return null;
                }
                if (response.IsSuccessStatusCode)
                {
                    result = response.Content.ReadAsStringAsync().Result;
                }
            }
            return result;
        }
        private string GetRelatedQuestionToFacebook(string nameFuncAPI, string question, string field, string number, string botId)
        {
            string result = "";
            if (String.IsNullOrEmpty(nameFuncAPI))
            {
                nameFuncAPI = apiRelateQA;
            }
            else
            {
                nameFuncAPI = nameFuncAPI.Replace("http://172.16.7.71:80", "").Trim();
            }
            var param = new
            {
                question = question,
                number = number,
                field = field,
                botid = botId
            };
            string responseString = ApiAddUpdateQA(nameFuncAPI, param, "Post");

            return responseString;
        }

        #endregion
        public class ResultBot
        {
            public string Type { set; get; }
            public int Total { set; get; }
            public string PatternPayload { set; get; }
        }

        #region CALL API SEARCH NATURAL LANGUAGE PROCESS
        private List<string> GetRelatedQuestion(string question, string field, string number, string botId)
        {
            List<string> _lstQuestion = new List<string>();
            string resultAPI = _apiNLR.GetRelatedPair(question, field, number, botId);
            if (!String.IsNullOrEmpty(resultAPI))
            {
                var lstQnaAPI = new JavaScriptSerializer
                {
                    MaxJsonLength = Int32.MaxValue,
                    RecursionLimit = 100
                }.Deserialize<List<SearchNlpQnAViewModel>>(resultAPI);
                // render template json generic
                int totalQnA = lstQnaAPI.Count();
                string totalFind = "Tôi tìm thấy " + totalQnA + " câu hỏi liên quan đến câu hỏi của bạn";
                string strTemplateTextFb = FacebookTemplate.GetMessageTemplateText(totalFind, "{{senderId}}").ToString();
                string strTemplateGenericRelatedQuestion = FacebookTemplate.GetMessageTemplateGenericByList("{{senderId}}", lstQnaAPI).ToString();
                _lstQuestion.Add(strTemplateTextFb);
                _lstQuestion.Add(strTemplateGenericRelatedQuestion);
            }

            return _lstQuestion;
        }
        private List<string> GetSymptoms(string text)
        {
            List<string> _lstSymptoms = new List<string>();
            string resultSymptomp = _apiNLR.GetListSymptoms(text, 1);
            if (!String.IsNullOrEmpty(resultSymptomp))
            {
                var dataSymptomp = new JavaScriptSerializer
                {
                    MaxJsonLength = Int32.MaxValue,
                    RecursionLimit = 100
                }.Deserialize<List<SearchSymptomViewModel>>(resultSymptomp);
                if (dataSymptomp.Count() != 0)
                {
                    string msgSymptoms = "Bạn vui lòng xem thêm thông tin triệu chứng bên dưới";
                    string strTemplateTextFb = FacebookTemplate.GetMessageTemplateText(msgSymptoms, "{{senderId}}").ToString();
                    string strTemplateGenericMedicalSymptoms = FacebookTemplate.GetMessageTemplateGenericByListMed("{{senderId}}", dataSymptomp).ToString();
                    _lstSymptoms.Add(strTemplateTextFb);
                    _lstSymptoms.Add(strTemplateGenericMedicalSymptoms);
                }
            }
            return _lstSymptoms;
        }
        #endregion
    }
}
