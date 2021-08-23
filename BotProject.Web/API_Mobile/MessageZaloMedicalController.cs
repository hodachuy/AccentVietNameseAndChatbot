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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
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

namespace BotProject.Web.API_Mobile
{
    public class MessageZaloMedicalController : ApiController
    {
        string pageToken = "OUQpPbeaQqbjuhvZLIjhJstikczICc8EDPBCEaCHQKStjV4gJ1fqPJEwnaeBPsSmOjhq7rL485CgnvKRSa97SYpW_bXRIMe04el3A2e9Rb9ca-C63ZKn9dcAbbeV0oDqMgos31yhEqLhhvfhD1HC45sWnKKR51GAUwEWHaS_0Xbvzhf94qu34LxQXKOVQI8gGwdxGpKILZjqrS1cAK5hAHA7oNvA3qzIVPpUA1quUtTTaDms2YflNsxgvNCMIZmDV8EVTZCa7ITJPWxmu1bSE657";
        //string appSecret = Helper.ReadString("AppSecret");
        //string verifytoken = Helper.ReadString("VerifyTokenWebHook");

        string _contactAdmin = Helper.ReadString("AdminContact");
        string _titlePayloadContactAdmin = Helper.ReadString("TitlePayloadAdminContact");

        int _timeOut = 60;
        bool _isHaveTimeOut = false;
        string _messageProactive = "";
        bool _isSearchAI = false;

        //tin nhắn vắng mặt
        string _messageAbsent = "";
        bool _isHaveMessageAbsent = false;

        string _patternCardPayloadProactive = "";
        string _titleCardPayloadProactive = "🔙 Quay về";

        //OTP
        int _timeOutOTP = 60;
        bool _isHaveTimeOutOTP = false;
        string _messageOTP = "";

        string _pathStopWord = System.IO.Path.Combine(PathServer.PathNLR, "StopWord.txt");
        string _stopWord = "";

        private readonly string Domain = Helper.ReadString("Domain");
        private readonly string UrlAPI = Helper.ReadString("UrlAPI");
        private readonly string KeyAPI = Helper.ReadString("KeyAPI");
        private string pathAIML = PathServer.PathAIML;
        private string pathSetting = PathServer.PathAIML + "config";

        private Dictionary<string, string> _dicNotMatch;
        private Dictionary<string, string> _dicAttributeUser;

        private IApplicationZaloUserService _appZaloUser;
        private BotServiceMedical _botService;
        private ISettingService _settingService;
        private IHandleModuleServiceService _handleMdService;
        private IModuleService _mdService;
        private IModuleKnowledegeService _mdKnowledgeService;
        private IMdSearchService _mdSearchService;
        private IErrorService _errorService;
        private IAIMLFileService _aimlFileService;
        private IQnAService _qnaService;
        private ApiQnaNLRService _apiNLR;
        private IModuleSearchEngineService _moduleSearchEngineService;
        private IHistoryService _historyService;
        private ICardService _cardService;
        private IApplicationThirdPartyService _app3rd;


        private const string NumberPattern = @"^\d+$";
        private int age = 10;

        private IAttributeSystemService _attributeService;

        //private Bot _bot;
        private User _user;

        private AccentService _accentService;
        public MessageZaloMedicalController(IErrorService errorService,
                              IBotService botDbService,
                              ISettingService settingService,
                              IHandleModuleServiceService handleMdService,
                              IModuleService mdService,
                              IMdSearchService mdSearchService,
                              IModuleKnowledegeService mdKnowledgeService,
                              IAIMLFileService aimlFileService,
                              IQnAService qnaService,
                              IModuleSearchEngineService moduleSearchEngineService,
                              IHistoryService historyService,
                              ICardService cardService,
                              IApplicationZaloUserService appZaloUser,
                              IApplicationThirdPartyService app3rd,
                              IAttributeSystemService attributeService)
        {
            _errorService = errorService;
            //_accentService = AccentService.AccentInstance;
            _settingService = settingService;
            _handleMdService = handleMdService;
            _mdService = mdService;
            _apiNLR = new ApiQnaNLRService();
            _mdKnowledgeService = mdKnowledgeService;
            _mdSearchService = mdSearchService;
            _aimlFileService = aimlFileService;
            _qnaService = qnaService;
            _botService = BotServiceMedical.BotInstance;
            _moduleSearchEngineService = moduleSearchEngineService;
            _historyService = historyService;
            _cardService = cardService;
            _appZaloUser = appZaloUser;
            _app3rd = app3rd;
            _attributeService = attributeService;
            //_accentService = new AccentService();// AccentService.AccentInstance;
        }

