using AIMLbot;
using AutoMapper;
using BotProject.Common;
using BotProject.Common.AppThird3PartyTemplate;
using BotProject.Common.ViewModels;
using BotProject.Model.Models;
using BotProject.Service;
using BotProject.Web.Infrastructure.Core;
using BotProject.Web.Infrastructure.Extensions;
using BotProject.Web.Infrastructure.Log4Net;
using BotProject.Web.Models;
using Common.Logging.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Script.Serialization;

namespace BotProject.Web.API_Webhook
{
    /// <summary>
    /// Developer Facebook 
    /// http://developers.facebook.com
    /// ------------------------------
    /// Webhook nhận tín hiệu dữ liệu tin nhắn gửi tới từ người dùng trên
    /// nền tảng Facebook.
    /// </summary>
    public class FacebookController : ApiController
    {
        // appSettings
        string pageToken = "";
        string appSecret = "";
        string verifytoken = "lacviet_bot_chat";
        private Dictionary<string, string> _dicAttributeUser = new Dictionary<string, string>();
        private readonly string Domain = Helper.ReadString("Domain");

        private static readonly HttpClient _client = new HttpClient();

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

        /// <summary>
        /// Thời gian chờ để phản hồi lại tin nhắn,thời gian tính từ tin nhắn cuối cùng
        /// người dùng không tương tác lại
        /// </summary>
        private int TIMEOUT_DELAY_SEND_MESSAGE = 60;

        /// <summary>
        /// Thẻ bắt đầu khi lần đầu tiên người dùng tương tác
        /// </summary>
        private string CARD_GET_STARTED = "danh mục"; //default "danh mục knowledgebase"

        private string TITLE_PAYLOAD_QUICKREPLY = "";

        private string _contactAdmin = Helper.ReadString("AdminContact");
        private string _titlePayloadContactAdmin = Helper.ReadString("TitlePayloadAdminContact");

        // Pattern kiểm tra là số
        private const string NumberPattern = @"^\d+$";

        // Điều kiện có mở search engine
        bool _isSearchAI = false;

        //tin nhắn vắng mặt
        string _messageAbsent = "";
        bool _isHaveMessageAbsent = false;

        //tin nhắn phản hồi chờ
        string _messageProactive = "";
        string _patternCardPayloadProactive = "";
        string _titleCardPayloadProactive = "🔙 Quay về";

        // Model user
        ApplicationFacebookUser _fbUser;

        // BOT PRIVATE CUSTOMIZE
        private const int BOT_Y_TE = 3019;

        private string _botID;


        // AIML Bot Services
        private AIMLBotService _aimlBotService;

        private AIMLbot.Bot _botService;

        // Services
        private IApplicationFacebookUserService _appFacebookUser;
        //private BotServiceMedical _botService;
        private ISettingService _settingService;
        private IHandleModuleServiceService _handleMdService;
        private IErrorService _errorService;
        private IAIMLFileService _aimlFileService;
        private ApiQnaNLRService _apiNLR;
        private IHistoryService _historyService;
        private ICardService _cardService;
        private AccentService _accentService;
        private IAttributeSystemService _attributeService;
        private User _user;

        public FacebookController(IApplicationFacebookUserService appFacebookUser,
                                  ISettingService settingService,
                                  IHandleModuleServiceService handleMdService,
                                  IErrorService errorService,
                                  IAIMLFileService aimlFileService,
                                  IHistoryService historyService,
                                  ICardService cardService,
                                  IAttributeSystemService attributeService)
        {
            _errorService = errorService;
            _appFacebookUser = appFacebookUser;
            _settingService = settingService;
            _historyService = historyService;
            _cardService = cardService;
            _attributeService = attributeService;
            _handleMdService = handleMdService;
            _aimlFileService = aimlFileService;
            //_botService = BotServiceMedical.BotInstance;

            _aimlBotService = AIMLBotService.AIMLBotInstance;

            //_accentService = AccentService.SingleInstance;
            _apiNLR = new ApiQnaNLRService();
            _fbUser = new ApplicationFacebookUser();
        }

