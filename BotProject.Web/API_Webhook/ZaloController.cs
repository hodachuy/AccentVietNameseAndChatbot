using AIMLbot;
using BotProject.Common;
using BotProject.Common.AppThird3PartyTemplate;
using BotProject.Common.ViewModels;
using BotProject.Model.Models;
using BotProject.Service;
using BotProject.Web.Infrastructure.Core;
using BotProject.Web.Infrastructure.Extensions;
using BotProject.Web.Infrastructure.Log4Net;
using BotProject.Web.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
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
    /// <summary>
    /// Developer Zalo 
    /// https://developers.zalo.me/
    /// ------------------------------
    /// Webhook nhận tín hiệu dữ liệu tin nhắn gửi tới từ người dùng trên
    /// nền tảng Zalo.
    /// </summary>
    public class ZaloController : ApiController
    {
        // appSettings
        string pageToken = "";
        string appSecret = "";
        string verifytoken = "lacviet_bot_chat";
        private Dictionary<string, string> _dicAttributeUser = new Dictionary<string, string>();

        private static readonly HttpClient _client = new HttpClient();

        private readonly string Domain = Helper.ReadString("Domain");
        private readonly string UrlAPI = Helper.ReadString("UrlAPI");
        private readonly string KeyAPI = Helper.ReadString("KeyAPI");
        private string pathAIML = PathServer.PathAIML;
        private string pathSetting = PathServer.PathAIML + "config";

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

        // Model user
        ApplicationZaloUser _zaloUser;

        // BOT PRIVATE CUSTOMIZE
        private const int BOT_Y_TE = 3019;


        // Điều kiện có mở search engine
        bool _isSearchAI = false;

        //tin nhắn vắng mặt
        string _messageAbsent = "";
        bool _isHaveMessageAbsent = false;

        //tin nhắn phản hồi chờ
        string _messageProactive = "";
        string _patternCardPayloadProactive = "";
        string _titleCardPayloadProactive = "🔙 Quay về";

        private string _botID;


        // AIML Bot Services
        private AIMLBotService _aimlBotService;

        private AIMLbot.Bot _botService;

        // Services
        private IApplicationZaloUserService _appZaloUser;
        private ISettingService _settingService;
        private IHandleModuleServiceService _handleMdService;
        private IErrorService _errorService;
        private IAIMLFileService _aimlFileService;
        private ApiQnaNLRService _apiNLR;
        private IHistoryService _historyService;
        private ICardService _cardService;
        private AccentService _accentService;
        private IAttributeSystemService _attributeService;
        private IApplicationThirdPartyService _app3rd;
        private User _user;

        public ZaloController(IApplicationZaloUserService appZaloUser,
                                  ISettingService settingService,
                                  IHandleModuleServiceService handleMdService,
                                  IErrorService errorService,
                                  IAIMLFileService aimlFileService,
                                  IHistoryService historyService,
                                  ICardService cardService,
                                  IAttributeSystemService attributeService,
                                  IApplicationThirdPartyService app3rd)
        {
            _errorService = errorService;
            _appZaloUser = appZaloUser;
            _settingService = settingService;
            _historyService = historyService;
            _cardService = cardService;
            _attributeService = attributeService;
            _handleMdService = handleMdService;
            _aimlFileService = aimlFileService;
            _aimlBotService = AIMLBotService.AIMLBotInstance;
            _app3rd = app3rd;
            _apiNLR = new ApiQnaNLRService();
            _zaloUser = new ApplicationZaloUser();
        }

        public HttpResponseMessage Get(string botId)
        {
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        [HttpPost]
        public async Task<HttpResponseMessage> Post(string botId)
        {
            if (String.IsNullOrEmpty(botId))
                return new HttpResponseMessage(HttpStatusCode.BadRequest);

            var body = await Request.Content.ReadAsStringAsync();
            //LogError(body);
            //BotLog.Info(body);

            if (body.Contains("user_send_text"))
            {

                var value = JsonConvert.DeserializeObject<ZaloBotRequest>(body);
                //LogError(body);      

                var settingDb = _settingService.GetSettingByBotID(Int32.Parse(botId));
                pageToken = settingDb.ZaloPageToken;
                _isSearchAI = settingDb.IsMDSearch;
                _patternCardPayloadProactive = "postback_card_" + settingDb.CardID.ToString();

                // Khởi động lấy "brain" của bot service theo id
                GetServerAIMLBot(botId);
                // Khởi tạo user theo bot service
                InitUserByServerAIMLBot(value.sender.id);

                // Lấy thuộc tính
                GetAttributeZalo(value.sender.id, botId);

                string message = value.message.text;
                string typeRequest = message.Contains("postback") ? "payload_postback" : "text";

                await ExcuteMessage(message, value.sender.id, Int32.Parse(botId), value.timestamp, typeRequest);

                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            if (body.Contains("follower"))
            {
                var value = JsonConvert.DeserializeObject<ZaloBotRequest>(body);

                var settingDb = _settingService.GetSettingByBotID(Int32.Parse(botId));
                pageToken = settingDb.ZaloPageToken;
                _patternCardPayloadProactive = "postback_card_" + settingDb.CardID.ToString();

                if (settingDb.CardID.HasValue)
                {
                    // Khởi động lấy "brain" của bot service theo id
                    GetServerAIMLBot(botId);
                    // Khởi tạo user theo bot service
                    InitUserByServerAIMLBot(value.sender.id);
                    // Lấy thuộc tính
                    GetAttributeZalo(value.sender.id, botId);

                    // Gửi thẻ lời chào bắt đầu thiết lập trong setting
                    await ExcuteMessage(_patternCardPayloadProactive, value.sender.id, Int32.Parse(botId), value.timestamp, "payload_postback");

                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
            }

            if (body.Contains("user_send_audio"))
            {
                var value = JsonConvert.DeserializeObject<ZaloBotRequest>(body);
                if (value.message.attachments[0].type == "audio")
                {
                    string urlAudio = value.message.attachments[0].payload.url;
                    //BotLog.Info(urlAudio);
                    var rsAudioToTextJson = await SpeechReconitionVNService.ConvertSpeechToText(urlAudio);
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
                            text = Regex.Replace(text, @"\.", "");
                        }
                        await ExcuteMessage(text, value.sender.id, Int32.Parse(botId), value.timestamp, "audio");
                        return new HttpResponseMessage(HttpStatusCode.OK);
                    }
                    else if (status == "1")
                    {
                        string meanTextFromAudio = ZaloTemplate.GetMessageTemplateText("Không có âm thanh", value.sender.id).ToString();
                        await SendMessage(meanTextFromAudio, value.sender.id);
                        return new HttpResponseMessage(HttpStatusCode.OK);
                    }
                    else if (status == "2")
                    {
                        string meanTextFromAudio = ZaloTemplate.GetMessageTemplateText("Xử lý âm thanh bị hủy", value.sender.id).ToString();
                        await SendMessage(meanTextFromAudio, value.sender.id);
                        return new HttpResponseMessage(HttpStatusCode.OK);
                    }
                    else if (status == "9")
                    {
                        string meanTextFromAudio = ZaloTemplate.GetMessageTemplateText("Hệ thống xử lý âm thanh đang bận", value.sender.id).ToString();
                        await SendMessage(meanTextFromAudio, value.sender.id);
                        return new HttpResponseMessage(HttpStatusCode.OK);
                    }
                    return new HttpResponseMessage(HttpStatusCode.OK);
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

        private void GetAttributeZalo(string senderId, string botId)
        {
            var lstAttribute = _attributeService.GetListAttributeZalo(senderId, Int32.Parse(botId)).ToList();
            if (lstAttribute.Count() != 0)
            {
                _dicAttributeUser = new Dictionary<string, string>();
                foreach (var attr in lstAttribute)
                {
                    _dicAttributeUser.Add(attr.AttributeKey, attr.AttributeValue);
                }
            }
        }

        /// <summary>
        /// Xử lý tin nhắn zalo
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

            text = HttpUtility.HtmlDecode(text);
            text = Regex.Replace(text, @"<(.|\n)*?>", "").Trim(); // remove tag html
            text = Regex.Replace(text, @"\p{Cs}", "").Trim();// remove emoji


            // Lấy thông tin người dùng
            _zaloUser = GetUserById(sender, botId);

            //HistoryViewModel hisVm = new HistoryViewModel();

            // Input text
            if (typeRequest == CommonConstants.BOT_REQUEST_TEXT)
            {
                // Thêm dấu tiếng việt
                bool isActive = true;
                string textAccentVN = GetPredictAccentVN(text, isActive);
                if (textAccentVN != text.ToLower())
                {
                    string msg = ZaloTemplate.GetMessageTemplateText("Ý bạn là: " + textAccentVN + "", sender).ToString();
                    await SendMessage(msg, sender);
                }
                text = textAccentVN;
                if (botId == BOT_Y_TE)
                {
                    AttributeZaloUser attZaloUser = new AttributeZaloUser();
                    attZaloUser.AttributeKey = "content_message";
                    attZaloUser.AttributeValue = text;
                    attZaloUser.BotID = botId;
                    attZaloUser.UserID = sender;
                    _dicAttributeUser.Remove("content_message");
                    _dicAttributeUser.Add("content_message", text);
                    _attributeService.CreateUpdateAttributeZalo(attZaloUser);
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
            if (_zaloUser.IsHaveSetAttributeSystem)
            {
                AttributeZaloUser attZaloUser = new AttributeZaloUser();
                attZaloUser.AttributeKey = _zaloUser.AttributeName;
                attZaloUser.BotID = botId;
                attZaloUser.UserID = sender;
                bool isUpdateAttr = false;
                if (typeRequest == CommonConstants.BOT_REQUEST_TEXT)
                {
                    attZaloUser.AttributeValue = text;
                    isUpdateAttr = true;
                }
                if (typeRequest == CommonConstants.BOT_REQUEST_PAYLOAD_POSTBACK)
                {
                    if (!String.IsNullOrEmpty(TITLE_PAYLOAD_QUICKREPLY))
                    {
                        attZaloUser.AttributeValue = TITLE_PAYLOAD_QUICKREPLY;
                        isUpdateAttr = true;
                    }
                }
                if (isUpdateAttr)
                {
                    // Kiểm tra giá trị nhập vào theo từng thuộc tính
                    // Bot Y Tế
                    // Tuổi
                    if (attZaloUser.AttributeKey == "age")
                    {
                        bool isAge = Regex.Match(text, NumberPattern).Success;
                        if (isAge)
                        {
                            attZaloUser.AttributeValue = text;
                        }
                        else
                        {
                            string msg = ZaloTemplate.GetMessageTemplateText("Ký tự phải là số, Anh/chị vui lòng nhập lại độ tuổi", sender).ToString();
                            await SendMessage(msg, sender);
                            return new HttpResponseMessage(HttpStatusCode.OK);
                        }
                    }

                    _dicAttributeUser.Remove(attZaloUser.AttributeKey);
                    _dicAttributeUser.Add(attZaloUser.AttributeKey, attZaloUser.AttributeValue);
                    _attributeService.CreateUpdateAttributeZalo(attZaloUser);
                }


                //hisVm.BotID = botId;
                //hisVm.CreatedDate = DateTime.Now;
                //hisVm.UserSay = text;
                //hisVm.UserName = sender;
                //hisVm.Type = CommonConstants.TYPE_FACEBOOK;
                //hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_004;
                //AddHistory(hisVm);

            }
            if (_zaloUser.PredicateName == "REQUIRE_CLICK_BUTTON_TO_NEXT_CARD")
            {
                if (typeRequest == CommonConstants.BOT_REQUEST_TEXT)
                {
                    string contentRequireClick = ZaloTemplate.GetMessageTemplateText("Anh/chị vui lòng chọn lại thông tin bên dưới", sender).ToString();
                    await SendMessage(contentRequireClick, sender);
                    string partternCardRequireClick = _zaloUser.PredicateValue;
                    string templateCardRequireClick = HandlePostbackCard(partternCardRequireClick, botId);
                    await SendMultiMessageTask(templateCardRequireClick, sender);
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
            }
            else if (_zaloUser.PredicateName == "REQUIRE_INPUT_TEXT_TO_NEXT_CARD")
            {
                if (typeRequest == CommonConstants.BOT_REQUEST_TEXT)
                {
                    string partternCardRequireInput = _zaloUser.PredicateValue;
                    string templateCardRequireInput = HandlePostbackCard(partternCardRequireInput, botId);
                    await SendMultiMessageTask(templateCardRequireInput, sender);
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
            }
            else if (_zaloUser.PredicateName == "VERIFY_TEXT_WITH_AREA_BUTTON")
            {
                if (typeRequest == CommonConstants.BOT_REQUEST_TEXT)
                {
                    var cardDb = _cardService.GetCardByPattern(_zaloUser.PredicateValue);
                    if (cardDb == null)
                    {
                        return new HttpResponseMessage(HttpStatusCode.OK);
                    }
                    string area = cardDb.Name;
                    text = text + " " + area;
                }
            }
            else if (_zaloUser.PredicateName == "POSTBACK_MODULE")
            {
                if (typeRequest == CommonConstants.BOT_REQUEST_TEXT)
                {
                    string postbackModule = _zaloUser.PredicateValue;
                    string templateModule = HandlePostbackModule(postbackModule, text, botId, false);
                    await SendMessage(templateModule, sender);
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
            }

            // print postback card
            if (typeRequest == CommonConstants.BOT_REQUEST_PAYLOAD_POSTBACK)
            {
                string templateCard = HandlePostbackCard(text, botId);
                await SendMultiMessageTask(templateCard, sender);
                if (_zaloUser.PredicateName == "AUTO_NEXT_CARD")
                {
                    string partternNextCard = _zaloUser.PredicateValue;
                    string templateNextCard = HandlePostbackCard(partternNextCard, botId);
                    await SendMultiMessageTask(templateNextCard, sender);
                }
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            AIMLbot.Result rsAIMLBot = GetBotReplyFromAIMLBot(text);
            ResultBot rsBOT = new ResultBot();
            rsBOT = CheckTypePostbackFromResultBotReply(rsAIMLBot);
            if (rsBOT.Type == POSTBACK_MODULE)
            {
                string templateModule = HandlePostbackModule(rsBOT.PatternPayload, text, botId, true);
                await SendMessage(templateModule, sender);
            }
            if (rsBOT.Type == POSTBACK_CARD)
            {
                string templateCard = HandlePostbackCard(rsBOT.PatternPayload, botId);
                await SendMultiMessageTask(templateCard, sender); // print message card
                if (_zaloUser.PredicateName == "AUTO_NEXT_CARD") // print message card kế tiếp nếu có
                {
                    string partternNextCard = _zaloUser.PredicateValue;
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
            if (rsBOT.Type == POSTBACK_NOT_MATCH)
            {
                if (_isSearchAI)
                {
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
                    List<string> lstFaq = new List<string>();
                    lstFaq = GetRelatedQuestion(text, "0", "5", botId.ToString());
                    if (lstFaq.Count() != 0)
                    {
                        foreach (var faq in lstFaq)
                        {
                            await SendMessage(faq, sender);
                        }
                    }
                    if (lstSymptoms.Count() == 0 && lstFaq.Count() == 0)
                    {
                        await SendMessageNotFound(sender);
                    }
                }
                else
                {
                    await SendMessageNotFound(sender);
                }

            }
            if (rsBOT.Type == POSTBACK_TEXT)
            {
                string templateText = ZaloTemplate.GetMessageTemplateText(rsBOT.PatternPayload, sender).ToString();
                await SendMessage(templateText, sender);
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
            _zaloUser.PredicateName = "";
            _zaloUser.PredicateValue = "";
            _zaloUser.IsHaveSetAttributeSystem = false;
            _zaloUser.AttributeName = "";
            var cardDb = _cardService.GetCardByPattern(patternCard);
            string tempCardFb = cardDb.TemplateJsonZalo;

            if (cardDb.TemplateJsonZalo.Contains("module"))
            {
                var rsAIMLBot = GetBotReplyFromAIMLBot(patternCard);
                string patternModule = rsAIMLBot.OutputSentences[0].ToString().Replace(".", "").Trim();
                return HandlePostbackModule(patternModule, patternModule, botId, true);
            }
            if (!String.IsNullOrEmpty(cardDb.AttributeSystemName))
            {
                _zaloUser.IsHaveSetAttributeSystem = true;
                _zaloUser.AttributeName = cardDb.AttributeSystemName;
            }
            if (cardDb.IsHaveCondition)
            {
                _zaloUser.PredicateName = "REQUIRE_CLICK_BUTTON_TO_NEXT_CARD";
                _zaloUser.PredicateValue = patternCard;
            }
            if (cardDb.CardStepID != null && cardDb.IsConditionWithInputText)
            {
                _zaloUser.PredicateName = "REQUIRE_INPUT_TEXT_TO_NEXT_CARD";
                _zaloUser.PredicateValue = CommonConstants.PostBackCard + cardDb.CardStepID;
            }
            if (cardDb.CardStepID != null && cardDb.IsConditionWithInputText == false)
            {
                _zaloUser.PredicateName = "AUTO_NEXT_CARD";
                _zaloUser.PredicateValue = CommonConstants.PostBackCard + cardDb.CardStepID;
            }
            if (cardDb.IsConditionWithAreaButton)
            {
                _zaloUser.PredicateName = "VERIFY_TEXT_WITH_AREA_BUTTON";
            }
            UpdateStatusZaloUser(_zaloUser);
            return tempCardFb;
        }
        private string HandlePostbackModule(string postbackModule, string text, int botId, bool isFristRequest)
        {
            string templateHandle = "";
            _zaloUser.IsHaveSetAttributeSystem = false;
            _zaloUser.AttributeName = "";
            _zaloUser.PredicateName = "POSTBACK_MODULE";
            if (postbackModule.Contains(CommonConstants.ModuleSearchAPI))
            {
                string mdSearchId = postbackModule.Replace("postback_module_api_search_", "");
                if (isFristRequest)
                {
                    var handleMdSearch = _handleMdService.HandleIsSearchAPI(postbackModule, mdSearchId, "");
                    templateHandle = handleMdSearch.TemplateJsonZalo;
                }
                else
                {
                    var handleMdSearch = _handleMdService.HandleIsSearchAPI(text, mdSearchId, "");
                    templateHandle = handleMdSearch.TemplateJsonZalo;
                }
                _zaloUser.PredicateValue = postbackModule;
            }
            if (postbackModule.Contains(CommonConstants.ModuleAdminContact))
            {
                var handleAdminContact = _handleMdService.HandleIsAdminContact(text, botId);
                templateHandle = handleAdminContact.TemplateJsonZalo;
                _zaloUser.PredicateValue = postbackModule;
            }

            UpdateStatusZaloUser(_zaloUser);
            return templateHandle;
        }
        #endregion

        private async Task SendMessageNotFound(string sender)
        {
            List<string> keyList = new List<string>(_DICTIONARY_NOT_MATCH.Keys);
            Random rand = new Random();
            string randomKey = keyList[rand.Next(keyList.Count)];
            string contentNotFound = _DICTIONARY_NOT_MATCH[randomKey];
            string templateNotFound = ZaloTemplate.GetMessageTemplateTextAndQuickReply(
                contentNotFound, sender, _contactAdmin, _titlePayloadContactAdmin).ToString();
            await SendMessage(templateNotFound, sender);
        }
        
        /// <summary>
        /// send message
        /// </summary>
        /// <param name="templateJson">templateJson</param>
        private async Task<HttpResponseMessage> SendMessage(string templateJson, string sender)
        {
            HttpResponseMessage res;
            if (!String.IsNullOrEmpty(templateJson))
            {
                // Lấy token file từ zalo
                if(templateJson.Contains("\"type\": \"file\""))
                {
                    var objFile = new JavaScriptSerializer
                    {
                        MaxJsonLength = Int32.MaxValue,
                        RecursionLimit = 100
                    }.Deserialize<dynamic>(templateJson);

                    string fileToken = GetFileToken(objFile.message.attachment.payload);

                    templateJson = ZaloTemplate.GetMessageTemplateFile(fileToken, sender).ToString();
                }

                templateJson = templateJson.Replace("{{senderId}}", sender);
                templateJson = Regex.Replace(templateJson, "File/", Domain + "File/");
                templateJson = Regex.Replace(templateJson, "<br />", "\\n");
                templateJson = Regex.Replace(templateJson, "<br/>", "\\n");
                templateJson = Regex.Replace(templateJson, @"\\n\\n", "\\n");
                templateJson = Regex.Replace(templateJson, @"\\n\\r\\n", "\\n");
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
                
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                res = await _client.PostAsync($"https://openapi.zalo.me/v2.0/oa/message?access_token=" + pageToken + "", new StringContent(templateJson, Encoding.UTF8, "application/json"));

            }
            return new HttpResponseMessage(HttpStatusCode.OK);
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

        #region Create update and get Zalo user

        private ApplicationZaloUser UpdateStatusZaloUser(ApplicationZaloUser zaloUserVm)
        {
            _appZaloUser.Update(zaloUserVm);
            _appZaloUser.Save();
            return zaloUserVm;
        }
        private ApplicationZaloUser GetUserById(string senderId, int botId)
        {
            ApplicationZaloUser zaloUserDb = new ApplicationZaloUser();
            zaloUserDb = _appZaloUser.GetByUserId(senderId);
            if (zaloUserDb == null)
            {
                
                zaloUserDb = new ApplicationZaloUser();
                zaloUserDb.UserId = senderId;
                zaloUserDb.IsHavePredicate = false;
                zaloUserDb.IsProactiveMessage = false;
                zaloUserDb.TimeOut = DateTime.Now.AddSeconds(TIMEOUT_DELAY_SEND_MESSAGE);
                zaloUserDb.CreatedDate = DateTime.Now;
                zaloUserDb.StartedOn = DateTime.Now;

                zaloUserDb.FirstName = "N/A";
                zaloUserDb.Age = 0;// "N/A";
                zaloUserDb.LastName = "N/A";

                ProfileUser profileUser = new ProfileUser();
                profileUser = GetProfileUser(senderId);
                if (profileUser.data != null)
                {
                    zaloUserDb.UserName = profileUser.data.display_name;
                    zaloUserDb.Gender = (profileUser.data.user_gender == 1 ? true : false);
                }
                else
                {
                    zaloUserDb.UserName = "N/A";
                    zaloUserDb.Gender = true;
                }            

                _appZaloUser.Add(zaloUserDb);
                _appZaloUser.Save();

                AddAttributeDefault(senderId, botId, "sender_name", zaloUserDb.UserName);
            }
            return zaloUserDb;
        }
        private ProfileUser GetProfileUser(string senderId)
        {
            ProfileUser user = new ProfileUser();
            string userId = JObject.FromObject(
                             new
                             {
                                 user_id = senderId
                             }).ToString();
            HttpResponseMessage res = new HttpResponseMessage();
            res = _client.GetAsync($"https://openapi.zalo.me/v2.0/oa/getprofile?access_token=" + pageToken + "&data=" + userId).Result;//gender y/c khi sử dụng
            if (res.IsSuccessStatusCode)
            {
                var serializer = new JavaScriptSerializer();
                serializer.MaxJsonLength = Int32.MaxValue;
                user = serializer.Deserialize<ProfileUser>(res.Content.ReadAsStringAsync().Result);
            }
            return user;
        }

        private string GetFileToken(string fileUrl)
        {
            string fileToken = "";
            var multiForm = new MultipartFormDataContent();

            // add file and directly upload it
            FileStream fs = File.OpenRead(fileUrl);
            multiForm.Add(new StreamContent(fs), "file", Domain + Path.GetFileName(fileUrl));

            // send request to API
            var url = "https://openapi.zalo.me/v2.0/oa/upload/file?access_token=" + pageToken;
            var response = _client.PostAsync(url, multiForm).Result;
            if (response.IsSuccessStatusCode)
            {
                var result = new JavaScriptSerializer
                {
                    MaxJsonLength = Int32.MaxValue,
                    RecursionLimit = 100
                }
                .Deserialize<dynamic>(response.Content.ReadAsStringAsync().Result);
                fileToken = result.data.token;
            }
            return fileToken;
        }

        private void AddAttributeDefault(string userId, int BotId, string key, string value)
        {
            AttributeZaloUser attZaloUser = new AttributeZaloUser();
            attZaloUser.UserID = userId;
            attZaloUser.BotID = BotId;
            attZaloUser.AttributeKey = key;
            attZaloUser.AttributeValue = value;
            _attributeService.CreateUpdateAttributeZalo(attZaloUser);
            _attributeService.Save();
        }

        #endregion

        public class ProfileUser
        {
            public ProfileInfo data { set; get; }
            public int error { set; get; }
            public string message { set; get; }
        }
        public class ProfileInfo
        {
            public string display_name { set; get; }
            public int user_gender { set; get; }
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
            catch
            {
            }
        }
        private void AddHistory(HistoryViewModel hisVm)
        {
            History hisDb = new History();
            hisDb.UpdateHistory(hisVm);
            _historyService.Create(hisDb);
            _historyService.Save();
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
                string strTemplateTextFb = ZaloTemplate.GetMessageTemplateText(totalFind, "{{senderId}}").ToString();
                string strTemplateGenericRelatedQuestion = ZaloTemplate.GetMessageTemplateGenericByList("{{senderId}}", lstQnaAPI).ToString();
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
                    string strTemplateTextFb = ZaloTemplate.GetMessageTemplateText(msgSymptoms, "{{senderId}}").ToString();
                    string strTemplateGenericMedicalSymptoms = ZaloTemplate.GetMessageTemplateGenericByListMed("{{senderId}}", dataSymptomp).ToString();
                    _lstSymptoms.Add(strTemplateTextFb);
                    _lstSymptoms.Add(strTemplateGenericMedicalSymptoms);
                }
            }
            return _lstSymptoms;
        }
        #endregion

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

        public class ResultBot
        {
            public string Type { set; get; }
            public int Total { set; get; }
            public string PatternPayload { set; get; }
        }
    }
}
