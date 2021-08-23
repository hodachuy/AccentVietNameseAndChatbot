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
    public class MessageZaloTNMTController : ApiController
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

        private IApplicationZaloUserService _appZaloUser;
        private BotService _botService;
        private IBotService _botDbService;
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
        //private Bot _bot;
        private User _user;

        private AccentService _accentService;
        public MessageZaloTNMTController(IErrorService errorService,
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
                              IApplicationThirdPartyService app3rd)
        {
            _errorService = errorService;
            //_accentService = AccentService.AccentInstance;
            _botDbService = botDbService;
            _settingService = settingService;
            _handleMdService = handleMdService;
            _mdService = mdService;
            _apiNLR = new ApiQnaNLRService();
            _mdKnowledgeService = mdKnowledgeService;
            _mdSearchService = mdSearchService;
            _aimlFileService = aimlFileService;
            _qnaService = qnaService;
            _botService = BotService.BotInstance;
            _moduleSearchEngineService = moduleSearchEngineService;
            _historyService = historyService;
            _cardService = cardService;
            _appZaloUser = appZaloUser;
            _app3rd = app3rd;
            //_accentService = new AccentService();// AccentService.AccentInstance;
            //_accentService = AccentService.AccentInstance;
        }

        public HttpResponseMessage Get()
        {
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        [HttpPost]
        public async Task<HttpResponseMessage> Post()
        {
            int botId = 5028;
            var body = await Request.Content.ReadAsStringAsync();
            //LogError(body);
            //BotLog.Info(body);
            if (body.Contains("user_send_text"))
            {

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

                // init stop word
                _stopWord = settingDb.StopWord;
                if (System.IO.File.Exists(_pathStopWord))
                {
                    string[] stopWordDefault = System.IO.File.ReadAllLines(_pathStopWord);
                    _stopWord += string.Join(",", stopWordDefault);
                }



                //if (settingDb.IsHaveMaintenance)
                //{
                //    await SendMessageTask(ZaloTemplate.GetMessageTemplateText(settingDb.MessageMaintenance, "{{senderId}}").ToString(), value.sender.id);
                //    return new HttpResponseMessage(HttpStatusCode.OK);
                //}


                var lstAIML = _aimlFileService.GetActiveByBotId(botId);
                var lstAIMLVm = Mapper.Map<IEnumerable<AIMLFileViewModel>, IEnumerable<AIMLViewModel>>(lstAIML);
                _botService.loadAIMLFromDatabase(lstAIMLVm);
                string _userId = Guid.NewGuid().ToString();
                _user = _botService.loadUserBot(_userId);
                //_user = _botService.loadUserBot(value.sender.id);

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

                // init stop word
                _stopWord = settingDb.StopWord;
                if (System.IO.File.Exists(_pathStopWord))
                {
                    string[] stopWordDefault = System.IO.File.ReadAllLines(_pathStopWord);
                    _stopWord += string.Join(",", stopWordDefault);
                }

                var lstAIML = _aimlFileService.GetActiveByBotId(botId);
                var lstAIMLVm = Mapper.Map<IEnumerable<AIMLFileViewModel>, IEnumerable<AIMLViewModel>>(lstAIML);
                _botService.loadAIMLFromDatabase(lstAIMLVm);
                string _userId = Guid.NewGuid().ToString();
                _user = _botService.loadUserBot(_userId);
                //_user = _botService.loadUserBot(value.sender.id);

                if(value.message.attachments[0].type == "audio")
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

                if (settingVm.CardID.HasValue)
                {
                    var lstAIML = _aimlFileService.GetActiveByBotId(botId);
                    var lstAIMLVm = Mapper.Map<IEnumerable<AIMLFileViewModel>, IEnumerable<AIMLViewModel>>(lstAIML);
                    _botService.loadAIMLFromDatabase(lstAIMLVm);
                    string _userId = Guid.NewGuid().ToString();
                    _user = _botService.loadUserBot(_userId);
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


            if (!text.Contains("postback") && !text.Contains(_contactAdmin))
            {
                //_accentService = new AccentService();// AccentService.AccentInstance;
                _accentService = AccentService.SingleInstance;
                text = _accentService.GetAccentVN(text);
            }

            string attributeValue = "";
            // Xét payload postback nếu postback từ quickreply sẽ chứa thêm sperator - và tiêu đề nút
            if (text.Contains("postback"))
            {
                var arrPostback = Regex.Split(text, "-");
                if (arrPostback.Length > 1)
                {
                    attributeValue = arrPostback[1];
                }
                text = arrPostback[0];
            }

            // Lọc từ cấm
            if (!String.IsNullOrEmpty(_stopWord))
            {
                string[] arrStopWord = _stopWord.Split(',');
                if (arrStopWord.Length != 0)
                {
                    foreach (var w in arrStopWord)
                    {
                        text = Regex.Replace(text, "\\b" + Regex.Escape(w) + "\\b", String.Empty).Trim();
                    }
                }
            }

            if (String.IsNullOrEmpty(text))
            {
                string strDefaultNotMatch = "Anh/chị cho em biết thêm chi tiết được không ạ";
                if (_isSearchAI)
                {
                    return await SendMessage(ZaloTemplate.GetMessageTemplateTextAndQuickReply(strDefaultNotMatch, sender, _contactAdmin, _titlePayloadContactAdmin));// not match
                }

                strDefaultNotMatch = "Anh/chị vui lòng chọn Chat với chuyên viên để được tư vấn chi tiết hơn ạ";
                return await SendMessage(ZaloTemplate.GetMessageTemplateTextAndQuickReply(strDefaultNotMatch, sender, _contactAdmin, _titlePayloadContactAdmin));// not match
            }

            HistoryViewModel hisVm = new HistoryViewModel();
            hisVm.BotID = botId;
            hisVm.CreatedDate = DateTime.Now;
            hisVm.UserSay = text;
            hisVm.UserName = sender;
            hisVm.Type = CommonConstants.TYPE_ZALO;

            DateTime dStartedTime = DateTime.Now;
            DateTime dTimeOut = DateTime.Now.AddSeconds(_timeOut);

            try
            {
                ApplicationZaloUser zlUserDb = new ApplicationZaloUser();
                zlUserDb = _appZaloUser.GetByUserId(sender);

                if (zlUserDb != null)
                {
                    if (zlUserDb.PredicateName == "Admin_Contact")
                    {
                        var handleAdminContact = _handleMdService.HandleIsAdminContact(text, botId);
                        hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_004;
                        AddHistory(hisVm);
                        if (text.Contains("postback") || text.Contains(_contactAdmin))
                        {
                            zlUserDb.IsHavePredicate = false;
                            zlUserDb.PredicateName = "";
                            zlUserDb.PredicateValue = "";
                            zlUserDb.IsHaveCardCondition = false;
                            zlUserDb.CardConditionPattern = "";
                            zlUserDb.IsConditionWithAreaButton = false;
                            zlUserDb.CardConditionAreaButtonPattern = "";
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

                if (_isHaveTimeOut)
                {
                    if (zlUserDb != null)
                    {
                        zlUserDb.StartedOn = dStartedTime;
                        zlUserDb.TimeOut = dTimeOut;
                        _appZaloUser.Update(zlUserDb);
                        _appZaloUser.Save();
                    }
                    await Schedule(sender, ZaloTemplate.GetMessageTemplateTextAndQuickReply(_messageProactive, "{{senderId}}", _patternCardPayloadProactive, _titleCardPayloadProactive).ToString(), pageToken, dTimeOut, text);
                    //Schedule(sender, ZaloTemplate.GetMessageTemplateText(_messageProactive, "{{senderId}}").ToString(), pageToken, dTimeOut);
                }

                if (zlUserDb == null)
                {
                    zlUserDb = new ApplicationZaloUser();
                    ApplicationZaloUserViewModel zlUserVm = new ApplicationZaloUserViewModel();
                    zlUserVm.UserId = sender;
                    zlUserVm.IsHavePredicate = false;
                    zlUserVm.IsProactiveMessage = false;
                    zlUserVm.TimeOut = dTimeOut;
                    zlUserDb.CreatedDate = DateTime.Now;
                    zlUserDb.StartedOn = dStartedTime;
                    zlUserDb.UpdateZaloUser(zlUserVm);
                    _appZaloUser.Add(zlUserDb);
                    _appZaloUser.Save();
                }
                else
                {
                    zlUserDb.StartedOn = DateTime.Now;
                    zlUserDb.TimeOut = dTimeOut;

                    // Nếu có yêu cầu click thẻ để đi theo luồng
                    if (zlUserDb.IsHaveCardCondition)
                    {
                        if (text.Contains("postback") || text.Contains(_contactAdmin))
                        {

                        }
                        else
                        {
                            var cardDb = _cardService.GetSingleCondition(zlUserDb.CardConditionPattern);
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
                                _appZaloUser.Update(zlUserDb);
                                _appZaloUser.Save();
                                return await ExcuteMessage(text, sender, botId);
                            }

                            string predicateValue = zlUserDb.PredicateValue;
                            var handleMdSearch = _handleMdService.HandleIsSearchAPI(text, predicateValue, "");

                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_005;
                            AddHistory(hisVm);

                            return await SendMessage(handleMdSearch.TemplateJsonZalo, sender);

                        }
                        if (predicateName == "Age")
                        {
                            var handleAge = _handleMdService.HandledIsAge(text, botId);
                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_004;
                            AddHistory(hisVm);
                            if (handleAge.Status)// đúng age
                            {
                                zlUserDb.IsHavePredicate = false;
                                zlUserDb.PredicateName = "";
                                zlUserDb.PredicateValue = "";
                                zlUserDb.IsHaveCardCondition = false;
                                zlUserDb.CardConditionPattern = "";
                                zlUserDb.PhoneNumber = text;
                                zlUserDb.IsConditionWithAreaButton = false;
                                zlUserDb.CardConditionAreaButtonPattern = "";
                                _appZaloUser.Update(zlUserDb);
                                _appZaloUser.Save();
                                if (!String.IsNullOrEmpty(handleAge.Postback))
                                {
                                    return await ExcuteMessage(handleAge.Postback, sender, botId);
                                }
                            }
                            return await SendMessage(handleAge.TemplateJsonZalo, sender);
                        }
                        if (predicateName == "Phone")
                        {
                            var handlePhone = _handleMdService.HandleIsPhoneNumber(text, botId);

                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_004;
                            AddHistory(hisVm);
                            if (handlePhone.Status)// đúng số dt
                            {
                                zlUserDb.IsHavePredicate = false;
                                zlUserDb.PredicateName = "";
                                zlUserDb.PredicateValue = "";
                                zlUserDb.IsHaveCardCondition = false;
                                zlUserDb.CardConditionPattern = "";
                                zlUserDb.PhoneNumber = text;
                                zlUserDb.IsConditionWithAreaButton = false;
                                zlUserDb.CardConditionAreaButtonPattern = "";
                                _appZaloUser.Update(zlUserDb);
                                _appZaloUser.Save();

                                // Nếu đúng số điện thoại sẽ trả về thẻ tiếp theo nếu có
                                if (!String.IsNullOrEmpty(handlePhone.Postback))
                                {
                                    return await ExcuteMessage(handlePhone.Postback, sender, botId);
                                }
                            }
                            return await SendMessage(handlePhone.TemplateJsonZalo, sender);
                        }
                        if (predicateName == "Email")
                        {
                            var handleEmail = _handleMdService.HandledIsEmail(text, botId);
                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_004;
                            AddHistory(hisVm);
                            if (handleEmail.Status)// đúng email
                            {
                                zlUserDb.IsHavePredicate = false;
                                zlUserDb.PredicateName = "";
                                zlUserDb.PredicateValue = "";
                                zlUserDb.IsHaveCardCondition = false;
                                zlUserDb.CardConditionPattern = "";
                                zlUserDb.IsConditionWithAreaButton = false;
                                zlUserDb.CardConditionAreaButtonPattern = "";
                                _appZaloUser.Update(zlUserDb);
                                _appZaloUser.Save();

                                if (!String.IsNullOrEmpty(handleEmail.Postback))
                                {
                                    return await ExcuteMessage(handleEmail.Postback, sender, botId);
                                }
                            }
                            return await SendMessage(handleEmail.TemplateJsonZalo, sender);
                        }
                        if (predicateName == "Engineer_Name")
                        {
                            var handleEngineerName = _handleMdService.HandleIsEngineerName(text, botId);
                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_004;
                            AddHistory(hisVm);
                            if (handleEngineerName.Status)
                            {
                                zlUserDb.IsHavePredicate = false;
                                zlUserDb.PredicateName = "";
                                zlUserDb.PredicateValue = "";
                                zlUserDb.IsHaveCardCondition = false;
                                zlUserDb.CardConditionPattern = "";
                                zlUserDb.IsConditionWithAreaButton = false;
                                zlUserDb.CardConditionAreaButtonPattern = "";
                                zlUserDb.EngineerName = text;
                                if (text.Contains("postback") || text.Contains(_contactAdmin))
                                {
                                    zlUserDb.EngineerName = "";
                                }
                                _appZaloUser.Update(zlUserDb);
                                _appZaloUser.Save();

                                if (!String.IsNullOrEmpty(handleEngineerName.Postback))
                                {
                                    return await ExcuteMessage(handleEngineerName.Postback, sender, botId);
                                }
                            }
                            return await SendMessage(handleEngineerName.TemplateJsonZalo, sender);
                        }
                        if (predicateName == "Voucher")
                        {
                            string mdVoucherId = zlUserDb.PredicateValue;
                            if (text.Contains("postback_card") || text.Contains(_contactAdmin))
                            {
                                zlUserDb.IsHavePredicate = false;
                                zlUserDb.PredicateName = "";
                                zlUserDb.PredicateValue = "";
                                zlUserDb.IsHaveCardCondition = false;
                                zlUserDb.CardConditionPattern = "";
                                zlUserDb.IsConditionWithAreaButton = false;
                                zlUserDb.CardConditionAreaButtonPattern = "";
                                _appZaloUser.Update(zlUserDb);
                                _appZaloUser.Save();
                                return await ExcuteMessage(text, sender, botId);
                            }

                            var handleMdVoucher = _handleMdService.HandleIsVoucher(text, mdVoucherId, zlUserDb.EngineerName, zlUserDb.BranchOTP, hisVm.Type);

                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_007;
                            AddHistory(hisVm);
                            if (handleMdVoucher.Status)
                            {
                                string telePhoneNumber = text;
                                string[] strArrSpecial = new string[] { "-", " ", ",", ":" };
                                //check phonenumber có kèm theo serialnumber không
                                foreach (var item in strArrSpecial)
                                {
                                    if (text.Contains(item))
                                    {
                                        var arrStrPhone = Regex.Split(text, item);
                                        telePhoneNumber = arrStrPhone[0];
                                        break;
                                    }
                                }

                                zlUserDb.IsHavePredicate = true;
                                zlUserDb.PredicateName = "IsVoucherOTP";
                                zlUserDb.PredicateValue = mdVoucherId;// voucherId
                                zlUserDb.PhoneNumber = telePhoneNumber;
                                zlUserDb.IsHaveCardCondition = false;
                                zlUserDb.CardConditionPattern = "";
                                zlUserDb.IsConditionWithAreaButton = false;
                                zlUserDb.CardConditionAreaButtonPattern = "";
                                _appZaloUser.Update(zlUserDb);
                                _appZaloUser.Save();

                                // send sms otp
                                await SendMessageTask(handleMdVoucher.TemplateJsonZalo, sender);

                                // gọi timeout thời gian hủy otp
                                if (_isHaveTimeOutOTP)
                                {
                                    DateTime dTimeOutOTP = DateTime.Now.AddSeconds(_timeOutOTP);
                                    await ScheduleOTP(sender, telePhoneNumber, mdVoucherId, pageToken, dTimeOutOTP);
                                }

                                return new HttpResponseMessage(HttpStatusCode.OK);

                                //await SendMessageTask(handleMdVoucher.TemplateJsonZalo, sender);
                                //return await SendMessage(ZaloTemplate.GetMessageTemplateText(("Mã OTP đang được gửi, Anh/Chị chờ tí nhé...").ToString(), sender));
                            }
                            return await SendMessage(handleMdVoucher.TemplateJsonZalo, sender);
                        }
                        if (predicateName == "IsVoucherOTP")
                        {
                            string mdVoucherId = zlUserDb.PredicateValue;
                            string phoneNumber = zlUserDb.PhoneNumber;
                            // thẻ gửi lại sms
                            if (text.Equals("CARD_RESEND_SMS"))
                            {
                                var handleMdVoucher = _handleMdService.HandleIsVoucher(phoneNumber, mdVoucherId, zlUserDb.EngineerName, zlUserDb.BranchOTP, hisVm.Type);
                                if (handleMdVoucher.Status)
                                {
                                    // send sms otp
                                    await SendMessageTask(handleMdVoucher.TemplateJsonZalo, sender);
                                    // gọi timeout thời gian hủy otp
                                    if (_isHaveTimeOutOTP)
                                    {
                                        DateTime dTimeOutOTP = DateTime.Now.AddSeconds(_timeOutOTP);
                                        await ScheduleOTP(sender, phoneNumber, mdVoucherId, pageToken, dTimeOutOTP);
                                    }
                                    return new HttpResponseMessage(HttpStatusCode.OK);
                                }
                                return await SendMessage(handleMdVoucher.TemplateJsonZalo, sender);
                            }
                            if (text.Contains("postback_card") || text.Contains(_contactAdmin))
                            {
                                zlUserDb.IsHavePredicate = false;
                                zlUserDb.PredicateName = "";
                                zlUserDb.PredicateValue = "";
                                zlUserDb.IsHaveCardCondition = false;
                                zlUserDb.CardConditionPattern = "";
                                zlUserDb.IsConditionWithAreaButton = false;
                                zlUserDb.CardConditionAreaButtonPattern = "";
                                _appZaloUser.Update(zlUserDb);
                                _appZaloUser.Save();
                                return await ExcuteMessage(text, sender, botId);
                            }
                            var handleOTP = _handleMdService.HandleIsCheckOTP(text, phoneNumber, mdVoucherId, _messageOTP);
                            if (handleOTP.Status)
                            {
                                zlUserDb.IsHavePredicate = false;
                                zlUserDb.PredicateName = "";
                                zlUserDb.PredicateValue = "";
                                zlUserDb.IsHaveCardCondition = false;
                                zlUserDb.CardConditionPattern = "";
                                zlUserDb.IsConditionWithAreaButton = false;
                                zlUserDb.CardConditionAreaButtonPattern = "";
                                _appZaloUser.Update(zlUserDb);
                                _appZaloUser.Save();

                                // trả về image voucher + text message end
                                string[] arrMsgHandleOTP = Regex.Split(handleOTP.TemplateJsonZalo, "split");
                                foreach (var itemMessageJson in arrMsgHandleOTP)
                                {
                                    await SendMessageTask(itemMessageJson, sender);
                                }
                                return new HttpResponseMessage(HttpStatusCode.OK);

                            }
                            return await SendMessage(handleOTP.TemplateJsonZalo, sender);
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
                            AddHistory(hisVm);

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
                            _appZaloUser.Update(zlUserDb);
                            _appZaloUser.Save();

                            hisVm.UserSay = "[Tra cứu]";
                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_003;
                            AddHistory(hisVm);

                            return await SendMessage(handleMdSearch.TemplateJsonZalo, sender);

                        }
                        if (text.Contains(CommonConstants.ModuleEngineerName))
                        {
                            var handleEngineerName = _handleMdService.HandleIsEngineerName(text, botId);

                            zlUserDb.IsHavePredicate = true;
                            zlUserDb.PredicateName = "Engineer_Name";
                            zlUserDb.EngineerName = "";
                            zlUserDb.IsHaveCardCondition = false;
                            zlUserDb.CardConditionPattern = "";
                            zlUserDb.BranchOTP = "";
                            zlUserDb.IsConditionWithAreaButton = false;
                            zlUserDb.CardConditionAreaButtonPattern = "";
                            _appZaloUser.Update(zlUserDb);
                            _appZaloUser.Save();

                            hisVm.UserSay = "[Tên hoặc mã kỹ sư]";
                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_003;
                            AddHistory(hisVm);

                            return await SendMessage(handleEngineerName.TemplateJsonZalo, sender);
                        }
                        if (text.Contains(CommonConstants.ModuleAge))
                        {
                            var handleAge = _handleMdService.HandledIsAge(text, botId);

                            zlUserDb.IsHavePredicate = true;
                            zlUserDb.PredicateName = "Age";
                            zlUserDb.IsHaveCardCondition = false;
                            zlUserDb.CardConditionPattern = "";
                            zlUserDb.IsConditionWithAreaButton = false;
                            zlUserDb.CardConditionAreaButtonPattern = "";
                            _appZaloUser.Update(zlUserDb);
                            _appZaloUser.Save();

                            hisVm.UserSay = "[Tuổi]";
                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_003;
                            AddHistory(hisVm);

                            return await SendMessage(handleAge.TemplateJsonZalo, sender);
                        }
                        if (text.Contains(CommonConstants.ModulePhone))
                        {
                            var handlePhone = _handleMdService.HandleIsPhoneNumber(text, botId);
                            zlUserDb.IsHavePredicate = true;
                            zlUserDb.PredicateName = "Phone";
                            zlUserDb.IsHaveCardCondition = false;
                            zlUserDb.CardConditionPattern = "";
                            zlUserDb.IsConditionWithAreaButton = false;
                            zlUserDb.CardConditionAreaButtonPattern = "";
                            _appZaloUser.Update(zlUserDb);
                            _appZaloUser.Save();

                            hisVm.UserSay = "[Số điện thoại]";
                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_003;
                            AddHistory(hisVm);

                            return await SendMessage(handlePhone.TemplateJsonZalo, sender);
                        }
                        if (text.Contains(CommonConstants.ModuleEmail))
                        {
                            var handleEmail = _handleMdService.HandledIsEmail(text, botId);
                            zlUserDb.IsHavePredicate = true;
                            zlUserDb.PredicateName = "Email";
                            zlUserDb.IsHaveCardCondition = false;
                            zlUserDb.CardConditionPattern = "";
                            zlUserDb.IsConditionWithAreaButton = false;
                            zlUserDb.CardConditionAreaButtonPattern = "";
                            _appZaloUser.Update(zlUserDb);
                            _appZaloUser.Save();

                            hisVm.UserSay = "[Email]";
                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_003;
                            AddHistory(hisVm);

                            return await SendMessage(handleEmail.TemplateJsonZalo, sender);
                        }

                        if (text.Contains(CommonConstants.ModuleVoucher))
                        {
                            string mdVoucherId = text.Replace(".", String.Empty).Replace("postback_module_voucher_", "");
                            var handleMdVoucher = _handleMdService.HandleIsVoucher(text, mdVoucherId, zlUserDb.EngineerName, zlUserDb.BranchOTP, hisVm.Type);

                            zlUserDb.IsHavePredicate = true;
                            zlUserDb.PredicateName = "Voucher";
                            zlUserDb.PredicateValue = mdVoucherId;
                            zlUserDb.IsHaveCardCondition = false;
                            zlUserDb.CardConditionPattern = "";
                            zlUserDb.IsConditionWithAreaButton = false;
                            zlUserDb.CardConditionAreaButtonPattern = "";
                            _appZaloUser.Update(zlUserDb);
                            _appZaloUser.Save();

                            hisVm.UserSay = "[Voucher]";
                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_003;
                            AddHistory(hisVm);

                            return await SendMessage(handleMdVoucher.TemplateJsonZalo, sender);
                        }

                    }
                }

                //// Lấy target from knowledge base QnA trained mongodb
                //if (_isSearchAI)
                //{
                //    if (text.Contains("postback") == false || text.Contains("module") == false)
                //    {
                //        string target = _apiNLR.GetPrecidictTextClass(text, botId);
                //        if (!String.IsNullOrEmpty(target))
                //        {
                //            target = Regex.Replace(target, "\n", "").Replace("\"", "");
                //            QuesTargetViewModel quesTarget = new QuesTargetViewModel();
                //            quesTarget = _qnaService.GetQuesByTarget(target, botId);
                //            if (quesTarget != null)
                //            {
                //                text = quesTarget.ContentText;
                //            }
                //            hisVm.BotUnderStands = target;
                //        }
                //    }
                //}


                AIMLbot.Result aimlBotResult = _botService.Chat(text, _user);
                string result = aimlBotResult.OutputSentences[0].ToString();

                //tìm theo keyword trước, nếu keyword k có thì tìm theo core NLP
                if (result.Contains("NOT_MATCH"))
                {
                    // Lấy target from knowledge base QnA trained mongodb
                    //turn on AI
                    if (_isSearchAI)
                    {
                        if (text.Contains("postback") == false || text.Contains("module") == false)
                        {
                            string target = _apiNLR.GetPrecidictTextClass(text, botId);
                            if (!String.IsNullOrEmpty(target))
                            {
                                target = Regex.Replace(target, "\n", "").Replace("\"", "");

                                QuesTargetViewModel quesTarget = new QuesTargetViewModel();
                                quesTarget = _qnaService.GetQuesByTarget(target, botId);
                                if (quesTarget != null)
                                {
                                    text = quesTarget.ContentText;
                                }
                                hisVm.BotUnderStands = target;
                                // gọi lại aiml
                                aimlBotResult = _botService.Chat(text, _user);
                                result = aimlBotResult.OutputSentences[0].ToString();
                            }
                        }
                    }
                }

                // lưu lịch sử
                if (text.Contains("postback_card"))
                {
                    var cardDb = _cardService.GetSingleCondition(text);
                    if (cardDb != null)
                    {
                        hisVm.UserSay = "[" + cardDb.Name + "]";
                        hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_001;
                        AddHistory(hisVm);
                    }
                }
                else if (text.Contains("postback_module") == false && result.Contains("NOT_MATCH") == false)
                {
                    hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_003;
                    AddHistory(hisVm);
                }

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
                            _appZaloUser.Update(zlUserDb);
                            _appZaloUser.Save();

                            return await SendMessage(rsHandle, sender);
                        }
                    }
                }
                if (result.Contains("NOT_MATCH"))
                {
                    hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_002;
                    AddHistory(hisVm);

                    zlUserDb.IsHaveCardCondition = false;
                    zlUserDb.CardConditionPattern = "";
                    _appZaloUser.Update(zlUserDb);
                    _appZaloUser.Save();

                    if (_isHaveTimeOutOTP) //_isSearchAI
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
                        AddHistory(hisVm);
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
                        {"NOT_MATCH_04", "Xin lỗi, anh/chị có thể giải thích thêm được không?"},
                        {"NOT_MATCH_05", "Xin lỗi, Anh/chị có thể giải thích thêm được không?"}
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


                    //if (_isSearchAI == false)
                    //{
                    //    zlUserDb.IsHavePredicate = true;
                    //    zlUserDb.PredicateName = "Admin_Contact";
                    //    zlUserDb.PredicateValue = "";
                    //    zlUserDb.IsHaveCardCondition = false;
                    //    zlUserDb.CardConditionPattern = "";
                    //    _appZaloUser.Update(zlUserDb);
                    //    _appZaloUser.Save();

                    //    // Tin nhắn vắng mặt
                    //    if (_isHaveMessageAbsent)
                    //    {
                    //        if (HelperMethods.IsTimeInWorks() == false)
                    //        {
                    //            await SendMessageTask(ZaloTemplate.GetMessageTemplateTextAndQuickReply(_messageAbsent, "{{senderId}}", _patternCardPayloadProactive, _titleCardPayloadProactive).ToString(), sender);
                    //            return new HttpResponseMessage(HttpStatusCode.OK);
                    //        }
                    //    }

                    //    return new HttpResponseMessage(HttpStatusCode.OK);

                    //    //string notmatch = "Anh/chị vui lòng chọn Chat với chuyên viên để được tư vấn chi tiết hơn ạ";
                    //    //return await SendMessage(ZaloTemplate.GetMessageTemplateTextAndQuickReply(notmatch, sender, _contactAdmin, _titlePayloadContactAdmin));// not match
                    //}

                    //// Chuyển tới tìm kiếm Search NLP
                    //var systemConfigDb = _settingService.GetListSystemConfigByBotId(botId);
                    //var systemConfigVm = Mapper.Map<IEnumerable<BotProject.Model.Models.SystemConfig>, IEnumerable<SystemConfigViewModel>>(systemConfigDb);
                    //if (systemConfigVm.Count() == 0)
                    //{
                    //    return await SendMessage(ZaloTemplate.GetMessageTemplateText("Tìm kiếm xử lý ngôn ngữ tự nhiên hiện không hoạt động, bạn vui lòng thử lại sau nhé!", sender));// not match
                    //}
                    //string nameFunctionAPI = "";
                    //string number = "";
                    //string field = "";
                    //foreach (var item in systemConfigVm)
                    //{
                    //    if (item.Code == "UrlAPI")
                    //        nameFunctionAPI = item.ValueString;
                    //    if (item.Code == "ParamAreaID")
                    //        field = item.ValueString;
                    //    if (item.Code == "ParamNumberResponse")
                    //        number = item.ValueString;
                    //}
                    //hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_006;
                    //AddHistory(hisVm);
                    //string resultAPI = GetRelatedQuestionToZalo(nameFunctionAPI, text, field, number, botId.ToString());
                    //if (!String.IsNullOrEmpty(resultAPI))
                    //{
                    //    var lstQnaAPI = new JavaScriptSerializer
                    //    {
                    //        MaxJsonLength = Int32.MaxValue,
                    //        RecursionLimit = 100
                    //    }.Deserialize<List<SearchNlpQnAViewModel>>(resultAPI);
                    //    // render template json generic
                    //    int totalQnA = lstQnaAPI.Count();
                    //    string totalFind = "Tôi tìm thấy " + totalQnA + " câu hỏi liên quan đến câu hỏi của bạn";
                    //    await SendMessageTask(ZaloTemplate.GetMessageTemplateText(totalFind, sender).ToString(), sender);
                    //    string strTemplateGenericRelatedQuestion = ZaloTemplate.GetMessageTemplateGenericByList(sender, lstQnaAPI).ToString();
                    //    return await SendMessage(strTemplateGenericRelatedQuestion, sender);
                    //}
                    //else
                    //{
                    //    hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_008;
                    //    AddHistory(hisVm);
                    //    string strDefaultNotMatch = "Xin lỗi! Anh/chị có thể giải thích thêm được không";
                    //    foreach (var item in _dicNotMatch)
                    //    {
                    //        string itemNotMatch = item.Key;
                    //        if (itemNotMatch.Contains(result.Trim().Replace(".", String.Empty)))
                    //        {
                    //            strDefaultNotMatch = item.Value;
                    //        }
                    //    }
                    //    return await SendMessage(ZaloTemplate.GetMessageTemplateTextAndQuickReply(strDefaultNotMatch, sender, _contactAdmin, _titlePayloadContactAdmin));// not match
                    //}
                }
                // input là postback
                if (text.Contains("postback_card"))
                {
                    var cardDb = _cardService.GetSingleCondition(text.Replace(".", String.Empty));
                    if (cardDb.IsHaveCondition)
                    {
                        zlUserDb.IsHaveCardCondition = true;
                        zlUserDb.CardConditionPattern = text.Replace(".", String.Empty);
                    }
                    else if (cardDb.IsConditionWithAreaButton)
                    {
                        zlUserDb.IsConditionWithAreaButton = true;
                        zlUserDb.CardConditionAreaButtonPattern = text.Replace(".", String.Empty);
                    }
                    else
                    {
                        zlUserDb.IsHaveCardCondition = false;
                        zlUserDb.CardConditionPattern = "";
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

                            return new HttpResponseMessage(HttpStatusCode.OK);
                        }
                    }
                }

                //chat admin
                if (text.Contains(_contactAdmin))
                {
                    zlUserDb.IsHaveCardCondition = false;
                    zlUserDb.CardConditionPattern = "";
                    _appZaloUser.Update(zlUserDb);
                    _appZaloUser.Save();

                    string strTempPostbackContactAdmin = aimlBotResult.SubQueries[0].Template;
                    bool isPostbackContactAdmin = Regex.Match(strTempPostbackContactAdmin, "<template><srai>postback_card_(\\d+)</srai></template>").Success;
                    if (isPostbackContactAdmin)
                    {
                        strTempPostbackContactAdmin = Regex.Replace(strTempPostbackContactAdmin, @"<(.|\n)*?>", "").Trim();
                        var cardDb = _cardService.GetSingleCondition(strTempPostbackContactAdmin.Replace(".", String.Empty));
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
                    var cardDb = _cardService.GetSingleCondition(strTempPostback.Replace(".", String.Empty));
                    if (cardDb.IsHaveCondition)
                    {
                        zlUserDb.IsHaveCardCondition = true;
                        zlUserDb.CardConditionPattern = strTempPostback.Replace(".", String.Empty);
                    }
                    else if (cardDb.IsConditionWithAreaButton)
                    {
                        zlUserDb.IsConditionWithAreaButton = true;
                        zlUserDb.CardConditionAreaButtonPattern = text.Replace(".", String.Empty);
                    }
                    else
                    {
                        zlUserDb.IsHaveCardCondition = false;
                        zlUserDb.CardConditionPattern = "";
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

                            return new HttpResponseMessage(HttpStatusCode.OK);
                        }
                    }
                }

                //trường hợp trả về câu hỏi random chứa postpack
                bool isPostbackAnswer = Regex.Match(strTempPostback, "<template><srai>postback_answer_(\\d+)</srai></template>").Success;
                if (isPostbackAnswer)
                {
                    if (result.Contains("postback_card"))
                    {
                        var cardDb = _cardService.GetSingleCondition(result.Replace(".", String.Empty));
                        if (cardDb.IsHaveCondition)
                        {
                            zlUserDb.IsHaveCardCondition = true;
                            zlUserDb.CardConditionPattern = text.Replace(".", String.Empty);
                        }
                        else if (cardDb.IsConditionWithAreaButton)
                        {
                            zlUserDb.IsConditionWithAreaButton = true;
                            zlUserDb.CardConditionAreaButtonPattern = text.Replace(".", String.Empty);
                        }
                        else
                        {
                            zlUserDb.IsHaveCardCondition = false;
                            zlUserDb.CardConditionPattern = "";
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

                                return new HttpResponseMessage(HttpStatusCode.OK);
                            }
                        }
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
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                res = await client.PostAsync($"https://openapi.zalo.me/v2.0/oa/message?access_token=" + pageToken + "", new StringContent(json.ToString(), Encoding.UTF8, "application/json"));
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
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage res = await client.PostAsync($"https://openapi.zalo.me/v2.0/oa/message?access_token=" + pageToken + "", new StringContent(templateJson, Encoding.UTF8, "application/json"));
                }
            }
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