        /// <summary>
        /// Facebook kiểm tra mã bảo mật của đường dẫn webhook thiết lập
        /// </summary>
        /// <returns></returns>
        public HttpResponseMessage Get(string botID)
        {
            var querystrings = Request.GetQueryNameValuePairs().ToDictionary(x => x.Key, x => x.Value);
            if (querystrings["hub.verify_token"] == verifytoken)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(querystrings["hub.challenge"], Encoding.UTF8, "text/plain")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Nhận tin nhắn từ người dùng facebook
        /// FacebookBotRequest
        /// <param></param>
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<HttpResponseMessage> Post(string botID = "")
        {
            if (String.IsNullOrEmpty(botID))
                return new HttpResponseMessage(HttpStatusCode.BadRequest);

            int botId = Int32.Parse(botID);

            var signature = Request.Headers.GetValues("X-Hub-Signature").FirstOrDefault().Replace("sha1=", "");

            var body = await Request.Content.ReadAsStringAsync();
            //BotLog.Info("FACEBOOK" + body);
            FacebookBotRequest objMsgUser = JsonConvert.DeserializeObject<FacebookBotRequest>(body);
            if (objMsgUser.@object != "page")
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            Setting settingDb = _settingService.GetSettingByBotID(botId);
            pageToken = settingDb.FacebookPageToken;
            appSecret = settingDb.FacebookAppSecrect;
            _isSearchAI = settingDb.IsMDSearch;

            if (!VerifySignature(signature, body))
                return new HttpResponseMessage(HttpStatusCode.BadRequest);

            // Khởi động lấy "brain" của bot service theo id
            GetServerAIMLBot(botID);
            // Khởi tạo user theo bot service
            InitUserByServerAIMLBot(objMsgUser.entry[0].messaging[0].sender.id);

            foreach (var item in objMsgUser.entry[0].messaging)
            {
                if (item.message == null && item.postback == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
                var lstAttribute = _attributeService.GetListAttributeFacebook(item.sender.id, botId).ToList();
                if (lstAttribute.Count() != 0)
                {
                    _dicAttributeUser = new Dictionary<string, string>();
                    foreach (var attr in lstAttribute)
                    {
                        _dicAttributeUser.Add(attr.AttributeKey, attr.AttributeValue);
                    }
                }
                if (item.message == null && item.postback != null)
                {
                    await ExcuteMessage(item.postback.payload, item.sender.id, botId, item.timestamp, "payload_postback");
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
                else
                {
                    if (item.message.quick_reply != null)
                    {
                        await ExcuteMessage(item.message.quick_reply.payload, item.sender.id, botId, item.timestamp, "payload_postback");
                        return new HttpResponseMessage(HttpStatusCode.OK);
                    }
                    else if (item.message.attachments != null && item.message.attachments[0].type == "audio")
                    {
                        string urlAudio = item.message.attachments[0].payload.url;
                        var rsAudioToTextJson = await SpeechReconitionVNService.ConvertSpeechToTextAsync(urlAudio);
                        if (String.IsNullOrEmpty(rsAudioToTextJson))
                        {
                            return new HttpResponseMessage(HttpStatusCode.OK);
                        }

                        dynamic stuff = JsonConvert.DeserializeObject(rsAudioToTextJson);

                        string status = stuff.status;
                        if (status == "0")
                        {
                            string text = stuff.hypotheses[0].utterance;
                            if (!String.IsNullOrEmpty(text))
                            {
                                string meanTextFromAudio = FacebookTemplate.GetMessageTemplateText("Ý bạn là: " + text, item.sender.id).ToString();
                                await SendMessage(meanTextFromAudio, item.sender.id);
                                text = Regex.Replace(text, @"\.", "");
                                await ExcuteMessage(text, item.sender.id, botId, item.timestamp, "audito");                               
                            }
                            return new HttpResponseMessage(HttpStatusCode.OK);
                        }
                        else if (status == "1")
                        {
                            string meanTextFromAudio = FacebookTemplate.GetMessageTemplateText("Không nhận được tín hiệu âm thanh", item.sender.id).ToString();
                            await SendMessage(meanTextFromAudio, item.sender.id);
                            return new HttpResponseMessage(HttpStatusCode.OK);
                        }
                        else if (status == "2")
                        {
                            string meanTextFromAudio = FacebookTemplate.GetMessageTemplateText("Xử lý âm thanh bị hủy", item.sender.id).ToString();
                            await SendMessage(meanTextFromAudio, item.sender.id);
                            return new HttpResponseMessage(HttpStatusCode.OK);
                        }
                        else if (status == "9")
                        {
                            string meanTextFromAudio = FacebookTemplate.GetMessageTemplateText("Hệ thống xử lý âm thanh đang bận", item.sender.id).ToString();
                            await SendMessage(meanTextFromAudio, item.sender.id);
                            return new HttpResponseMessage(HttpStatusCode.OK);
                        }
                    }
                    else
                    {
                        await ExcuteMessage(item.message.text, item.sender.id, botId, item.timestamp, "text");
                        return new HttpResponseMessage(HttpStatusCode.OK);
                    }
                }
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        private void GetServerAIMLBot(string botId)
        {
            _botService = _aimlBotService.GetServerBot(botId);
            string pathAIML2Graphmaster = ConfigurationManager.AppSettings["AIML2GraphmasterPath"] + "BotID_" + botId + ".bin";
            _aimlBotService.LoadGraphmasterFromAIMLBinaryFile(pathAIML2Graphmaster, _botService);
        }
        private void InitUserByServerAIMLBot(string senderId)
        {
            _user = _aimlBotService.loadUserBot(senderId, _botService);
        }

        /// <summary>
        /// Xử lý tin nhắn facebook
        /// </summary>
        /// <param name="text"></param>
        /// <param name="sender"></param>
        /// <param name="botId"></param>
        /// <param name="timeStamp"></param>
        /// <param name="typeBotRequest"></param>
        /// <returns></returns>
        private async Task<HttpResponseMessage> ExcuteMessage(string text,
                                                              string sender,
                                                              int botId,
                                                              string timeStamp,
                                                              string typeRequest)
        {
            if (String.IsNullOrEmpty(text) || String.IsNullOrEmpty(sender))
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            //Kiểm tra duplicate request trong cùng một thời gian
            if (!String.IsNullOrEmpty(timeStamp))
            {
                var rs = _appFacebookUser.CheckDuplicateRequestWithTimeStamp(timeStamp, sender);
                if (rs == 4)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
            }

            text = HttpUtility.HtmlDecode(text);
            text = Regex.Replace(text, @"<(.|\n)*?>", "").Trim(); // remove tag html
            text = Regex.Replace(text, @"\p{Cs}", "").Trim();// remove emoji

            // Typing writing...
            await SendMessageTyping(sender);

            // Lấy thông tin người dùng
            _fbUser = GetUserById(sender, botId);

            
            // Input text
            if (typeRequest == CommonConstants.BOT_REQUEST_TEXT)
            {
                // Thêm dấu tiếng việt
                bool isActive = true;
                // Kiểm tra nếu chuỗi không có chứa ký tự unicode sẽ chuyển qua tự thêm dấu
                if (text.Any(c => c > 255) == false)
                {
                    string textAccentVN = GetPredictAccentVN(text, isActive);

                    if (!String.IsNullOrEmpty(textAccentVN))
                    {
                        if (textAccentVN != text.ToLower())
                        {
                            string msg = FacebookTemplate.GetMessageTemplateText("Ý bạn là: " + textAccentVN + "", sender).ToString();
                            await SendMessage(msg, sender);
                        }
                        text = textAccentVN;
                    }
                }                                  
            }

            if (typeRequest == CommonConstants.BOT_REQUEST_TEXT || typeRequest == CommonConstants.BOT_REQUEST_AUDIO)
            {
                if (botId == BOT_Y_TE)
                {
                    AttributeFacebookUser attFbUser = new AttributeFacebookUser();
                    attFbUser.AttributeKey = "content_message";
                    attFbUser.AttributeValue = text;
                    attFbUser.BotID = botId;
                    attFbUser.UserID = sender;
                    _dicAttributeUser.Remove("content_message");
                    _dicAttributeUser.Add("content_message", text);
                    _attributeService.CreateUpdateAttributeFacebook(attFbUser);
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
            if (_fbUser.IsHaveSetAttributeSystem)
            {
                AttributeFacebookUser attFbUser = new AttributeFacebookUser();
                attFbUser.AttributeKey = _fbUser.AttributeName;
                attFbUser.BotID = botId;
                attFbUser.UserID = sender;
                bool isUpdateAttr = false;
                if (typeRequest == CommonConstants.BOT_REQUEST_TEXT)
                {
                    attFbUser.AttributeValue = text;
                    isUpdateAttr = true;
                }
                if (typeRequest == CommonConstants.BOT_REQUEST_PAYLOAD_POSTBACK)
                {
                    if (!String.IsNullOrEmpty(TITLE_PAYLOAD_QUICKREPLY))
                    {
                        attFbUser.AttributeValue = TITLE_PAYLOAD_QUICKREPLY;
                        isUpdateAttr = true;
                    }
                }
                if (isUpdateAttr)
                {
                    // Kiểm tra giá trị nhập vào theo từng thuộc tính
                    // Bot Y Tế
                    // Tuổi
                    if(attFbUser.AttributeKey == "age")
                    {
                        bool isAge = Regex.Match(text, NumberPattern).Success;
                        if (isAge)
                        {
                            attFbUser.AttributeValue = text;
                        }
                        else
                        {
                            string msg = FacebookTemplate.GetMessageTemplateText("Ký tự phải là số, Anh/chị vui lòng nhập lại độ tuổi", sender).ToString();
                            await SendMessage(msg, sender);
                            return new HttpResponseMessage(HttpStatusCode.OK);
                        }
                    }

                    _dicAttributeUser.Remove(attFbUser.AttributeKey);
                    _dicAttributeUser.Add(attFbUser.AttributeKey, attFbUser.AttributeValue);
                    _attributeService.CreateUpdateAttributeFacebook(attFbUser);
                }
            }
            if (_fbUser.PredicateName == "REQUIRE_CLICK_BUTTON_TO_NEXT_CARD")
            {
                if (typeRequest == CommonConstants.BOT_REQUEST_TEXT)
                {
                    string contentRequireClick = FacebookTemplate.GetMessageTemplateText("Anh/chị vui lòng chọn lại thông tin bên dưới", sender).ToString();
                    await SendMessage(contentRequireClick, sender);
                    string partternCardRequireClick = _fbUser.PredicateValue;
                    string templateCardRequireClick = HandlePostbackCard(partternCardRequireClick, botId);
                    await SendMultiMessageTask(templateCardRequireClick, sender);
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
            }
            else if (_fbUser.PredicateName == "REQUIRE_INPUT_TEXT_TO_NEXT_CARD")
            {
                if (typeRequest == CommonConstants.BOT_REQUEST_TEXT)
                {
                    string partternCardRequireInput = _fbUser.PredicateValue;
                    string templateCardRequireInput = HandlePostbackCard(partternCardRequireInput, botId);
                    await SendMultiMessageTask(templateCardRequireInput, sender);
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
            }
            else if (_fbUser.PredicateName == "VERIFY_TEXT_WITH_AREA_BUTTON")
            {
                if (typeRequest == CommonConstants.BOT_REQUEST_TEXT)
                {
                    var cardDb = _cardService.GetCardByPattern(_fbUser.PredicateValue);
                    if (cardDb == null)
                    {
                        return new HttpResponseMessage(HttpStatusCode.OK);
                    }
                    string area = cardDb.Name;
                    text = text + " " + area;
                }
            }
            else if (_fbUser.PredicateName == "POSTBACK_MODULE")
            {
                if (typeRequest == CommonConstants.BOT_REQUEST_TEXT)
                {
                    string postbackModule = _fbUser.PredicateValue;
                    string templateModule = HandlePostbackModule(postbackModule, text, botId, false);
                    await SendMultiMessageTask(templateModule, sender);
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
            }

            // print postback card
            if (typeRequest == CommonConstants.BOT_REQUEST_PAYLOAD_POSTBACK)
            {
                string templateCard = HandlePostbackCard(text, botId);
                await SendMultiMessageTask(templateCard, sender);
                if (_fbUser.PredicateName == "AUTO_NEXT_CARD")
                {
                    string partternNextCard = _fbUser.PredicateValue;
                    string templateNextCard = HandlePostbackCard(partternNextCard, botId);
                    await SendMultiMessageTask(templateNextCard, sender);
                }
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            AIMLbot.Result rsAIMLBot = GetBotReplyFromAIMLBot(text);
            ResultBot rsBOT = new ResultBot();
            rsBOT = CheckTypePostbackFromResultBotReply(rsAIMLBot);
            if (botId.ToString() == "5041" || botId.ToString() == "5072")
            {
                // Kiểm tra nếu chứa từ khóa về tìm kiếm văn bản luật thì gọi tìm văn bản               
                if (CheckIfContainsLegal(text))
                {
                    await HandleSearchAPI(botId, text, sender);
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
            }
             
            if (rsBOT.Type == POSTBACK_MODULE)
            {
                string templateModule = HandlePostbackModule(rsBOT.PatternPayload, text, botId, true);
                await SendMessage(templateModule, sender);
            }
            if (rsBOT.Type == POSTBACK_CARD)
            {
                string templateCard = HandlePostbackCard(rsBOT.PatternPayload, botId);
                await SendMultiMessageTask(templateCard, sender); // print message card
                if (_fbUser.PredicateName == "AUTO_NEXT_CARD") // print message card kế tiếp nếu có
                {
                    string partternNextCard = _fbUser.PredicateValue;
                    templateCard = HandlePostbackCard(partternNextCard, botId);
                    await SendMultiMessageTask(templateCard, sender);
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
                                await SendMessage(symp, sender);
                            }
                        }
                    }

                }
            }
            if (rsBOT.Type == POSTBACK_TEXT)
            {
                string templateText = FacebookTemplate.GetMessageTemplateText(rsBOT.PatternPayload, sender).ToString();
                await SendMessage(templateText, sender);
            }
            if (rsBOT.Type == POSTBACK_NOT_MATCH)
            {
                if (_isSearchAI)
                {
                    await HandleSearchAPI(botId, text, sender);
                }
                else
                {
                    await SendMessageNotFound(sender);
                }                              
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
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
            _fbUser.PredicateName = "";
            _fbUser.PredicateValue = "";
            _fbUser.IsHaveSetAttributeSystem = false;
            _fbUser.AttributeName = "";
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
                _fbUser.IsHaveSetAttributeSystem = true;
                _fbUser.AttributeName = cardDb.AttributeSystemName;
            }
            if (cardDb.IsHaveCondition)
            {
                _fbUser.PredicateName = "REQUIRE_CLICK_BUTTON_TO_NEXT_CARD";
                _fbUser.PredicateValue = patternCard;
            }
            if (cardDb.CardStepID != null && cardDb.IsConditionWithInputText)
            {
                _fbUser.PredicateName = "REQUIRE_INPUT_TEXT_TO_NEXT_CARD";
                _fbUser.PredicateValue = CommonConstants.PostBackCard + cardDb.CardStepID;
            }
            if (cardDb.CardStepID != null && cardDb.IsConditionWithInputText == false)
            {
                _fbUser.PredicateName = "AUTO_NEXT_CARD";
                _fbUser.PredicateValue = CommonConstants.PostBackCard + cardDb.CardStepID;
            }
            if (cardDb.IsConditionWithAreaButton)
            {
                _fbUser.PredicateName = "VERIFY_TEXT_WITH_AREA_BUTTON";
            }
            UpdateStatusFacebookUser(_fbUser);
            return tempCardFb;
        }
        private string HandlePostbackModule(string postbackModule, string text, int botId, bool isFristRequest)
        {
            string templateHandle = "";
            _fbUser.IsHaveSetAttributeSystem = false;
            _fbUser.AttributeName = "";
            _fbUser.PredicateName = "POSTBACK_MODULE";
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
                _fbUser.PredicateValue = postbackModule;
            }
            if (postbackModule.Contains(CommonConstants.ModuleAdminContact))
            {
                var handleAdminContact = _handleMdService.HandleIsAdminContact(text, botId);
                templateHandle = handleAdminContact.TemplateJsonFacebook;
                _fbUser.PredicateValue = postbackModule;
            }

            UpdateStatusFacebookUser(_fbUser);
            return templateHandle;
        }
        #endregion


        #region Create update and get Facebook user
        private ApplicationFacebookUser UpdateStatusFacebookUser(ApplicationFacebookUser fbUserVm)
        {
            _appFacebookUser.Update(fbUserVm);
            _appFacebookUser.Save();
            return fbUserVm;
        }
        private ApplicationFacebookUser GetUserById(string senderId, int botId)
        {
            ApplicationFacebookUser fbUserDb = new ApplicationFacebookUser();
            fbUserDb = _appFacebookUser.GetByUserId(senderId);
            if (fbUserDb == null)
            {
                ProfileUser profileUser = new ProfileUser();
                profileUser = GetProfileUser(senderId);

                fbUserDb = new ApplicationFacebookUser();
                fbUserDb.UserId = senderId;
                fbUserDb.IsHavePredicate = false;
                fbUserDb.IsProactiveMessage = false;
                fbUserDb.TimeOut = DateTime.Now.AddSeconds(TIMEOUT_DELAY_SEND_MESSAGE);
                fbUserDb.CreatedDate = DateTime.Now;
                fbUserDb.StartedOn = DateTime.Now;
                fbUserDb.FirstName = profileUser.first_name;
                fbUserDb.Age = 0;// "N/A";
                fbUserDb.LastName = profileUser.last_name;
                fbUserDb.UserName = profileUser.first_name + " " + profileUser.last_name;
                fbUserDb.Gender = true; //"N/A";
                _appFacebookUser.Add(fbUserDb);
                _appFacebookUser.Save();

                AddAttributeDefault(senderId, botId, "sender_name", fbUserDb.UserName);
            }
            return fbUserDb;
        }

        private ProfileUser GetProfileUser(string senderId)
        {
            ProfileUser user = new ProfileUser();
            HttpResponseMessage res = new HttpResponseMessage();
            res = _client.GetAsync($"https://graph.facebook.com/" + senderId + "?fields=first_name,last_name,profile_pic&access_token=" + pageToken).Result;//gender y/c khi sử dụng
            if (res.IsSuccessStatusCode)
            {
                var serializer = new JavaScriptSerializer();
                serializer.MaxJsonLength = Int32.MaxValue;
                user = serializer.Deserialize<ProfileUser>(res.Content.ReadAsStringAsync().Result);
            }
            return user;
        }

        private void AddAttributeDefault(string userId, int BotId, string key, string value)
        {
            AttributeFacebookUser attFbUser = new AttributeFacebookUser();
            attFbUser.UserID = userId;
            attFbUser.BotID = BotId;
            attFbUser.AttributeKey = key;
            attFbUser.AttributeValue = value;
            _attributeService.CreateUpdateAttributeFacebook(attFbUser);
            _attributeService.Save();
        }
        #endregion

        #region Send API Message Facebook
        private async Task HandleSearchAPI(int botId, string text, string sender)
        {
            HistoryViewModel hisVm = new HistoryViewModel();
            hisVm.BotID = botId;
            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_002;
            hisVm.CreatedDate = DateTime.Now;
            hisVm.UserSay = text;
            hisVm.UserName = sender;
            hisVm.Type = CommonConstants.TYPE_FACEBOOK;
            AddHistory(hisVm);

            List<string> lstSymptoms = new List<string>();
            if (botId == BOT_Y_TE)
            {
                lstSymptoms = GetSymptoms(text);
                if (lstSymptoms.Count() != 0)
                {
                    foreach (var symp in lstSymptoms)
                    {
                        await SendMessage(symp, sender);
                    }
                }
            }
            //List<string> lstFaq = new List<string>();
            //lstFaq = GetRelatedQuestion(text, "0", "5", botId.ToString());
            //if (lstFaq.Count() != 0)
            //{
            //    foreach (var faq in lstFaq)
            //    {
            //        await SendMessage(faq, sender);
            //    }
            //}

            List<string> lstLegalDocs = new List<string>();
            List<string> lstArticles = new List<string>();
            if (botId.ToString() == "5041" || botId.ToString() == "5072")
            {
                if (CheckIfContainsLegal(text))
                {
                    lstLegalDocs = GetModuleApiSearchLegal(text, "keyword", "https://trogiupluat.vn", "", "get");
                    if (lstLegalDocs.Count() != 0)
                    {
                        foreach (var legal in lstLegalDocs)
                        {
                            await SendMessage(legal, sender);
                        }
                    }
                    else
                    {
                        lstArticles = GetModuleApiSearchArticle(text, "keyword", "https://trogiupluat.vn", "", "get");
                        if (lstArticles.Count() != 0)
                        {
                            foreach (var article in lstArticles)
                            {
                                await SendMessage(article, sender);
                            }
                        }
                    }
                }
            }

            if (lstSymptoms.Count() == 0 && lstLegalDocs.Count() == 0 && lstArticles.Count() == 0)
            {
                hisVm.BotID = botId;
                hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_008;
                hisVm.CreatedDate = DateTime.Now;
                hisVm.UserSay = text;
                hisVm.UserName = sender;
                hisVm.Type = CommonConstants.TYPE_FACEBOOK;
                AddHistory(hisVm);
                await SendMessageNotFound(sender);
            }
        }

        private async Task SendMessageNotFound(string sender)
        {
            List<string> keyList = new List<string>(_DICTIONARY_NOT_MATCH.Keys);
            Random rand = new Random();
            string randomKey = keyList[rand.Next(keyList.Count)];
            string contentNotFound = _DICTIONARY_NOT_MATCH[randomKey];

            //string templateNotFound = FacebookTemplate.GetMessageTemplateTextAndQuickReply(
            //    contentNotFound, sender, _contactAdmin, _titlePayloadContactAdmin).ToString();

            string templateNotFound = FacebookTemplate.GetMessageTemplateText(
                contentNotFound, sender).ToString();

            await SendMessage(templateNotFound, sender);
        }

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

        /// <summary>
        /// Hiển thị trạng thái dấu 3 chấm bot đang viết gì đó
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        private async Task SendMessageTyping(string sender)
        {
            string senderActionTyping = FacebookTemplate.GetMessageSenderAction("typing_on", sender).ToString();
            await SendMessage(senderActionTyping, sender);
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
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage res = await _client.PostAsync($"https://graph.facebook.com/v3.2/me/messages?access_token=" + pageToken + "",
                new StringContent(templateJson, Encoding.UTF8, "application/json"));
            }
        }

        #endregion

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

        private string apiSearchLegal = "/api/legal/SearchLegalDoc";
        private string apiSearchArticle = "/api/article/search-relate";
        private string urlAPISearchLegal = "https://trogiupluat.vn";

        private List<string> GetModuleApiSearchLegal(string contentText, string param, string urlAPI, string keyAPI, string methodeHttp)
        {
            List<string> lstLegalDocs = new List<string>();

            param = "keyword=" + contentText;
            string result = ExcuteModuleSearchAPI(apiSearchLegal, param, urlAPISearchLegal, keyAPI, methodeHttp);
            if (!String.IsNullOrEmpty(result))
            {
                var dataListLegal = new JavaScriptSerializer
                {
                    MaxJsonLength = Int32.MaxValue,
                    RecursionLimit = 100
                }.Deserialize<List<LegalApiModel>>(result);
                if (dataListLegal.Count() != 0)
                {
                    string resultTotal = "Tìm thấy " + dataListLegal.Count() + " kết quả liên quan văn bản luật";
                    lstLegalDocs.Add(FacebookTemplate.GetMessageTemplateText(resultTotal, "{{senderId}}").ToString());
                    lstLegalDocs.Add(FacebookTemplate.GetMessageTemplateGenericByListLegal("{{senderId}}", dataListLegal.Take(4).ToList(), "").ToString());
                }
            }
            return lstLegalDocs;
        }

        private List<string> GetModuleApiSearchArticle(string contentText, string param, string urlAPI, string keyAPI, string methodeHttp)
        {
            List<string> lstArticles = new List<string>();

            param = "keyword=" + contentText;
            string result = ExcuteModuleSearchAPI(apiSearchArticle, param, urlAPISearchLegal, keyAPI, methodeHttp);

            if (!String.IsNullOrEmpty(result))
            {
                var resultArticles = new JavaScriptSerializer
                {
                    MaxJsonLength = Int32.MaxValue,
                    RecursionLimit = 100
                }.Deserialize<Dictionary<string, string>>(result);

                string totalArticle = resultArticles["total"];
                if (totalArticle != "0")
                {
                    string resultTotal = "Tìm thấy " + totalArticle + " kết quả liên quan điều luật";
                    string url = "https://trogiupluat.vn/dieu-luat-lien-quan.html?content=" + contentText;
                    lstArticles.Add(FacebookTemplate.GetMessageTemplateTextAndButtonLink(resultTotal, "{{senderId}}", url, "Xem chi tiết").ToString());
                }
            }
            return lstArticles;
        }
        private string ExcuteModuleSearchAPI(string NameFuncAPI, string param, string UrlAPI, string KeySecrectAPI, string Type = "Post")
        {
            string result = null;
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(UrlAPI);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!String.IsNullOrEmpty(KeySecrectAPI))
                {
                    string[] key = KeySecrectAPI.Split(':');
                    client.DefaultRequestHeaders.Add(key[0], key[1]);
                }
                HttpResponseMessage response = new HttpResponseMessage();
                param = Uri.UnescapeDataString(param);
                var dict = HttpUtility.ParseQueryString(param);
                string json = JsonConvert.SerializeObject(dict.Cast<string>().ToDictionary(k => k, v => dict[v]));

                StringContent httpContent = new StringContent(json, UnicodeEncoding.UTF8, "application/json");
                try
                {
                    if (Type.ToUpper().Equals(Common.CommonConstants.MethodeHTTP_POST))
                    {
                        response = client.PostAsync(NameFuncAPI, httpContent).Result;
                    }
                    else if (Type.ToUpper().Equals(Common.CommonConstants.MethodeHTTP_GET))
                    {
                        string requestUri = NameFuncAPI + "?" + param;
                        response = client.GetAsync(requestUri).Result;
                    }
                }
                catch (Exception ex)
                {
                    return String.Empty;
                }
                if (response.IsSuccessStatusCode)
                {
                    result = response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    result = String.Empty;
                }
            }
            return result;
        }

        #endregion

        #region VerifySignatureFacebook
        private bool VerifySignature(string signature, string body)
        {
            var hashString = new StringBuilder();
            using (var crypto = new HMACSHA1(Encoding.UTF8.GetBytes(appSecret)))
            {
                var hash = crypto.ComputeHash(Encoding.UTF8.GetBytes(body));
                foreach (var item in hash)
                    hashString.Append(item.ToString("X2"));
            }

            return hashString.ToString().ToLower() == signature.ToLower();
        }
        #endregion

        #region Convert AccentVN - Thêm dấu Tiếng việt
        private string GetPredictAccentVN(string text, bool isActive = false)
        {
            string textVN = text;
            if (isActive)
            {
                try
                {
                    _accentService = AccentService.SingleInstance;
                    textVN = _accentService.GetAccentVN(text);
                }
                catch(Exception ex)
                {
                    BotLog.Error(ex.StackTrace + " " + ex.InnerException.Message + ex.Message);
                }
            }
            return textVN;
        }
        #endregion

        private bool CheckIfContainsLegal(string text)
        {
            bool hasLegal = false;
            // Kiểm tra nếu chứa từ khóa về tìm kiếm văn bản luật thì gọi tìm văn bản
            string[] arrayDocument = new string[] {
                        "Hiến pháp","Bộ luật","Luật","Pháp lệnh","Lệnh","Nghị quyết","Nghị quyết liên tịch","Nghị định",
                        "Quyết định","Thông tư","Thông tư liên tịch","Chỉ thị","Công điện","Báo cáo","Biên bản","Công văn",
                        "Điều lệ","Đính chính","Quy chế","Quy định","Quy trình","Quy chế phối hợp","Thông báo","Thông báo liên tịch",
                        "Thông cáo", "Chỉ dẫn áp dụng văn bản luật","điều","văn bản"
                    };
            foreach (var docType in arrayDocument)
            {
                if (text.ToLower().Contains(docType.ToLower()))
                {
                    hasLegal = true;
                    break;
                }
            }
            return hasLegal;
        }

        private void AddHistory(HistoryViewModel hisVm)
        {
            History hisDb = new History();
            hisDb.UpdateHistory(hisVm);
            _historyService.Create(hisDb);
            _historyService.Save();
        }


        public class ProfileUser
        {
            public string first_name { set; get; }
            public string last_name { set; get; }
            public string profile_pic { set; get; }
            public string id { set; get; }
            public string gender { set; get; }
        }

        public class ResultBot
        {
            public string Type { set; get; }
            public int Total { set; get; }
            public string PatternPayload { set; get; }
        }
    }
}
