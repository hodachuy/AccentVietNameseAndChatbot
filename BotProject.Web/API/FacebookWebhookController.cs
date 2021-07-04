using AIMLbot;
using AutoMapper;
using BotProject.Common;
using BotProject.Common.AppThird3PartyTemplate;
using BotProject.Common.DigiproService.Digipro;
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
//using SearchEngine.Service;
using System;
using System.Collections.Generic;
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
using static BotProject.Common.DigiproService.Digipro.DigiproService;

namespace BotProject.Web.API
{
    /// <summary>
    /// Webhook
    /// FACEBOOK DIGIPRO
    /// </summary>
    /// 
    public class FacebookWebhookController : ApiController
    {
        string pageToken = Helper.ReadString("AccessToken");
        string appSecret = Helper.ReadString("AppSecret");
        string verifytoken = Helper.ReadString("VerifyTokenWebHook");
        string _contactAdmin = Helper.ReadString("AdminContact");
        string _titlePayloadContactAdmin = Helper.ReadString("TitlePayloadAdminContact");
        int _timeOut = 60;
        bool _isHaveTimeOut = false;
        //tin nhắn phản hồi chờ
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

        private IApplicationFacebookUserService _appFacebookUser;
        private BotServiceDigipro _botService;
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

        public FacebookWebhookController(IErrorService errorService,
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
                              IApplicationFacebookUserService appFacebookUser,
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
            _botService = BotServiceDigipro.BotInstance;
            _moduleSearchEngineService = moduleSearchEngineService;
            _historyService = historyService;
            _cardService = cardService;
            _appFacebookUser = appFacebookUser;
            _app3rd = app3rd;
        }

        public HttpResponseMessage Get()
        {
            var querystrings = Request.GetQueryNameValuePairs().ToDictionary(x => x.Key, x => x.Value);
            if (querystrings["hub.verify_token"] == "lacviet_bot_chat")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(querystrings["hub.challenge"], Encoding.UTF8, "text/plain")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }

        [HttpPost]
        public async Task<HttpResponseMessage> Post()
        {
            int botId = 5028;

            var signature = Request.Headers.GetValues("X-Hub-Signature").FirstOrDefault().Replace("sha1=", "");
            var body = await Request.Content.ReadAsStringAsync();
            var value = JsonConvert.DeserializeObject<FacebookBotRequest>(body);
            //LogError(body);

            var app3rd = _app3rd.GetByPageId(value.entry[0].id);
            if (app3rd == null)
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            botId = app3rd.BotID;
            var settingDb = _settingService.GetSettingByBotID(botId);


            //get pagetoken
            pageToken = settingDb.FacebookPageToken;
            appSecret = settingDb.FacebookAppSecrect;
            _patternCardPayloadProactive = "postback_card_" + settingDb.CardID.ToString();

            //init stop word
            _stopWord = settingDb.StopWord;
            if (System.IO.File.Exists(_pathStopWord))
            {
                string[] stopWordDefault = System.IO.File.ReadAllLines(_pathStopWord);
                _stopWord += string.Join(",", stopWordDefault);
            }

            // xác nhận app facebook
            if (!VerifySignature(signature, body))
                return new HttpResponseMessage(HttpStatusCode.BadRequest);

            ////Test
            //var botRequest = new JavaScriptSerializer().Serialize(value);
            //LogError(botRequest);

            if (value.@object != "page")
                return new HttpResponseMessage(HttpStatusCode.OK);

            //BotLog.Info(body);

            _isHaveTimeOut = settingDb.IsProactiveMessageFacebook;
            _timeOut = settingDb.Timeout;
            _messageProactive = settingDb.ProactiveMessageText;
            _isSearchAI = settingDb.IsMDSearch;

            //tin vắng mặt
            _messageAbsent = settingDb.MessageMaintenance;
            _isHaveMessageAbsent = settingDb.IsHaveMaintenance;

            //OTP
            _timeOutOTP = settingDb.TimeoutOTP;
            _isHaveTimeOutOTP = settingDb.IsHaveTimeoutOTP;
            _messageOTP = settingDb.MessageTimeoutOTP;

            var lstAIML = _aimlFileService.GetActiveByBotId(botId);
            var lstAIMLVm = Mapper.Map<IEnumerable<AIMLFileViewModel>, IEnumerable<AIMLViewModel>>(lstAIML);
            _botService.loadAIMLFromDatabase(lstAIMLVm);
            string _userId = Guid.NewGuid().ToString();
            _user = _botService.loadUserBot(_userId);

            foreach (var item in value.entry[0].messaging)
            {
                //if (settingDb.IsHaveMaintenance)
                //{
                //    await SendMessageTask(FacebookTemplate.GetMessageTemplateText(settingDb.MessageMaintenance, "{{senderId}}").ToString(), item.sender.id);
                //    return new HttpResponseMessage(HttpStatusCode.OK);
                //}

                if (item.message == null && item.postback == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }

                if (item.message == null && item.postback != null)
                {
                    await ExcuteMessage(item.postback.payload, item.sender.id, botId);
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
                else
                {
                    if (item.message.quick_reply != null)
                    {
                        await ExcuteMessage(item.message.quick_reply.payload, item.sender.id, botId);
                        return new HttpResponseMessage(HttpStatusCode.OK);
                    }
                    else
                    {
                        await ExcuteMessage(item.message.text, item.sender.id, botId);
                        return new HttpResponseMessage(HttpStatusCode.OK);
                    }
                }
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        private async Task<HttpResponseMessage> ExcuteMessage(string text, string sender, int botId)
        {
            if (String.IsNullOrEmpty(text))
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            text = HttpUtility.HtmlDecode(text);
            text = Regex.Replace(text, @"<(.|\n)*?>", "").Trim();
            text = Regex.Replace(text, @"\p{Cs}", "").Trim();// remove emoji

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
                    return await SendMessage(FacebookTemplate.GetMessageTemplateTextAndQuickReply(strDefaultNotMatch, sender, _contactAdmin, _titlePayloadContactAdmin));// not match
                }
                //turn off AI
                strDefaultNotMatch = "Anh/chị vui lòng chọn Chat với chuyên viên để được tư vấn chi tiết hơn ạ";
                return await SendMessage(FacebookTemplate.GetMessageTemplateTextAndQuickReply(strDefaultNotMatch, sender, _contactAdmin, _titlePayloadContactAdmin));// not match
            }

            HistoryViewModel hisVm = new HistoryViewModel();
            hisVm.BotID = botId;
            hisVm.CreatedDate = DateTime.Now;
            hisVm.UserSay = text;
            hisVm.UserName = sender;
            hisVm.Type = CommonConstants.TYPE_FACEBOOK;


            DateTime dStartedTime = DateTime.Now;
            DateTime dTimeOut = DateTime.Now.AddSeconds(_timeOut);


            try
            {
                ApplicationFacebookUser fbUserDb = new ApplicationFacebookUser();
                fbUserDb = _appFacebookUser.GetByUserId(sender);

                // chat với admin
                if (fbUserDb != null)
                {
                    if (fbUserDb.PredicateName == "Admin_Contact")
                    {
                        var handleAdminContact = _handleMdService.HandleIsAdminContact(text, botId);
                        hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_004;
                        AddHistory(hisVm);
                        if (text.Contains("postback") || text.Contains(_contactAdmin))
                        {
                            fbUserDb.IsHavePredicate = false;
                            fbUserDb.PredicateName = "";
                            fbUserDb.PredicateValue = "";
                            fbUserDb.IsHaveCardCondition = false;
                            fbUserDb.CardConditionPattern = "";
                            _appFacebookUser.Update(fbUserDb);
                            _appFacebookUser.Save();
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
                            string[] strArrayJson = Regex.Split(handleAdminContact.TemplateJsonFacebook, "split");
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

                // TimeOut được bật
                if (_isHaveTimeOut)
                {
                    if (fbUserDb != null)
                    {
                        fbUserDb.StartedOn = dStartedTime;
                        fbUserDb.TimeOut = dTimeOut;
                        _appFacebookUser.Update(fbUserDb);
                        _appFacebookUser.Save();
                    }
                    await Schedule(sender, FacebookTemplate.GetMessageTemplateTextAndQuickReply(_messageProactive, "{{senderId}}", _patternCardPayloadProactive, _titleCardPayloadProactive).ToString(), pageToken, dTimeOut, text);
                    // Schedule(sender, FacebookTemplate.GetMessageTemplateText(_messageProactive, "{{senderId}}").ToString(), pageToken, dTimeOut);
                }
                if (fbUserDb == null)
                {
                    fbUserDb = new ApplicationFacebookUser();
                    ApplicationFacebookUserViewModel fbUserVm = new ApplicationFacebookUserViewModel();
                    fbUserVm.UserId = sender;
                    fbUserVm.IsHavePredicate = false;
                    fbUserVm.IsProactiveMessage = false;
                    fbUserVm.TimeOut = dTimeOut;
                    fbUserDb.CreatedDate = DateTime.Now;
                    fbUserDb.StartedOn = dStartedTime;
                    fbUserDb.BotID = botId;
                    fbUserDb.UpdateFacebookUser(fbUserVm);

                    ProfileUser profileUser = new ProfileUser();
                    profileUser = GetProfileUser(sender);

                    fbUserDb.AvatarPicture = profileUser.profile_pic;
                    fbUserDb.FirstName = profileUser.first_name;
                    fbUserDb.LastName = profileUser.last_name;
                    fbUserDb.UserName = fbUserDb.FirstName +" " + fbUserDb.LastName;

                    _appFacebookUser.Add(fbUserDb);
                    _appFacebookUser.Save();
                }
                else
                {
                    fbUserDb.StartedOn = dStartedTime;
                    fbUserDb.TimeOut = dTimeOut;
                    fbUserDb.BotID = botId;
                    // Nếu có yêu cầu click thẻ để đi theo luồng
                    if (fbUserDb.IsHaveCardCondition)
                    {
                        if (text.Contains("postback") || text.Contains(_contactAdmin))
                        {

                        }
                        else
                        {
                            var cardDb = _cardService.GetSingleCondition(fbUserDb.CardConditionPattern);
                            if (cardDb == null)
                            {
                                return new HttpResponseMessage(HttpStatusCode.OK);
                            }
                            string tempJsonFacebook = cardDb.TemplateJsonFacebook;
                            if (!String.IsNullOrEmpty(tempJsonFacebook))
                            {
                                tempJsonFacebook = tempJsonFacebook.Trim();
                                string[] strArrayJson = Regex.Split(tempJsonFacebook, "split");
                                if (strArrayJson.Length != 0)
                                {
                                    await SendMessageTask(FacebookTemplate.GetMessageTemplateText("Anh/chị vui lòng chọn lại thông tin bên dưới", sender).ToString(), sender);
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
                    if (fbUserDb.IsHavePredicate)
                    {
                        var predicateName = fbUserDb.PredicateName;
                        if (predicateName == "ApiSearch")
                        {
                            if (text.Contains("postback_card") || text.Contains(_contactAdmin))// nều còn điều kiện search mà chọn postback
                            {
                                fbUserDb.IsHavePredicate = false;
                                fbUserDb.PredicateName = "";
                                fbUserDb.PredicateValue = "";
                                fbUserDb.IsHaveCardCondition = false;
                                fbUserDb.CardConditionPattern = "";
                                _appFacebookUser.Update(fbUserDb);
                                _appFacebookUser.Save();
                                return await ExcuteMessage(text, sender, botId);
                            }

                            if (!String.IsNullOrEmpty(text))
                            {
                                // custom api digipro check text is service tag 
                                Regex rSvTagPattern = new Regex(@"^(?=.*[a-z])[a-zA-Z0-9]+$", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                                Match _mSvTagPattern = rSvTagPattern.Match(text);
                                if (_mSvTagPattern.Success == false)
                                {
                                    string numberPattern = @"^\d+$";
                                    bool isRofNumber = Regex.Match(text, numberPattern).Success;
                                    //check is isRofNumber
                                    if (isRofNumber == false)
                                    {
                                        fbUserDb.IsHavePredicate = false;
                                        fbUserDb.PredicateName = "";
                                        fbUserDb.PredicateValue = "";
                                        fbUserDb.IsHaveCardCondition = false;
                                        fbUserDb.CardConditionPattern = "";
                                        _appFacebookUser.Update(fbUserDb);
                                        _appFacebookUser.Save();

                                        return new HttpResponseMessage(HttpStatusCode.OK);
                                    }
                                }
                            }

                            string predicateValue = fbUserDb.PredicateValue;
                            var handleMdSearch = _handleMdService.HandleIsSearchAPI(text, predicateValue, "");

                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_005;
                            AddHistory(hisVm);

                            return await SendMessage(handleMdSearch.TemplateJsonFacebook, sender);

                        }
                        if (predicateName == "Age")
                        {
                            var handleAge = _handleMdService.HandledIsAge(text, botId);
                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_004;
                            AddHistory(hisVm);
                            if (handleAge.Status)// đúng age
                            {
                                fbUserDb.IsHavePredicate = false;
                                fbUserDb.PredicateName = "";
                                fbUserDb.PredicateValue = "";
                                fbUserDb.IsHaveCardCondition = false;
                                fbUserDb.CardConditionPattern = "";
                                fbUserDb.PhoneNumber = text;
                                _appFacebookUser.Update(fbUserDb);
                                _appFacebookUser.Save();
                                if (!String.IsNullOrEmpty(handleAge.Postback))
                                {
                                    return await ExcuteMessage(handleAge.Postback, sender, botId);
                                }
                            }
                            return await SendMessage(handleAge.TemplateJsonFacebook, sender);
                        }
                        if (predicateName == "Phone")
                        {
                            if (text.Contains("postback_card") || text.Contains(_contactAdmin))// nều còn điều kiện phone mà chọn postback
                            {
                                fbUserDb.IsHavePredicate = false;
                                fbUserDb.PredicateName = "";
                                fbUserDb.PredicateValue = "";
                                fbUserDb.IsHaveCardCondition = false;
                                fbUserDb.CardConditionPattern = "";
                                _appFacebookUser.Update(fbUserDb);
                                _appFacebookUser.Save();
                                return await ExcuteMessage(text, sender, botId);
                            }

                            var handlePhone = _handleMdService.HandleIsPhoneNumber(text, botId);

                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_004;
                            AddHistory(hisVm);
                            if (handlePhone.Status)// đúng số dt
                            {
                                ProfileUser profileUser = new ProfileUser();
                                profileUser = GetProfileUser(sender);
                                profileUser.phone_number = text;

                                ReturnCode rt = new ReturnCode();
                                rt = DigiproService.SendFbUserToService(sender,
                                                                   profileUser.first_name,
                                                                   profileUser.last_name,
                                                                   profileUser.profile_pic,
                                                                   profileUser.phone_number);
                                if(rt.Status == "BadRequest")
                                {
                                    return await SendMessage(FacebookTemplate.GetMessageTemplateTextAndQuickReply(rt.Message.Replace("\"",String.Empty), sender, "postback_card_6069", "Quay về").ToString(), sender);
                                }


                                fbUserDb.AvatarPicture = profileUser.profile_pic;
                                fbUserDb.FirstName = profileUser.first_name;
                                fbUserDb.LastName = profileUser.last_name;
                                fbUserDb.UserName = fbUserDb.FirstName + " " + fbUserDb.LastName;

                                fbUserDb.BotID = botId;
                                fbUserDb.IsHavePredicate = false;
                                fbUserDb.PredicateName = "";
                                fbUserDb.PredicateValue = "";
                                fbUserDb.IsHaveCardCondition = false;
                                fbUserDb.CardConditionPattern = "";
                                fbUserDb.PhoneNumber = text;
                                _appFacebookUser.Update(fbUserDb);
                                _appFacebookUser.Save();

                                // Nếu đúng số điện thoại sẽ trả về thẻ tiếp theo nếu có
                                if (!String.IsNullOrEmpty(handlePhone.Postback))
                                {
                                    return await ExcuteMessage(handlePhone.Postback, sender, botId);
                                }
                            }
                            return await SendMessage(handlePhone.TemplateJsonFacebook, sender);
                        }
                        if (predicateName == "Email")
                        {
                            var handleEmail = _handleMdService.HandledIsEmail(text, botId);
                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_004;
                            AddHistory(hisVm);
                            if (handleEmail.Status)// đúng email
                            {
                                fbUserDb.IsHavePredicate = false;
                                fbUserDb.PredicateName = "";
                                fbUserDb.PredicateValue = "";
                                fbUserDb.IsHaveCardCondition = false;
                                fbUserDb.CardConditionPattern = "";
                                _appFacebookUser.Update(fbUserDb);
                                _appFacebookUser.Save();

                                if (!String.IsNullOrEmpty(handleEmail.Postback))
                                {
                                    return await ExcuteMessage(handleEmail.Postback, sender, botId);
                                }
                            }
                            return await SendMessage(handleEmail.TemplateJsonFacebook, sender);
                        }
                        if (predicateName == "Engineer_Name")
                        {
                            var handleEngineerName = _handleMdService.HandleIsEngineerName(text, botId);
                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_004;
                            AddHistory(hisVm);
                            if (handleEngineerName.Status)
                            {
                                if (text.Contains("postback") || text.Contains(_contactAdmin))
                                {
                                    fbUserDb.EngineerName = "";
                                }

                                fbUserDb.IsHavePredicate = false;
                                fbUserDb.PredicateName = "";
                                fbUserDb.PredicateValue = "";
                                fbUserDb.IsHaveCardCondition = false;
                                fbUserDb.CardConditionPattern = "";
                                fbUserDb.EngineerName = text;

                                _appFacebookUser.Update(fbUserDb);
                                _appFacebookUser.Save();

                                if (!String.IsNullOrEmpty(handleEngineerName.Postback))
                                {
                                    return await ExcuteMessage(handleEngineerName.Postback, sender, botId);
                                }
                            }
                            return await SendMessage(handleEngineerName.TemplateJsonFacebook, sender);
                        }
                        if (predicateName == "Voucher")
                        {
                            string mdVoucherId = fbUserDb.PredicateValue;
                            if (text.Contains("postback_card") || text.Contains(_contactAdmin))
                            {
                                fbUserDb.IsHavePredicate = false;
                                fbUserDb.PredicateName = "";
                                fbUserDb.PredicateValue = "";
                                fbUserDb.IsHaveCardCondition = false;
                                fbUserDb.CardConditionPattern = "";
                                _appFacebookUser.Update(fbUserDb);
                                _appFacebookUser.Save();
                                return await ExcuteMessage(text, sender, botId);
                            }

                            var handleMdVoucher = _handleMdService.HandleIsVoucher(text, mdVoucherId, fbUserDb.EngineerName, fbUserDb.BranchOTP, hisVm.Type);

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

                                fbUserDb.IsHavePredicate = true;
                                fbUserDb.PredicateName = "IsVoucherOTP";
                                fbUserDb.PredicateValue = mdVoucherId;// voucherId
                                fbUserDb.PhoneNumber = telePhoneNumber;
                                fbUserDb.IsHaveCardCondition = false;
                                fbUserDb.CardConditionPattern = "";
                                _appFacebookUser.Update(fbUserDb);
                                _appFacebookUser.Save();


                                // send sms otp
                                await SendMessageTask(handleMdVoucher.TemplateJsonFacebook, sender);

                                // gọi timeout thời gian hủy otp
                                if (_isHaveTimeOutOTP)
                                {
                                    DateTime dTimeOutOTP = DateTime.Now.AddSeconds(_timeOutOTP);
                                    await ScheduleOTP(sender, telePhoneNumber, mdVoucherId, pageToken, dTimeOutOTP);
                                }

                                return new HttpResponseMessage(HttpStatusCode.OK);

                                //await SendMessageTask(handleMdVoucher.TemplateJsonFacebook, sender);
                                //return await SendMessage(FacebookTemplate.GetMessageTemplateText(("Mã OTP đang được gửi, Anh/Chị chờ tí nhé...").ToString(), sender));
                            }
                            return await SendMessage(handleMdVoucher.TemplateJsonFacebook, sender);
                        }
                        if (predicateName == "IsVoucherOTP")
                        {
                            string mdVoucherId = fbUserDb.PredicateValue;
                            string phoneNumber = fbUserDb.PhoneNumber;
                            // thẻ gửi lại sms
                            if (text.Equals("CARD_RESEND_SMS"))
                            {
                                var handleMdVoucher = _handleMdService.HandleIsVoucher(phoneNumber, mdVoucherId, fbUserDb.EngineerName, fbUserDb.BranchOTP, hisVm.Type);
                                if (handleMdVoucher.Status)
                                {
                                    // send sms otp
                                    await SendMessageTask(handleMdVoucher.TemplateJsonFacebook, sender);
                                    // gọi timeout thời gian hủy otp
                                    if (_isHaveTimeOutOTP)
                                    {
                                        DateTime dTimeOutOTP = DateTime.Now.AddSeconds(_timeOutOTP);
                                        await ScheduleOTP(sender, phoneNumber, mdVoucherId, pageToken, dTimeOutOTP);
                                    }
                                    return new HttpResponseMessage(HttpStatusCode.OK);
                                }
                                return await SendMessage(handleMdVoucher.TemplateJsonFacebook, sender);
                            }
                            if (text.Contains("postback_card") || text.Contains(_contactAdmin))
                            {
                                fbUserDb.IsHavePredicate = false;
                                fbUserDb.PredicateName = "";
                                fbUserDb.PredicateValue = "";
                                fbUserDb.IsHaveCardCondition = false;
                                fbUserDb.CardConditionPattern = "";
                                _appFacebookUser.Update(fbUserDb);
                                _appFacebookUser.Save();
                                return await ExcuteMessage(text, sender, botId);
                            }
                            var handleOTP = _handleMdService.HandleIsCheckOTP(text, phoneNumber, mdVoucherId, _messageOTP);
                            if (handleOTP.Status)
                            {
                                fbUserDb.IsHavePredicate = false;
                                fbUserDb.PredicateName = "";
                                fbUserDb.PredicateValue = "";
                                fbUserDb.IsHaveCardCondition = false;
                                fbUserDb.CardConditionPattern = "";
                                _appFacebookUser.Update(fbUserDb);
                                _appFacebookUser.Save();

                                // trả về image voucher + text message end
                                string[] arrMsgHandleOTP = Regex.Split(handleOTP.TemplateJsonFacebook, "split");
                                foreach (var itemMessageJson in arrMsgHandleOTP)
                                {
                                    await SendMessageTask(itemMessageJson, sender);
                                }
                                return new HttpResponseMessage(HttpStatusCode.OK);

                            }
                            return await SendMessage(handleOTP.TemplateJsonFacebook, sender);
                        }
                    }
                    else // Input: Khởi tạo module được chọn
                    {
                        if (text.Contains(CommonConstants.ModuleAdminContact))
                        {
                            var handleAdminContact = _handleMdService.HandleIsAdminContact(text, botId);

                            fbUserDb.IsHavePredicate = true;
                            fbUserDb.PredicateName = "Admin_Contact";
                            fbUserDb.PredicateValue = "";
                            fbUserDb.IsHaveCardCondition = false;
                            fbUserDb.CardConditionPattern = "";
                            _appFacebookUser.Update(fbUserDb);
                            _appFacebookUser.Save();

                            hisVm.UserSay = "[Chat với chuyên viên]";
                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_003;
                            AddHistory(hisVm);

                            // Tin nhắn vắng mặt
                            if (_isHaveMessageAbsent)
                            {
                                if (HelperMethods.IsTimeInWorks() == false)
                                {
                                    await SendMessageTask(FacebookTemplate.GetMessageTemplateTextAndQuickReply(_messageAbsent, "{{senderId}}", _patternCardPayloadProactive, _titleCardPayloadProactive).ToString(), sender);
                                    return new HttpResponseMessage(HttpStatusCode.OK);
                                }
                            }

                            string[] strArrayJson = Regex.Split(handleAdminContact.TemplateJsonFacebook, "split");//nhớ thêm bên formcard xử lý lục trên face
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
                            fbUserDb.IsHavePredicate = true;
                            fbUserDb.PredicateName = "ApiSearch";
                            fbUserDb.PredicateValue = mdSearchId;
                            fbUserDb.IsHaveCardCondition = false;
                            fbUserDb.CardConditionPattern = "";
                            _appFacebookUser.Update(fbUserDb);
                            _appFacebookUser.Save();

                            hisVm.UserSay = "[Tra cứu]";
                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_003;
                            AddHistory(hisVm);

                            return await SendMessage(handleMdSearch.TemplateJsonFacebook, sender);

                        }
                        if (text.Contains(CommonConstants.ModuleEngineerName))
                        {
                            var handleEngineerName = _handleMdService.HandleIsEngineerName(text, botId);

                            fbUserDb.IsHavePredicate = true;
                            fbUserDb.PredicateName = "Engineer_Name";
                            fbUserDb.EngineerName = "";
                            fbUserDb.IsHaveCardCondition = false;
                            fbUserDb.CardConditionPattern = "";
                            fbUserDb.BranchOTP = "";
                            _appFacebookUser.Update(fbUserDb);
                            _appFacebookUser.Save();

                            hisVm.UserSay = "[Tên hoặc mã kỹ sư]";
                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_003;
                            AddHistory(hisVm);

                            return await SendMessage(handleEngineerName.TemplateJsonFacebook, sender);
                        }

                        if (text.Contains(CommonConstants.ModuleAge))
                        {
                            var handleAge = _handleMdService.HandledIsAge(text, botId);

                            fbUserDb.IsHavePredicate = true;
                            fbUserDb.PredicateName = "Age";
                            fbUserDb.IsHaveCardCondition = false;
                            fbUserDb.CardConditionPattern = "";
                            _appFacebookUser.Update(fbUserDb);
                            _appFacebookUser.Save();

                            hisVm.UserSay = "[Tuổi]";
                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_003;
                            AddHistory(hisVm);

                            return await SendMessage(handleAge.TemplateJsonFacebook, sender);
                        }
                        if (text.Contains(CommonConstants.ModulePhone))
                        {
                            var handlePhone = _handleMdService.HandleIsPhoneNumber(text, botId);
                            fbUserDb.IsHavePredicate = true;
                            fbUserDb.PredicateName = "Phone";
                            fbUserDb.IsHaveCardCondition = false;
                            fbUserDb.CardConditionPattern = "";
                            _appFacebookUser.Update(fbUserDb);
                            _appFacebookUser.Save();

                            hisVm.UserSay = "[Số điện thoại]";
                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_003;
                            AddHistory(hisVm);

                            return await SendMessage(handlePhone.TemplateJsonFacebook, sender);
                        }
                        if (text.Contains(CommonConstants.ModuleEmail))
                        {
                            var handleEmail = _handleMdService.HandledIsEmail(text, botId);
                            fbUserDb.IsHavePredicate = true;
                            fbUserDb.PredicateName = "Email";
                            fbUserDb.IsHaveCardCondition = false;
                            fbUserDb.CardConditionPattern = "";
                            _appFacebookUser.Update(fbUserDb);
                            _appFacebookUser.Save();

                            hisVm.UserSay = "[Email]";
                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_003;
                            AddHistory(hisVm);

                            return await SendMessage(handleEmail.TemplateJsonFacebook, sender);
                        }

                        if (text.Contains(CommonConstants.ModuleVoucher))
                        {
                            string mdVoucherId = text.Replace(".", String.Empty).Replace("postback_module_voucher_", "");
                            var handleMdVoucher = _handleMdService.HandleIsVoucher(text, mdVoucherId, fbUserDb.EngineerName, fbUserDb.BranchOTP, hisVm.Type);

                            fbUserDb.IsHavePredicate = true;
                            fbUserDb.PredicateName = "Voucher";
                            fbUserDb.PredicateValue = mdVoucherId;
                            fbUserDb.IsHaveCardCondition = false;
                            fbUserDb.CardConditionPattern = "";
                            _appFacebookUser.Update(fbUserDb);
                            _appFacebookUser.Save();

                            hisVm.UserSay = "[Voucher]";
                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_003;
                            AddHistory(hisVm);

                            return await SendMessage(handleMdVoucher.TemplateJsonFacebook, sender);
                        }
                    }
                }

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
                        }
                    }
                }


                AIMLbot.Result aimlBotResult = _botService.Chat(text, _user);
                string result = aimlBotResult.OutputSentences[0].ToString();

                // lấy thông tin chi nhánh voucher map id từ tạo thẻ và thêm bên appsetingconfig
                if (text.Equals(Helper.ReadString("HCM")))
                {
                    fbUserDb.BranchOTP = "HCM";
                    _appFacebookUser.Update(fbUserDb);
                    _appFacebookUser.Save();
                }
                if (text.Equals(Helper.ReadString("HN")))
                {
                    fbUserDb.BranchOTP = "Hà Nội";
                    _appFacebookUser.Update(fbUserDb);
                    _appFacebookUser.Save();
                }
                if (text.Equals(Helper.ReadString("DN")))
                {
                    fbUserDb.BranchOTP = "Đà Nẵng";
                    _appFacebookUser.Update(fbUserDb);
                    _appFacebookUser.Save();
                }
                if (text.Equals(Helper.ReadString("CT")))
                {
                    fbUserDb.BranchOTP = "Cần Thơ";
                    _appFacebookUser.Update(fbUserDb);
                    _appFacebookUser.Save();
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
                                fbUserDb.PredicateName = "Age";
                                var handleAge = _handleMdService.HandledIsAge(contains, botId);
                                rsHandle = handleAge.TemplateJsonFacebook;
                            }
                            if (contains == "postback_module_email")
                            {
                                fbUserDb.PredicateName = "Email";
                                var handleEmail = _handleMdService.HandledIsEmail(contains, botId);
                                rsHandle = handleEmail.TemplateJsonFacebook;
                            }
                            if (contains == "postback_module_phone")
                            {
                                fbUserDb.PredicateName = "Phone";
                                var handlePhone = _handleMdService.HandleIsPhoneNumber(contains, botId);
                                rsHandle = handlePhone.TemplateJsonFacebook;
                            }
                            fbUserDb.IsHavePredicate = true;
                            fbUserDb.PredicateValue = "";
                            fbUserDb.IsHaveCardCondition = false;
                            fbUserDb.CardConditionPattern = "";
                            _appFacebookUser.Update(fbUserDb);
                            _appFacebookUser.Save();

                            return await SendMessage(rsHandle, sender);
                        }
                    }
                }
                if (result.Contains("NOT_MATCH"))
                {
                    hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_002;
                    AddHistory(hisVm);

                    fbUserDb.IsHaveCardCondition = false;
                    fbUserDb.CardConditionPattern = "";
                    _appFacebookUser.Update(fbUserDb);
                    _appFacebookUser.Save();

                    _dicNotMatch = new Dictionary<string, string>() {
                        {"NOT_MATCH_01", "Xin lỗi,em chưa hiểu ý anh/chị ạ!"},
                        {"NOT_MATCH_02", "Anh/chị có thể giải thích thêm được không?"},
                        {"NOT_MATCH_03", "Chưa hiểu lắm ạ, anh/chị có thể nói rõ hơn được không ạ?"},
                        {"NOT_MATCH_04", "Xin lỗi, anh/chị có thể giải thích thêm được không?"},
                        {"NOT_MATCH_05", "Xin lỗi, em chưa hiểu ạ"}
                    };

                    //turn off AI
                    if (_isSearchAI == false)
                    {
                        fbUserDb.IsHavePredicate = true;
                        fbUserDb.PredicateName = "Admin_Contact";
                        fbUserDb.PredicateValue = "";
                        fbUserDb.IsHaveCardCondition = false;
                        fbUserDb.CardConditionPattern = "";
                        _appFacebookUser.Update(fbUserDb);
                        _appFacebookUser.Save();

                        // Tin nhắn vắng mặt
                        if (_isHaveMessageAbsent)
                        {
                            if (HelperMethods.IsTimeInWorks() == false)
                            {
                                await SendMessageTask(FacebookTemplate.GetMessageTemplateTextAndQuickReply(_messageAbsent, "{{senderId}}", _patternCardPayloadProactive, _titleCardPayloadProactive).ToString(), sender);
                                return new HttpResponseMessage(HttpStatusCode.OK);
                            }
                        }

                        return new HttpResponseMessage(HttpStatusCode.OK);

                        //string notmatch = "Anh/chị vui lòng chọn Chat với chuyên viên để được tư vấn chi tiết hơn ạ";
                        //return await SendMessage(FacebookTemplate.GetMessageTemplateTextAndQuickReply(notmatch, sender, _contactAdmin, _titlePayloadContactAdmin));// not match
                    }

                    //Chuyển tới tìm kiếm Search NLP
                    var systemConfigDb = _settingService.GetListSystemConfigByBotId(botId);
                    var systemConfigVm = Mapper.Map<IEnumerable<BotProject.Model.Models.SystemConfig>, IEnumerable<SystemConfigViewModel>>(systemConfigDb);
                    if (systemConfigVm.Count() == 0)
                    {
                        return await SendMessage(FacebookTemplate.GetMessageTemplateText("Tìm kiếm xử lý ngôn ngữ tự nhiên hiện không hoạt động, bạn vui lòng thử lại sau nhé!", sender));// not match
                    }
                    string nameFunctionAPI = "";
                    string number = "";
                    string field = "";
                    foreach (var item in systemConfigVm)
                    {
                        if (item.Code == "UrlAPI")
                            nameFunctionAPI = item.ValueString;
                        if (item.Code == "ParamAreaID")
                            field = item.ValueString;
                        if (item.Code == "ParamNumberResponse")
                            number = item.ValueString;
                    }
                    hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_006;
                    AddHistory(hisVm);
                    string resultAPI = GetRelatedQuestionToFacebook(nameFunctionAPI, text, field, "5", botId.ToString());
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
                        await SendMessageTask(FacebookTemplate.GetMessageTemplateText(totalFind, sender).ToString(), sender);
                        string strTemplateGenericRelatedQuestion = FacebookTemplate.GetMessageTemplateGenericByList(sender, lstQnaAPI).ToString();
                        return await SendMessage(strTemplateGenericRelatedQuestion, sender);
                    }
                    else
                    {
                        hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_008;
                        AddHistory(hisVm);

                        string strDefaultNotMatch = "Xin lỗi! Anh/chị có thể giải thích thêm được không";
                        foreach (var item in _dicNotMatch)
                        {
                            string itemNotMatch = item.Key;
                            if (itemNotMatch.Contains(result.Trim().Replace(".", String.Empty)))
                            {
                                strDefaultNotMatch = item.Value;
                            }
                        }
                        return await SendMessage(FacebookTemplate.GetMessageTemplateTextAndQuickReply(strDefaultNotMatch, sender, _contactAdmin, _titlePayloadContactAdmin));// not match
                    }
                }
                // input là postback
                if (text.Contains("postback_card"))
                {
                    var cardDb = _cardService.GetSingleCondition(text.Replace(".", String.Empty));
                    if (cardDb.IsHaveCondition)
                    {
                        fbUserDb.IsHaveCardCondition = true;
                        fbUserDb.CardConditionPattern = text.Replace(".", String.Empty);
                        _appFacebookUser.Update(fbUserDb);
                        _appFacebookUser.Save();
                    }
                    else
                    {
                        fbUserDb.IsHaveCardCondition = false;
                        fbUserDb.CardConditionPattern = "";
                        _appFacebookUser.Update(fbUserDb);
                        _appFacebookUser.Save();
                    }
                    string tempJsonFacebook = cardDb.TemplateJsonFacebook;
                    if (!String.IsNullOrEmpty(tempJsonFacebook))
                    {
                        tempJsonFacebook = tempJsonFacebook.Trim();
                        string[] strArrayJson = Regex.Split(tempJsonFacebook, "split");//nhớ thêm bên formcard xử lý lục trên face
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

                if (text.Contains(_contactAdmin))//chat admin
                {
                    fbUserDb.IsHaveCardCondition = false;
                    fbUserDb.CardConditionPattern = "";
                    _appFacebookUser.Update(fbUserDb);
                    _appFacebookUser.Save();

                    string strTempPostbackContactAdmin = aimlBotResult.SubQueries[0].Template;
                    bool isPostbackContactAdmin = Regex.Match(strTempPostbackContactAdmin, "<template><srai>postback_card_(\\d+)</srai></template>").Success;
                    if (isPostbackContactAdmin)
                    {
                        strTempPostbackContactAdmin = Regex.Replace(strTempPostbackContactAdmin, @"<(.|\n)*?>", "").Trim();
                        var cardDb = _cardService.GetSingleCondition(strTempPostbackContactAdmin.Replace(".", String.Empty));
                        string tempJsonFacebook = cardDb.TemplateJsonFacebook;
                        if (!String.IsNullOrEmpty(tempJsonFacebook))
                        {
                            tempJsonFacebook = tempJsonFacebook.Trim();
                            string[] strArrayJson = Regex.Split(tempJsonFacebook, "split");//nhớ thêm bên formcard xử lý lục trên face
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
                        fbUserDb.IsHaveCardCondition = true;
                        fbUserDb.CardConditionPattern = strTempPostback.Replace(".", String.Empty);
                        _appFacebookUser.Update(fbUserDb);
                        _appFacebookUser.Save();
                    }
                    else
                    {
                        fbUserDb.IsHaveCardCondition = false;
                        fbUserDb.CardConditionPattern = "";
                        _appFacebookUser.Update(fbUserDb);
                        _appFacebookUser.Save();
                    }
                    string tempJsonFacebook = cardDb.TemplateJsonFacebook;
                    if (!String.IsNullOrEmpty(tempJsonFacebook))
                    {
                        tempJsonFacebook = tempJsonFacebook.Trim();
                        string[] strArrayJson = Regex.Split(tempJsonFacebook, "split");//nhớ thêm bên formcard xử lý lục trên face
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
                            fbUserDb.IsHaveCardCondition = true;
                            fbUserDb.CardConditionPattern = text.Replace(".", String.Empty);
                            _appFacebookUser.Update(fbUserDb);
                            _appFacebookUser.Save();
                        }
                        else
                        {
                            fbUserDb.IsHaveCardCondition = false;
                            fbUserDb.CardConditionPattern = "";
                            _appFacebookUser.Update(fbUserDb);
                            _appFacebookUser.Save();
                        }
                        string tempJsonFacebook = cardDb.TemplateJsonFacebook;
                        if (!String.IsNullOrEmpty(tempJsonFacebook))
                        {
                            tempJsonFacebook = tempJsonFacebook.Trim();
                            string[] strArrayJson = Regex.Split(tempJsonFacebook, "split");//nhớ thêm bên formcard xử lý lục trên face
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


                // trả lời text bình thường
                return await SendMessage(FacebookTemplate.GetMessageTemplateText(result, sender));

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
                res = await client.PostAsync($"https://graph.facebook.com/v3.2/me/messages?access_token=" + pageToken + "", new StringContent(json.ToString(), Encoding.UTF8, "application/json"));
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
                    res = await client.PostAsync($"https://graph.facebook.com/v3.2/me/messages?access_token=" + pageToken + "", new StringContent(templateJson, Encoding.UTF8, "application/json"));
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
                    HttpResponseMessage res = await client.PostAsync($"https://graph.facebook.com/v3.2/me/messages?access_token=" + pageToken + "", new StringContent(templateJson, Encoding.UTF8, "application/json"));
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

        #region Get User info
        private ProfileUser GetProfileUser(string senderId)
        {
            ProfileUser user = new ProfileUser();
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage res = new HttpResponseMessage();
                res = client.GetAsync($"https://graph.facebook.com/" + senderId + "?fields=first_name,last_name,profile_pic&access_token=" + pageToken).Result;//gender y/c khi sử dụng
                if (res.IsSuccessStatusCode)
                {
                    var serializer = new JavaScriptSerializer();
                    serializer.MaxJsonLength = Int32.MaxValue;
                    user = serializer.Deserialize<ProfileUser>(res.Content.ReadAsStringAsync().Result);
                }
                return user;
            }
        }

        public class ProfileUser
        {
            public string id { set; get; }
            public string first_name { set; get; }
            public string last_name { set; get; }
            public string profile_pic { set; get; }
            public string phone_number { set; get; }
            //public string gender { set; get; }
        }
        #endregion

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

                SqlCommand command = new SqlCommand("Select TimeOut from [ApplicationFacebookUsers] where UserId=@userId", sqlConnection);
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

                        SqlCommand command2 = new SqlCommand("UPDATE ApplicationFacebookUsers SET PredicateName = @predicateName, PredicateValue = @predicateValue, IsHavePredicate = @isHavePredicate,IsHaveCardCondition = @isHaveCardCondition,CardConditionPattern = @cardConditionPattern Where UserId=@userId", sqlConnection2);
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
                        res = await client.PostAsync($"https://graph.facebook.com/v3.2/me/messages?access_token=" + pageToken + "", new StringContent(templateJson, Encoding.UTF8, "application/json"));
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