        public HttpResponseMessage Get()
        {
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        [HttpPost]
        public async Task<HttpResponseMessage> Post()
        {
            int botId = 3019;
            var body = await Request.Content.ReadAsStringAsync();

            if (body.Contains("user_send_text"))
            {
                var value = JsonConvert.DeserializeObject<ZaloBotRequest>(body);
                //LogError(body);
                BotLog.Info(body);
                var app3rd = _app3rd.GetByZaloPageId(value.app_id);
                if (app3rd == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
                botId = app3rd.BotID;
                var settingDb = _settingService.GetSettingByBotID(botId);
                pageToken = settingDb.ZaloPageToken;

                _isHaveTimeOut = settingDb.IsProactiveMessageZalo;
                _timeOut = settingDb.Timeout;
                _messageProactive = settingDb.ProactiveMessageText;
                _isSearchAI = settingDb.IsMDSearch;
                _patternCardPayloadProactive = "postback_card_" + settingDb.CardID.ToString();

                //tin vắng mặt
                _messageAbsent = settingDb.MessageMaintenance;
                _isHaveMessageAbsent = settingDb.IsHaveMaintenance;

                //OTP
                _timeOutOTP = settingDb.TimeoutOTP;
                _isHaveTimeOutOTP = settingDb.IsHaveTimeoutOTP;
                _messageOTP = settingDb.MessageTimeoutOTP;

                var lstAttribute = _attributeService.GetListAttributeZalo(value.sender.id, botId).ToList();
                if (lstAttribute.Count() != 0)
                {
                    _dicAttributeUser = new Dictionary<string, string>();
                    foreach (var attr in lstAttribute)
                    {
                        _dicAttributeUser.Add(attr.AttributeKey, attr.AttributeValue);
                    }
                }
                await ExcuteMessage(value.message.text, value.sender.id, botId);

                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            if (body.Contains("user_send_audio"))
            {
                //BotLog.Info(body);
                var value = JsonConvert.DeserializeObject<ZaloBotRequest>(body);
                //LogError(body);
                var app3rd = _app3rd.GetByZaloPageId(value.app_id);
                if (app3rd == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
                botId = app3rd.BotID;
                var settingDb = _settingService.GetSettingByBotID(botId);
                pageToken = settingDb.ZaloPageToken;

                _isHaveTimeOut = settingDb.IsProactiveMessageZalo;
                _timeOut = settingDb.Timeout;
                _messageProactive = settingDb.ProactiveMessageText;
                _isSearchAI = settingDb.IsMDSearch;
                _patternCardPayloadProactive = "postback_card_" + settingDb.CardID.ToString();

                //tin vắng mặt
                _messageAbsent = settingDb.MessageMaintenance;
                _isHaveMessageAbsent = settingDb.IsHaveMaintenance;

                //OTP
                _timeOutOTP = settingDb.TimeoutOTP;
                _isHaveTimeOutOTP = settingDb.IsHaveTimeoutOTP;
                _messageOTP = settingDb.MessageTimeoutOTP;

                var lstAttribute = _attributeService.GetListAttributeZalo(value.sender.id, botId).ToList();
                if (lstAttribute.Count() != 0)
                {
                    _dicAttributeUser = new Dictionary<string, string>();
                    foreach (var attr in lstAttribute)
                    {
                        _dicAttributeUser.Add(attr.AttributeKey, attr.AttributeValue);
                    }
                }

                if (value.message.attachments[0].type == "audio")
                {
                    string urlAudio = value.message.attachments[0].payload.url;
                    //BotLog.Info(urlAudio);
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
                            text = Regex.Replace(text, @"\.", "");
                        }
                        await ExcuteMessage(text, value.sender.id, botId, "audio");
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

                return new HttpResponseMessage(HttpStatusCode.OK);
            }


            // sự kiện người dùng quan tâm
            if (body.Contains("follower"))
            {
                var value = JsonConvert.DeserializeObject<ZaloBotRequest>(body);
                var app3rd = _app3rd.GetByZaloPageId(value.app_id);
                if (app3rd == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
                botId = app3rd.BotID;
                var settingDb = _settingService.GetSettingByBotID(botId);
                var settingVm = Mapper.Map<BotProject.Model.Models.Setting, BotSettingViewModel>(settingDb);

                pageToken = settingDb.ZaloPageToken;
                _isHaveTimeOut = settingDb.IsProactiveMessageZalo;
                _timeOut = settingDb.Timeout;
                _messageProactive = settingDb.ProactiveMessageText;
                _isSearchAI = settingDb.IsMDSearch;
                _patternCardPayloadProactive = "postback_card_" + settingDb.CardID.ToString();

                //tin vắng mặt
                _messageAbsent = settingDb.MessageMaintenance;
                _isHaveMessageAbsent = settingDb.IsHaveMaintenance;

                //OTP
                _timeOutOTP = settingDb.TimeoutOTP;
                _isHaveTimeOutOTP = settingDb.IsHaveTimeoutOTP;
                _messageOTP = settingDb.MessageTimeoutOTP;

                var lstAttribute = _attributeService.GetListAttributeZalo(value.sender.id, botId).ToList();
                if (lstAttribute.Count() != 0)
                {
                    _dicAttributeUser = new Dictionary<string, string>();
                    foreach (var attr in lstAttribute)
                    {
                        _dicAttributeUser.Add(attr.AttributeKey, attr.AttributeValue);
                    }
                }

                if (settingVm.CardID.HasValue)
                {
                    string getStartCardPayload = "postback_card_" + settingVm.CardID;
                    await ExcuteMessage(getStartCardPayload, value.follower.id, botId);

                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }


        private async Task<HttpResponseMessage> ExcuteMessage(string text, string sender, int botId, string typeFile = "")
        {
            if (String.IsNullOrEmpty(text))
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            text = HttpUtility.HtmlDecode(text);

            text = Regex.Replace(text, @"<(.|\n)*?>", "").Trim();
            text = Regex.Replace(text, @"\p{Cs}", "").Trim();// remove emoji
            text = Regex.Replace(text, @"#", "").Trim();

            ApplicationZaloUser zlUserDb = new ApplicationZaloUser();
            zlUserDb = _appZaloUser.GetByUserId(sender);

            int age = 10;

            //Typing ...
            if (zlUserDb != null && (zlUserDb.PredicateName != "Admin_Contact"))
            {
                if (zlUserDb.Age != 0)
                {
                    bool isAge = Regex.Match(text, NumberPattern).Success;
                    if (isAge)
                    {
                        age = zlUserDb.Age;
                    }
                }
            }


            if (!text.Contains("postback") && !text.Contains(_contactAdmin))
            {
                //_accentService = new AccentService();// AccentService.AccentInstance;
                _accentService = AccentService.SingleInstance;
                text = _accentService.GetAccentVN(text);
            }

            string attributeValueFromPostback = "";
            // Xét payload postback nếu postback từ quickreply sẽ chứa thêm sperator - và tiêu đề nút
            if (text.Contains("postback"))
            {
                var arrPostback = Regex.Split(text, "-");
                if (arrPostback.Length > 1)
                {
                    attributeValueFromPostback = arrPostback[1];
                }
                text = arrPostback[0];
            }

            HistoryViewModel hisVm = new HistoryViewModel();
            hisVm.BotID = botId;
            hisVm.CreatedDate = DateTime.Now;
            hisVm.UserSay = text;
            hisVm.UserName = sender;
            hisVm.Type = CommonConstants.TYPE_ZALO;

            DateTime dStartedTime = DateTime.Now;
            DateTime dTimeOut = DateTime.Now.AddSeconds(_timeOut);

            if (text == "postback_card_4031")// thẻ chào hỏi
            {
                if (zlUserDb.Age == 0)//nếu thông tin tuổi chưa có trả về thẻ hỏi thông tin
                {
                    text = "postback_card_8917";//trả về thẻ hỏi thông tin
                }
            }

            try
            {
                if (zlUserDb != null)
                {
                    if (zlUserDb.PredicateName == "Admin_Contact")
                    {
                        var handleAdminContact = _handleMdService.HandleIsAdminContact(text, botId);
                        hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_004;
                        //AddHistory(hisVm);
                        if (text.Contains("postback") || text.Contains(_contactAdmin))
                        {
                            zlUserDb.IsHavePredicate = false;
                            zlUserDb.PredicateName = "";
                            zlUserDb.PredicateValue = "";
                            zlUserDb.IsHaveCardCondition = false;
                            zlUserDb.CardConditionPattern = "";
                            zlUserDb.IsConditionWithAreaButton = false;
                            zlUserDb.CardConditionAreaButtonPattern = "";
                            zlUserDb.CardStepPattern = "";
                            zlUserDb.AttributeName = "";
                            zlUserDb.IsHaveSetAttributeSystem = false;
                            zlUserDb.IsConditionWithInputText = false;
                            zlUserDb.CardConditionWithInputTextPattern = "";
                            _appZaloUser.Update(zlUserDb);
                            _appZaloUser.Save();
                            return await ExcuteMessage(text, sender, botId);
                        }

                        if (_isHaveMessageAbsent)
                        {
                            if (HelperMethods.IsTimeInWorks() == false)
                            {
                                //await SendMessageTask(FacebookTemplate.GetMessageTemplateTextAndQuickReply(_messageAbsent, "{{senderId}}", _patternCardPayloadProactive, _titleCardPayloadProactive).ToString(),sender);
                                return new HttpResponseMessage(HttpStatusCode.OK);
                            }
                        }

                        if (handleAdminContact.Status == false)
                        {
                            string[] strArrayJson = Regex.Split(handleAdminContact.TemplateJsonZalo, "split");
                            if (strArrayJson.Length != 0)
                            {
                                var strArray = strArrayJson.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                                foreach (var temp in strArray)
                                {
                                    string tempJson = temp;
                                    await SendMessageTask(tempJson, sender);
                                }
                                return new HttpResponseMessage(HttpStatusCode.OK);
                            }
                        }
                        return new HttpResponseMessage(HttpStatusCode.OK);
                    }
                }

                //xử lý audio
                if (!String.IsNullOrEmpty(typeFile))
                {
                    string meanTextFromAudio = ZaloTemplate.GetMessageTemplateText("Ý của bạn là: " + text, sender).ToString();
                    await SendMessageTask(meanTextFromAudio, sender);
                }

                if (zlUserDb == null)
                {
                    ProfileUser profileUser = new ProfileUser();
                    profileUser = GetProfileUser(sender);

                    zlUserDb = new ApplicationZaloUser();
                    ApplicationZaloUserViewModel zlUserVm = new ApplicationZaloUserViewModel();
                    zlUserVm.UserId = sender;
                    zlUserVm.IsHavePredicate = false;
                    zlUserVm.IsProactiveMessage = false;
                    zlUserVm.TimeOut = dTimeOut;
                    zlUserDb.CreatedDate = DateTime.Now;
                    zlUserDb.StartedOn = dStartedTime;

                    zlUserDb.FirstName = "N/A";
                    zlUserDb.Age = 0;// "N/A";
                    zlUserDb.LastName = "N/A";
                    zlUserDb.UserName = profileUser.data.display_name;
                    zlUserDb.Gender = (profileUser.data.user_gender == 1 ? true : false);

                    zlUserDb.UpdateZaloUser(zlUserVm);
                    _appZaloUser.Add(zlUserDb);
                    _appZaloUser.Save();

                    // add attribute defailt user facebook
                    AddAttributeDefault(sender, botId, "sender_id", sender);
                    AddAttributeDefault(sender, botId, "sender_name", zlUserDb.UserName);
                    AddAttributeDefault(sender, botId, "sender_first_name", "N/A");
                    AddAttributeDefault(sender, botId, "sender_last_name", "N/A");
                    AddAttributeDefault(sender, botId, "gender", (profileUser.data.user_gender == 1 ? "Nam" : "Nữ"));
                }
                else
                {
                    zlUserDb.StartedOn = DateTime.Now;
                    zlUserDb.TimeOut = dTimeOut;

                    // Nếu có yêu cầu click thẻ để đi theo luồng
                    if (zlUserDb.IsHaveCardCondition)
                    {
                        if (!text.Contains("postback") && !text.Contains(_contactAdmin))
                        {
                            var cardDb = _cardService.GetCardByPattern(zlUserDb.CardConditionPattern);
                            if (cardDb == null)
                            {
                                return new HttpResponseMessage(HttpStatusCode.OK);
                            }
                            string tempJsonZalo = cardDb.TemplateJsonZalo;
                            if (!String.IsNullOrEmpty(tempJsonZalo))
                            {
                                tempJsonZalo = tempJsonZalo.Trim();
                                string[] strArrayJson = Regex.Split(tempJsonZalo, "split");
                                if (strArrayJson.Length != 0)
                                {
                                    await SendMessageTask(ZaloTemplate.GetMessageTemplateText("Anh/chị vui lòng chọn lại thông tin bên dưới", sender).ToString(), sender);
                                    var strArray = strArrayJson.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                                    foreach (var temp in strArray)
                                    {
                                        string tempJson = temp;
                                        await SendMessageTask(tempJson, sender);
                                    }
                                    return new HttpResponseMessage(HttpStatusCode.OK);
                                }
                            }
                        }
                    }

                    // AttributeFacebook nếu thẻ trước có biến cần lưu
                    if (zlUserDb.IsHaveSetAttributeSystem)
                    {
                        AttributeZaloUser attZlUser = new AttributeZaloUser();
                        attZlUser.AttributeKey = zlUserDb.AttributeName;
                        //attZlUser.AttributeValue = text;
                        attZlUser.BotID = botId;
                        attZlUser.UserID = sender;
                        if (!text.Contains("postback") && !text.Contains(_contactAdmin))
                        {
                            attZlUser.AttributeValue = text;
                        }
                        else if (text.Contains("postback"))
                        {
                            attZlUser.AttributeValue = attributeValueFromPostback.Trim();
                        }
                        if (attZlUser.AttributeKey == "age")
                        {
                            bool isAge = Regex.Match(text, NumberPattern).Success;
                            if (isAge)
                            {
                                attZlUser.AttributeValue = text;
                            }
                            else
                            {
                                await SendMessageTask(ZaloTemplate.GetMessageTemplateText("Ký tự phải là số, Anh/chị vui lòng nhập lại độ tuổi", sender).ToString(), sender);
                                return new HttpResponseMessage(HttpStatusCode.OK);
                            }
                        }

                        _dicAttributeUser.Remove(attZlUser.AttributeKey);
                        _dicAttributeUser.Add(attZlUser.AttributeKey, attZlUser.AttributeValue);

                        var att = _attributeService.CreateUpdateAttributeZalo(attZlUser);

                        if (attZlUser.AttributeKey == "age")
                        {
                            zlUserDb.Age = Int32.Parse(text);
                            zlUserDb.IsHaveSetAttributeSystem = false;
                            zlUserDb.AttributeName = "";
                            _appZaloUser.Update(zlUserDb);
                            _appZaloUser.Save();

                            _dicAttributeUser.Remove("age");
                            _dicAttributeUser.Add("age", text);
                        }
                    }

                    // Nhập text để đi luồng tiếp theo nhưng CardStepID không được rỗng
                    if (zlUserDb.IsConditionWithInputText)
                    {
                        if (!text.Contains("postback") && !text.Contains(_contactAdmin))
                        {
                            zlUserDb.IsHaveSetAttributeSystem = false;
                            zlUserDb.AttributeName = "";
                            zlUserDb.IsConditionWithInputText = false;
                            zlUserDb.IsHaveCardCondition = false;
                            _appZaloUser.Update(zlUserDb);
                            _appZaloUser.Save();
                            return await ExcuteMessage(zlUserDb.CardConditionWithInputTextPattern, sender, botId); //postback_card_8927 thẻ thông tin người dùng
                        }
                    }


                    // Nếu có yêu cầu query text theo lĩnh vực button
                    // Click button -> card (tên card nên đặt như tên lĩnh vực ngắn gọn)
                    // Build lại kịch bản với từ khoán ngắn gọn + tên lĩnh vực
                    // ví dụ: thủ tục cấp phép, thủ tục giạn + tên lĩnh vực
                    if (zlUserDb.IsConditionWithAreaButton)
                    {
                        if (!text.Contains("postback") && !text.Contains(_contactAdmin))
                        {
                            var cardDb = _cardService.GetCardByPattern(zlUserDb.CardConditionAreaButtonPattern);
                            if (cardDb == null)
                            {
                                return new HttpResponseMessage(HttpStatusCode.OK);
                            }
                            string area = cardDb.Name;
                            text = text + " " + area;// + thêm tên lĩnh vực button và nội dung trong form QnA có chứa từ lĩnh vực
                        }
                    }

                    // Điều kiện xử lý module
                    if (zlUserDb.IsHavePredicate)
                    {
                        var predicateName = zlUserDb.PredicateName;
                        if (predicateName == "ApiSearch")
                        {
                            if (text.Contains("postback_card") || text.Contains(_contactAdmin))// nều còn điều kiện search mà chọn postback
                            {
                                zlUserDb.IsHavePredicate = false;
                                zlUserDb.PredicateName = "";
                                zlUserDb.PredicateValue = "";
                                zlUserDb.IsHaveCardCondition = false;
                                zlUserDb.CardConditionPattern = "";
                                zlUserDb.IsConditionWithAreaButton = false;
                                zlUserDb.CardConditionAreaButtonPattern = "";
                                zlUserDb.CardStepPattern = "";
                                zlUserDb.AttributeName = "";
                                zlUserDb.IsHaveSetAttributeSystem = false;
                                zlUserDb.IsConditionWithInputText = false;
                                zlUserDb.CardConditionWithInputTextPattern = "";
                                _appZaloUser.Update(zlUserDb);
                                _appZaloUser.Save();
                                return await ExcuteMessage(text, sender, botId);
                            }

                            string predicateValue = zlUserDb.PredicateValue;
                            var handleMdSearch = _handleMdService.HandleIsSearchAPI(text, predicateValue, "");

                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_005;
                            //AddHistory(hisVm);

                            return await SendMessage(handleMdSearch.TemplateJsonZalo, sender);

                        }
                    }
                    else // Input: Khởi tạo module được chọn
                    {
                        if (text.Contains(CommonConstants.ModuleAdminContact))
                        {
                            var handleAdminContact = _handleMdService.HandleIsAdminContact(text, botId);

                            zlUserDb.IsHavePredicate = true;
                            zlUserDb.PredicateName = "Admin_Contact";
                            zlUserDb.PredicateValue = "";
                            zlUserDb.IsHaveCardCondition = false;
                            zlUserDb.CardConditionPattern = "";
                            zlUserDb.IsConditionWithAreaButton = false;
                            zlUserDb.CardConditionAreaButtonPattern = "";
                            _appZaloUser.Update(zlUserDb);
                            _appZaloUser.Save();

                            hisVm.UserSay = "[Chat với chuyên viên]";
                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_003;
                            //AddHistory(hisVm);

                            // Tin nhắn vắng mặt
                            if (_isHaveMessageAbsent)
                            {
                                if (HelperMethods.IsTimeInWorks() == false)
                                {
                                    await SendMessageTask(ZaloTemplate.GetMessageTemplateTextAndQuickReply(_messageAbsent, "{{senderId}}", _patternCardPayloadProactive, _titleCardPayloadProactive).ToString(), sender);
                                    return new HttpResponseMessage(HttpStatusCode.OK);
                                }
                            }

                            string[] strArrayJson = Regex.Split(handleAdminContact.TemplateJsonZalo, "split");//nhớ thêm bên formcard xử lý lục trên face
                            if (strArrayJson.Length != 0)
                            {
                                var strArray = strArrayJson.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                                foreach (var temp in strArray)
                                {
                                    string tempJson = temp;
                                    await SendMessageTask(tempJson, sender);
                                }

                                return new HttpResponseMessage(HttpStatusCode.OK);
                            }

                            return new HttpResponseMessage(HttpStatusCode.OK);
                        }

                        if (text.Contains(CommonConstants.ModuleSearchAPI))
                        {
                            string mdSearchId = text.Replace(".", String.Empty).Replace("postback_module_api_search_", "");
                            var handleMdSearch = _handleMdService.HandleIsSearchAPI(text, mdSearchId, "");
                            zlUserDb.IsHavePredicate = true;
                            zlUserDb.PredicateName = "ApiSearch";
                            zlUserDb.PredicateValue = mdSearchId;
                            zlUserDb.IsHaveCardCondition = false;
                            zlUserDb.CardConditionPattern = "";
                            zlUserDb.IsConditionWithAreaButton = false;
                            zlUserDb.CardConditionAreaButtonPattern = "";
                            zlUserDb.CardStepPattern = "";
                            zlUserDb.IsHaveSetAttributeSystem = false;
                            zlUserDb.AttributeName = "";
                            zlUserDb.IsConditionWithInputText = false;
                            zlUserDb.CardConditionWithInputTextPattern = "";
                            _appZaloUser.Update(zlUserDb);
                            _appZaloUser.Save();

                            hisVm.UserSay = "[Tra cứu]";
                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_003;
                            //AddHistory(hisVm);

                            return await SendMessage(handleMdSearch.TemplateJsonZalo, sender);

                        }
                    }
                }

                if (!text.Contains("postback") && !text.Contains(_contactAdmin))
                {
                    if (age <= 7)
                    {
                        text = "trẻ em " + text;
                    }
                }

                AIMLbot.Result aimlBotResult = _botService.Chat(text);
                string result = aimlBotResult.OutputSentences[0].ToString();

                // Nếu trả về là module
                if (result.Replace("\r\n", "").Trim().Contains(CommonConstants.POSTBACK_MODULE))
                {
                    if (result.Contains("<module>") != true)// k phải button module trả về
                    {
                        string txtModule = result.Replace("\r\n", "").Replace(".", "").Trim();
                        txtModule = Regex.Replace(txtModule, @"<(.|\n)*?>", "").Trim();
                        int idxModule = txtModule.IndexOf("postback_module");
                        if (idxModule != -1)
                        {
                            string strPostback = txtModule.Substring(idxModule, txtModule.Length - idxModule);
                            var punctuation = strPostback.Where(Char.IsPunctuation).Distinct().ToArray();
                            var words = strPostback.Split().Select(x => x.Trim(punctuation));
                            var contains = words.SingleOrDefault(x => x.Contains("postback_module") == true);

                            if (words.ToList().Count == 1 && (txtModule.Length == contains.Length))
                            {
                                return await ExcuteMessage(contains, sender, botId);
                            }

                            string rsHandle = "";

                            if (contains == "postback_module_api_search")
                            {
                                return await ExcuteMessage(txtModule, sender, botId);
                            }
                            if (contains == "postback_module_med_get_info_patient")
                            {
                                return await ExcuteMessage(txtModule, sender, botId);
                            }
                            if (contains == "postback_module_age")
                            {
                                zlUserDb.PredicateName = "Age";
                                var handleAge = _handleMdService.HandledIsAge(contains, botId);
                                rsHandle = handleAge.TemplateJsonZalo;
                            }
                            if (contains == "postback_module_email")
                            {
                                zlUserDb.PredicateName = "Email";
                                var handleEmail = _handleMdService.HandledIsEmail(contains, botId);
                                rsHandle = handleEmail.TemplateJsonZalo;
                            }
                            if (contains == "postback_module_phone")
                            {
                                zlUserDb.PredicateName = "Phone";
                                var handlePhone = _handleMdService.HandleIsPhoneNumber(contains, botId);
                                rsHandle = handlePhone.TemplateJsonZalo;
                            }
                            zlUserDb.IsHavePredicate = true;
                            zlUserDb.PredicateValue = "";
                            zlUserDb.IsHaveCardCondition = false;
                            zlUserDb.CardConditionPattern = "";
                            zlUserDb.CardStepPattern = "";
                            zlUserDb.IsHaveSetAttributeSystem = false;
                            zlUserDb.AttributeName = "";
                            zlUserDb.IsConditionWithInputText = false;
                            zlUserDb.CardConditionWithInputTextPattern = "";
                            _appZaloUser.Update(zlUserDb);
                            _appZaloUser.Save();

                            return await SendMessage(rsHandle, sender);
                        }
                    }
                }
                if (result.Contains("NOT_MATCH"))
                {
                    hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_002;
                    //AddHistory(hisVm);

                    zlUserDb.IsHaveCardCondition = false;
                    zlUserDb.CardConditionPattern = "";
                    zlUserDb.IsConditionWithAreaButton = false;
                    zlUserDb.CardConditionAreaButtonPattern = "";
                    zlUserDb.CardStepPattern = "";
                    zlUserDb.IsHaveSetAttributeSystem = false;
                    zlUserDb.AttributeName = "";
                    zlUserDb.IsConditionWithInputText = false;
                    zlUserDb.CardConditionWithInputTextPattern = "";
                    _appZaloUser.Update(zlUserDb);
                    _appZaloUser.Save();

                    if (_isSearchAI) //_isSearchAI
                    {
                        var systemConfigDb = _settingService.GetListSystemConfigByBotId(botId);
                        var systemConfigVm = Mapper.Map<IEnumerable<BotProject.Model.Models.SystemConfig>, IEnumerable<SystemConfigViewModel>>(systemConfigDb);
                        if (systemConfigVm.Count() == 0)
                        {
                            return await SendMessage(ZaloTemplate.GetMessageTemplateText("Tìm kiếm xử lý ngôn ngữ tự nhiên hiện không hoạt động, bạn vui lòng thử lại sau nhé!", sender));// not match
                        }
                        string nameFunctionAPI = "";
                        string number = "";
                        string field = "";
                        string valueBotId = "";
                        foreach (var item in systemConfigVm)
                        {
                            if (item.Code == "UrlAPI")
                                nameFunctionAPI = item.ValueString;
                            if (item.Code == "ParamBotID")
                                valueBotId = item.ValueString;
                            if (item.Code == "ParamAreaID")
                                field = item.ValueString;
                            if (item.Code == "ParamNumberResponse")
                                number = item.ValueString;
                        }
                        hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_006;
                        //AddHistory(hisVm);
                        string resultAPI = GetRelatedQuestionToZalo(nameFunctionAPI, text, field, number, valueBotId);
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
                            await SendMessageTask(ZaloTemplate.GetMessageTemplateText(totalFind, sender).ToString(), sender);
                            string strTemplateGenericRelatedQuestion = ZaloTemplate.GetMessageTemplateGenericByList(sender, lstQnaAPI).ToString();
                            return await SendMessage(strTemplateGenericRelatedQuestion, sender);
                        }
                    }

                    _dicNotMatch = new Dictionary<string, string>() {
                        {"NOT_MATCH_01", "Xin lỗi,em chưa hiểu ý anh/chị ạ!"},
                        {"NOT_MATCH_02", "Anh/chị có thể giải thích thêm được không?"},
                        {"NOT_MATCH_03", "Chưa hiểu lắm ạ, anh/chị có thể nói rõ hơn được không ạ?"},
                        {"NOT_MATCH_04", "Xin lỗi, Anh/chị có thể giải thích thêm được không?"},
                        {"NOT_MATCH_05", "Xin lỗi, Tôi chưa được học để hiểu nội dung này?"}
                    };


                    string strDefaultNotMatch = "";
                    foreach (var item in _dicNotMatch)
                    {
                        string itemNotMatch = item.Key;
                        if (itemNotMatch.Contains(result.Trim().Replace(".", String.Empty)))
                        {
                            strDefaultNotMatch = item.Value;
                        }
                    }
                    return await SendMessage(ZaloTemplate.GetMessageTemplateTextAndQuickReply(strDefaultNotMatch, sender, _contactAdmin, _titlePayloadContactAdmin));// not match
                }

                // input là postback
                if (text.Contains("postback_card"))
                {
                    var cardDb = _cardService.GetCardByPattern(text.Replace(".", String.Empty));
                    if (!String.IsNullOrEmpty(cardDb.AttributeSystemName))
                    {
                        zlUserDb.IsHaveSetAttributeSystem = true;
                        zlUserDb.AttributeName = cardDb.AttributeSystemName;
                    }
                    else
                    {
                        zlUserDb.IsHaveSetAttributeSystem = false;
                        zlUserDb.AttributeName = "";
                    }

                    if (cardDb.CardStepID != null)
                    {
                        zlUserDb.CardStepPattern = "postback_card_" + cardDb.CardStepID;
                        if (cardDb.IsConditionWithInputText)// yêu cầu nhập text để chuyển sang card step
                        {
                            zlUserDb.IsConditionWithInputText = true;
                            zlUserDb.CardConditionWithInputTextPattern = zlUserDb.CardStepPattern;
                        }
                        else
                        {
                            zlUserDb.IsConditionWithInputText = false;
                            zlUserDb.CardConditionWithInputTextPattern = "";
                        }
                    }
                    else
                    {
                        zlUserDb.CardStepPattern = "";
                    }

                    if (cardDb.IsHaveCondition)
                    {
                        zlUserDb.IsHaveCardCondition = true;
                        zlUserDb.CardConditionPattern = text.Replace(".", String.Empty);
                    }
                    else
                    {
                        zlUserDb.IsHaveCardCondition = false;
                        zlUserDb.CardConditionPattern = "";
                    }

                    if (cardDb.IsConditionWithAreaButton)
                    {
                        zlUserDb.IsConditionWithAreaButton = true;
                        zlUserDb.CardConditionAreaButtonPattern = text.Replace(".", String.Empty);
                    }
                    else
                    {
                        zlUserDb.IsConditionWithAreaButton = false;
                        zlUserDb.CardConditionAreaButtonPattern = "";
                    }
                    _appZaloUser.Update(zlUserDb);
                    _appZaloUser.Save();

                    string tempJsonZalo = cardDb.TemplateJsonZalo;
                    if (!String.IsNullOrEmpty(tempJsonZalo))
                    {
                        tempJsonZalo = tempJsonZalo.Trim();
                        string[] strArrayJson = Regex.Split(tempJsonZalo, "split");//nhớ thêm bên formcard xử lý lục trên face
                        if (strArrayJson.Length != 0)
                        {
                            var strArray = strArrayJson.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                            foreach (var temp in strArray)
                            {
                                string tempJson = temp;
                                await SendMessageTask(tempJson, sender);
                            }
                        }
                    }
                    if (cardDb.CardStepID != null && cardDb.IsConditionWithInputText == false)
                    {
                        zlUserDb.CardStepPattern = "postback_card_" + cardDb.CardStepID;
                        return await ExcuteMessage(zlUserDb.CardStepPattern, sender, botId);
                    }
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }

                //chat admin
                if (text.Contains(_contactAdmin))
                {
                    zlUserDb.IsHaveCardCondition = false;
                    zlUserDb.CardConditionPattern = "";
                    zlUserDb.CardStepPattern = "";
                    zlUserDb.IsHaveSetAttributeSystem = false;
                    _appZaloUser.Update(zlUserDb);
                    _appZaloUser.Save();

                    string strTempPostbackContactAdmin = aimlBotResult.SubQueries[0].Template;
                    bool isPostbackContactAdmin = Regex.Match(strTempPostbackContactAdmin, "<template><srai>postback_card_(\\d+)</srai></template>").Success;
                    if (isPostbackContactAdmin)
                    {
                        strTempPostbackContactAdmin = Regex.Replace(strTempPostbackContactAdmin, @"<(.|\n)*?>", "").Trim();
                        var cardDb = _cardService.GetCardByPattern(strTempPostbackContactAdmin.Replace(".", String.Empty));
                        string tempJsonZalo = cardDb.TemplateJsonZalo;
                        if (!String.IsNullOrEmpty(tempJsonZalo))
                        {
                            tempJsonZalo = tempJsonZalo.Trim();
                            string[] strArrayJson = Regex.Split(tempJsonZalo, "split");//nhớ thêm bên formcard xử lý lục trên face
                            if (strArrayJson.Length != 0)
                            {
                                var strArray = strArrayJson.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                                foreach (var temp in strArray)
                                {
                                    string tempJson = temp;
                                    await SendMessageTask(tempJson, sender);
                                }

                                return new HttpResponseMessage(HttpStatusCode.OK);
                            }
                        }
                    }
                }

                // nếu nhập text -> output là postback
                string strTempPostback = aimlBotResult.SubQueries[0].Template;
                bool isPostback = Regex.Match(strTempPostback, "<template><srai>postback_card_(\\d+)</srai></template>").Success;
                if (isPostback)
                {
                    strTempPostback = Regex.Replace(strTempPostback, @"<(.|\n)*?>", "").Trim();
                    var cardDb = _cardService.GetCardByPattern(strTempPostback.Replace(".", String.Empty));
                    if (cardDb.ID == 4031)
                    {
                        if (zlUserDb.Age == 0)//nếu thông tin tuổi chưa có trả về thẻ hỏi thông tin
                        {
                            return await ExcuteMessage("postback_card_8917", sender, botId);
                        }
                    }

                    if (!String.IsNullOrEmpty(cardDb.AttributeSystemName))
                    {
                        zlUserDb.IsHaveSetAttributeSystem = true;
                        zlUserDb.AttributeName = cardDb.AttributeSystemName;
                    }
                    else
                    {
                        zlUserDb.IsHaveSetAttributeSystem = false;
                        zlUserDb.AttributeName = "";
                    }

                    if (cardDb.CardStepID != null)
                    {
                        zlUserDb.CardStepPattern = "postback_card_" + cardDb.CardStepID;
                        if (cardDb.IsConditionWithInputText)// yêu cầu nhập text để chuyển sang card step
                        {
                            zlUserDb.IsConditionWithInputText = true;
                            zlUserDb.CardConditionWithInputTextPattern = zlUserDb.CardStepPattern;
                        }
                        else
                        {
                            zlUserDb.IsConditionWithInputText = false;
                            zlUserDb.CardConditionWithInputTextPattern = "";
                        }
                    }
                    else
                    {
                        zlUserDb.CardStepPattern = "";
                    }

                    if (cardDb.IsHaveCondition)
                    {
                        zlUserDb.IsHaveCardCondition = true;
                        zlUserDb.CardConditionPattern = text.Replace(".", String.Empty);
                    }
                    else
                    {
                        zlUserDb.IsHaveCardCondition = false;
                        zlUserDb.CardConditionPattern = "";
                    }

                    if (cardDb.IsConditionWithAreaButton)
                    {
                        zlUserDb.IsConditionWithAreaButton = true;
                        zlUserDb.CardConditionAreaButtonPattern = text.Replace(".", String.Empty);
                    }
                    else
                    {
                        zlUserDb.IsConditionWithAreaButton = false;
                        zlUserDb.CardConditionAreaButtonPattern = "";
                    }
                    _appZaloUser.Update(zlUserDb);
                    _appZaloUser.Save();

                    string tempJsonZalo = cardDb.TemplateJsonZalo;
                    if (!String.IsNullOrEmpty(tempJsonZalo))
                    {
                        tempJsonZalo = tempJsonZalo.Trim();
                        string[] strArrayJson = Regex.Split(tempJsonZalo, "split");//nhớ thêm bên formcard xử lý lục trên face
                        if (strArrayJson.Length != 0)
                        {
                            var strArray = strArrayJson.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                            foreach (var temp in strArray)
                            {
                                string tempJson = temp;
                                await SendMessageTask(tempJson, sender);
                            }
                        }
                    }
                    if (cardDb.CardStepID != null && cardDb.IsConditionWithInputText == false)
                    {
                        zlUserDb.CardStepPattern = "postback_card_" + cardDb.CardStepID;
                        return await ExcuteMessage(zlUserDb.CardStepPattern, sender, botId);
                    }
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }

                //trường hợp trả về câu hỏi random chứa postpack
                bool isPostbackAnswer = Regex.Match(strTempPostback, "<template><srai>postback_answer_(\\d+)</srai></template>").Success;
                if (isPostbackAnswer)
                {
                    if (result.Contains("postback_card"))
                    {
                        var cardDb = _cardService.GetCardByPattern(result.Replace(".", String.Empty));
                        if (!String.IsNullOrEmpty(cardDb.AttributeSystemName))
                        {
                            zlUserDb.IsHaveSetAttributeSystem = true;
                            zlUserDb.AttributeName = cardDb.AttributeSystemName;
                        }
                        else
                        {
                            zlUserDb.IsHaveSetAttributeSystem = false;
                            zlUserDb.AttributeName = "";
                        }

                        if (cardDb.CardStepID != null)
                        {
                            zlUserDb.CardStepPattern = "postback_card_" + cardDb.CardStepID;
                            if (cardDb.IsConditionWithInputText)// yêu cầu nhập text để chuyển sang card step
                            {
                                zlUserDb.IsConditionWithInputText = true;
                                zlUserDb.CardConditionWithInputTextPattern = zlUserDb.CardStepPattern;
                            }
                            else
                            {
                                zlUserDb.IsConditionWithInputText = false;
                                zlUserDb.CardConditionWithInputTextPattern = "";
                            }
                        }
                        else
                        {
                            zlUserDb.CardStepPattern = "";
                        }

                        if (cardDb.IsHaveCondition)
                        {
                            zlUserDb.IsHaveCardCondition = true;
                            zlUserDb.CardConditionPattern = text.Replace(".", String.Empty);
                        }
                        else
                        {
                            zlUserDb.IsHaveCardCondition = false;
                            zlUserDb.CardConditionPattern = "";
                        }

                        if (cardDb.IsConditionWithAreaButton)
                        {
                            zlUserDb.IsConditionWithAreaButton = true;
                            zlUserDb.CardConditionAreaButtonPattern = text.Replace(".", String.Empty);
                        }
                        else
                        {
                            zlUserDb.IsConditionWithAreaButton = false;
                            zlUserDb.CardConditionAreaButtonPattern = "";
                        }
                        _appZaloUser.Update(zlUserDb);
                        _appZaloUser.Save();

                        string tempJsonZalo = cardDb.TemplateJsonZalo;
                        if (!String.IsNullOrEmpty(tempJsonZalo))
                        {
                            tempJsonZalo = tempJsonZalo.Trim();
                            string[] strArrayJson = Regex.Split(tempJsonZalo, "split");//nhớ thêm bên formcard xử lý lục trên face
                            if (strArrayJson.Length != 0)
                            {
                                var strArray = strArrayJson.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                                foreach (var temp in strArray)
                                {
                                    string tempJson = temp;
                                    await SendMessageTask(tempJson, sender);
                                }
                            }
                        }
                        if (cardDb.CardStepID != null && cardDb.IsConditionWithInputText == false)
                        {
                            zlUserDb.CardStepPattern = "postback_card_" + cardDb.CardStepID;
                            return await ExcuteMessage(zlUserDb.CardStepPattern, sender, botId);
                        }
                        return new HttpResponseMessage(HttpStatusCode.OK);
                    }
                }

                return await SendMessage(ZaloTemplate.GetMessageTemplateText(result, sender));

            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }

        /// <summary>
        /// send message
        /// </summary>
        /// <param name="json">json</param>
        private async Task<HttpResponseMessage> SendMessage(JObject json)
        {
            HttpResponseMessage res;
            using (HttpClient client = new HttpClient())
            {
                string templateJson = json.ToString();
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
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                res = await client.PostAsync($"https://openapi.zalo.me/v2.0/oa/message?access_token=" + pageToken + "", new StringContent(templateJson, Encoding.UTF8, "application/json"));
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
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
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    res = await client.PostAsync($"https://openapi.zalo.me/v2.0/oa/message?access_token=" + pageToken + "", new StringContent(templateJson, Encoding.UTF8, "application/json"));
                }
            }
            return new HttpResponseMessage(HttpStatusCode.OK);

        }

        private async Task SendMessageTask(string templateJson, string sender)
        {
            if (!String.IsNullOrEmpty(templateJson))
            {
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
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage res = await client.PostAsync($"https://openapi.zalo.me/v2.0/oa/message?access_token=" + pageToken + "", new StringContent(templateJson, Encoding.UTF8, "application/json"));
                }
            }
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

        private ProfileUser GetProfileUser(string senderId)
        {
            ProfileUser user = new ProfileUser();
            using (HttpClient client = new HttpClient())
            {
                string userId = JObject.FromObject(
                 new
                 {
                     user_id = senderId
                 }).ToString();
                HttpResponseMessage res = new HttpResponseMessage();
                res = client.GetAsync($"https://openapi.zalo.me/v2.0/oa/getprofile?access_token=" + pageToken + "&data="+ userId).Result;//gender y/c khi sử dụng
                if (res.IsSuccessStatusCode)
                {
                    var serializer = new JavaScriptSerializer();
                    serializer.MaxJsonLength = Int32.MaxValue;
                    user = serializer.Deserialize<ProfileUser>(res.Content.ReadAsStringAsync().Result);
                }
                return user;
            }
        }
        public static JObject GetUserID(string sender)
        {
            return JObject.FromObject(
                 new
                 {
                     recipient = new { user_id = sender }
                 });
        }

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
        private string GetRelatedQuestionToZalo(string nameFuncAPI, string question, string field, string number, string botId)
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

        private static async Task Schedule(string UserId, string strMessage, string pageToken, DateTime dTimeOut, string modulePayload)
        {
            try
            {
                // Grab the Scheduler instance from the Factory
                System.Collections.Specialized.NameValueCollection props = new System.Collections.Specialized.NameValueCollection
                {
                    { "quartz.serializer.type", "binary" }
                };
                StdSchedulerFactory factory = new StdSchedulerFactory(props);
                IScheduler scheduler = await factory.GetScheduler();
                // and start it off
                await scheduler.Start();

                IJobDetail job = JobBuilder.Create<ProactiveMessageJob>()
                     //.WithIdentity("ProactiveMsgJob", "ProactiveMsgJob") 
                     //.UsingJobData("userId", "0")
                     .Build();
                ITrigger trigger = TriggerBuilder.Create()
                        //.WithIdentity(triggerName, "ProactiveMsgJob")
                        .UsingJobData("UserId", UserId)
                        .UsingJobData("Message", strMessage)
                        .UsingJobData("PageToken", pageToken)
                        .UsingJobData("Payload", modulePayload)
                        .UsingJobData("TimeOut", dTimeOut.ToLocalTime().ToString())
                        .StartAt(dTimeOut.ToLocalTime())
                        //.WithSimpleSchedule(x => x
                        //    .WithIntervalInSeconds(5)
                        //    .WithRepeatCount(0))
                        //.ForJob("ProactiveMsgJob", group)
                        .Build();

                // Tell quartz to schedule the job using our trigger
                await scheduler.ScheduleJob(job, trigger);

                //await scheduler.Shutdown();
            }
            catch (SchedulerException se)
            {

            }
        }

        public class ProactiveMessageJob : IJob
        {
            private readonly string Domain = Helper.ReadString("Domain");
            private readonly string _sqlConnection = Helper.ReadString("SqlConnection");
            public async Task Execute(IJobExecutionContext context)
            {
                JobKey key = context.JobDetail.Key;
                //JobDataMap dataMapDefault = context.JobDetail.JobDataMap;
                JobDataMap dataMap = context.MergedJobDataMap;
                string userId = dataMap.GetString("UserId");
                string message = dataMap.GetString("Message");
                string pageToken = dataMap.GetString("PageToken");
                string TimeOut = dataMap.GetString("TimeOut");
                string payLoad = dataMap.GetString("Payload");
                DateTime dTimeOut = Convert.ToDateTime(TimeOut);

                DateTime timeOutDb;
                int resultTimeCompare = 3;
                var sqlConnection = new SqlConnection(_sqlConnection);
                sqlConnection.Open();

                SqlCommand command = new SqlCommand("Select TimeOut from [ApplicationZaloUsers] where UserId=@userId", sqlConnection);
                command.Parameters.AddWithValue("@userId", userId);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        timeOutDb = (DateTime)reader["TimeOut"];
                        resultTimeCompare = DateTime.Compare(DateTime.Now, timeOutDb);
                    }
                }
                command.ExecuteNonQuery();
                sqlConnection.Close();

                if (resultTimeCompare == 1)
                {
                    if (payLoad != CommonConstants.ModuleAdminContact)
                    {
                        await SendProactiveMessage(message, userId, pageToken, dTimeOut);

                        var sqlConnection2 = new SqlConnection(_sqlConnection);
                        sqlConnection2.Open();

                        SqlCommand command2 = new SqlCommand("UPDATE ApplicationZaloUsers SET PredicateName = @predicateName, PredicateValue = @predicateValue, IsHavePredicate = @isHavePredicate,IsHaveCardCondition = @isHaveCardCondition,CardConditionPattern = @cardConditionPattern Where UserId=@userId", sqlConnection2);
                        command2.Parameters.AddWithValue("@userId", userId);
                        command2.Parameters.AddWithValue("@predicateName", "");
                        command2.Parameters.AddWithValue("@predicateValue", "");
                        command2.Parameters.AddWithValue("@isHavePredicate", "0");
                        command2.Parameters.AddWithValue("@isHaveCardCondition", "0");
                        command2.Parameters.AddWithValue("@cardConditionPattern", "");
                        command2.ExecuteNonQuery();
                        sqlConnection2.Close();
                    }
                }
            }
            private async Task<HttpResponseMessage> SendProactiveMessage(string templateJson, string sender, string pageToken, DateTime dTimeOut)
            {
                HttpResponseMessage res;
                if (!String.IsNullOrEmpty(templateJson))
                {
                    templateJson = templateJson.Replace("{{senderId}}", sender);
                    templateJson = Regex.Replace(templateJson, "File/", Domain + "File/");
                    templateJson = Regex.Replace(templateJson, "<br />", "\\n");
                    templateJson = Regex.Replace(templateJson, "<br/>", "\\n");
                    templateJson = Regex.Replace(templateJson, @"\\n\\n", "\\n");
                    templateJson = Regex.Replace(templateJson, @"\\n\\r\\n", "\\n");
                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        res = await client.PostAsync($"https://openapi.zalo.me/v2.0/oa/message?access_token=" + pageToken + "", new StringContent(templateJson, Encoding.UTF8, "application/json"));
                    }
                }
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }

        private static async Task ScheduleOTP(string UserId, string telePhoneNumber, string mdVoucherId, string pageToken, DateTime dTimeOut)
        {
            try
            {
                // Grab the Scheduler instance from the Factory
                System.Collections.Specialized.NameValueCollection props = new System.Collections.Specialized.NameValueCollection
                {
                    { "quartz.serializer.type", "binary" }
                };
                StdSchedulerFactory factory = new StdSchedulerFactory(props);
                IScheduler scheduler = await factory.GetScheduler();
                // and start it off
                await scheduler.Start();

                IJobDetail job = JobBuilder.Create<CancleCodeOtpJob>()
                     .Build();
                ITrigger trigger = TriggerBuilder.Create()
                        .UsingJobData("UserId", UserId)
                        .UsingJobData("PageToken", pageToken)
                        .UsingJobData("PhoneNumber", telePhoneNumber)
                        .UsingJobData("MdVoucherID", mdVoucherId)
                        .UsingJobData("TimeOut", dTimeOut.ToLocalTime().ToString())
                        .StartAt(dTimeOut.ToLocalTime())
                        .Build();
                // Tell quartz to schedule the job using our trigger
                await scheduler.ScheduleJob(job, trigger);
            }
            catch (SchedulerException se)
            {

            }
        }

        public class CancleCodeOtpJob : IJob
        {
            private readonly string Domain = Helper.ReadString("Domain");
            private readonly string _sqlConnection = Helper.ReadString("SqlConnection");
            public async Task Execute(IJobExecutionContext context)
            {
                JobKey key = context.JobDetail.Key;
                //JobDataMap dataMapDefault = context.JobDetail.JobDataMap;
                JobDataMap dataMap = context.MergedJobDataMap;
                string userId = dataMap.GetString("UserId");
                string pageToken = dataMap.GetString("PageToken");
                string phoneNumber = dataMap.GetString("PhoneNumber");
                string mdVoucherId = dataMap.GetString("MdVoucherID");
                string TimeOut = dataMap.GetString("TimeOut");
                DateTime dTimeOut = Convert.ToDateTime(TimeOut);

                bool isReceived = false;
                var sqlConnection = new SqlConnection(_sqlConnection);
                sqlConnection.Open();

                SqlCommand command = new SqlCommand("Select top 1 * from [UserTelePhones] where TelephoneNumber = @phone and MdVoucherID = @voucherID", sqlConnection);
                command.Parameters.AddWithValue("@phone", phoneNumber);
                command.Parameters.AddWithValue("@voucherID", mdVoucherId);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        isReceived = (bool)reader["IsReceive"];
                    }
                }
                command.ExecuteNonQuery();
                sqlConnection.Close();

                if (isReceived == false)
                {
                    var sqlConnection2 = new SqlConnection(_sqlConnection);
                    sqlConnection2.Open();

                    SqlCommand command2 = new SqlCommand("UPDATE UserTelePhones SET Code = @code Where TelephoneNumber = @phone and MdVoucherID = @voucherID", sqlConnection2);
                    command2.Parameters.AddWithValue("@phone", phoneNumber);
                    command2.Parameters.AddWithValue("@code", "timeout");
                    command2.Parameters.AddWithValue("@voucherID", mdVoucherId);
                    command2.ExecuteNonQuery();
                    sqlConnection2.Close();
                }

            }
        }
    }
}
