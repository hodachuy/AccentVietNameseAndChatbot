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
using BotProject.Web.SignalRChat;
using ExcelDataReader;
using Microsoft.Owin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace BotProject.Web.Controllers
{
	[AllowCrossSite]
	public class Apiv1Controller : Controller
	{
		private Dictionary<string, string> _dicAttributeUser = new Dictionary<string, string>();

		private readonly string Domain = Helper.ReadString("Domain");

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
		private string CARD_GET_STARTED = "danh mục";

		private string TITLE_PAYLOAD_QUICKREPLY = "";

		private string _contactAdmin = Helper.ReadString("AdminContact");
		private string _titlePayloadContactAdmin = Helper.ReadString("TitlePayloadAdminContact");

		private readonly string UrlAPI = Helper.ReadString("UrlAPI");
		private readonly string KeyAPI = Helper.ReadString("KeyAPI");

        private AIMLBotService _aimlBotService;

        //private BotService _botService;

        private AIMLbot.Bot _botService;

        //private ElasticSearch _elastic;
        private IBotService _botDbService;
		private ISettingService _settingService;
		private IHandleModuleServiceService _handleMdService;
		private IModuleService _mdService;
		private IModuleKnowledegeService _mdKnowledgeService;
		private IMdSearchService _mdSearchService;
		private IMdVoucherService _mdVoucherService;
		private IErrorService _errorService;
		private IAIMLFileService _aimlFileService;
		private IQnAService _qnaService;
		private ApiQnaNLRService _apiNLR;
		private IModuleSearchEngineService _moduleSearchEngineService;
		private IHistoryService _historyService;
		private ICardService _cardService;
		private IApplicationPlatformUserService _appPlatformUser;
		//private Bot _bot;
		private User _user;
		private IAttributeSystemService _attributeService;

		public bool isMdSearch = false;
		public bool isHaveSymptomAndMsgNotMatch = false;
		// Model user
		ApplicationPlatformUser _appUser;
		public Apiv1Controller(IErrorService errorService,
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
								IMdVoucherService mdVoucherService,
								IApplicationPlatformUserService appPlatformUser,
								IAttributeSystemService attributeService)
		{
			_errorService = errorService;
			//_elastic = new ElasticSearch();
			//_accentService = new AccentService();// AccentService.AccentInstance;
			_botDbService = botDbService;
			_settingService = settingService;
			_handleMdService = handleMdService;
			_mdService = mdService;
			_apiNLR = new ApiQnaNLRService();
			_mdKnowledgeService = mdKnowledgeService;
			_mdSearchService = mdSearchService;
			_aimlFileService = aimlFileService;
			_qnaService = qnaService;
			_moduleSearchEngineService = moduleSearchEngineService;
			_historyService = historyService;
			_cardService = cardService;
			_mdVoucherService = mdVoucherService;
			_attributeService = attributeService;
			_appPlatformUser = appPlatformUser;

            //_botService = BotService.BotInstance;
            _aimlBotService = AIMLBotService.AIMLBotInstance;
        }

		// GET: Apiv1
		public ActionResult Index()
		{
			return View();
		}

        public ActionResult FormChat(string token, string botId)
		{
			int botID = Int32.Parse(botId);

            //var lstAIML = _aimlFileService.GetByBotId(botID);
            // _botService.loadAIMLFile(lstAIML, botID.ToString());

            //string pathFileBinary = ConfigurationManager.AppSettings["AIML2GraphmasterPath"] + "BotID_" + botId + ".bin";
            //_botService.loadGraphmaster2AIMLFile(pathFileBinary);

            //_botService = new AIMLbot.Bot();

            _botService = _aimlBotService.GetServerBot(botId);

            string pathAIML2Graphmaster = ConfigurationManager.AppSettings["AIML2GraphmasterPath"] + "BotID_" + botId + ".bin";
            _aimlBotService.LoadGraphmasterFromAIMLBinaryFile(pathAIML2Graphmaster, _botService);

            var settingDb = _settingService.GetSettingByBotID(botID);
            var settingVm = Mapper.Map<BotProject.Model.Models.Setting, BotSettingViewModel>(settingDb);
            var systemConfig = _settingService.GetListSystemConfigByBotId(botID);
            var systemConfigVm = Mapper.Map<IEnumerable<BotProject.Model.Models.SystemConfig>, IEnumerable<SystemConfigViewModel>>(systemConfig);

            UserBotViewModel userBot = new UserBotViewModel();
			userBot.StopWord = settingVm.StopWord;
			userBot.SystemConfigViewModel = systemConfigVm;

			// load file stopword default
			//string pathStopWord = System.IO.Path.Combine(PathServer.PathNLR, "StopWord.txt");
			//if (System.IO.File.Exists(pathStopWord))
			//{
			//    string[] stopWordDefault = System.IO.File.ReadAllLines(pathStopWord);
			//    userBot.StopWord += string.Join(",", stopWordDefault);
			//}

			userBot.ID = Guid.NewGuid().ToString();
			userBot.BotID = botId;
			_user = _aimlBotService.loadUserBot(userBot.ID, _botService);

			_user.Predicates.addSetting("phone", "");
			_user.Predicates.addSetting("phonecheck", "false");

			_user.Predicates.addSetting("email", "");
			_user.Predicates.addSetting("emailcheck", "false");

			_user.Predicates.addSetting("age", "");
			_user.Predicates.addSetting("agecheck", "false");

			_user.Predicates.addSetting("name", "");
			_user.Predicates.addSetting("namecheck", "false");

			_user.Predicates.addSetting("cardConditionCheck", "false");
			_user.Predicates.addSetting("cardConditionPattern", "");

			_user.Predicates.addSetting("IsHaveSetAttributeSystem", "false");
			_user.Predicates.addSetting("AttributeName", "");

			_user.Predicates.addSetting("IsConditionWithInputText", "false");
			_user.Predicates.addSetting("CardConditionWithInputTextPattern", "");

			_user.Predicates.addSetting("isMdSearch", settingVm.IsMDSearch.ToString());

			// load tất cả module của bot và thêm key vao predicate
			var mdBotDb = _mdService.GetAllModuleByBotID(botID).Where(x => x.Name != "med_get_info_patient").ToList();
			if (mdBotDb.Count() != 0)
			{
				foreach (var item in mdBotDb)
				{
					_user.Predicates.addSetting(item.Name, "");
					_user.Predicates.addSetting(item.Name + "check", "false");
				}
			}
			var lstMdGetInfoPatientDb = _mdKnowledgeService.GetAllMdKnowledgeMedInfPatientByBotID(botID).ToList();
			if (lstMdGetInfoPatientDb.Count() != 0)
			{
				foreach (var item in lstMdGetInfoPatientDb)
				{
					if (!String.IsNullOrEmpty(item.OptionText))
					{
						int index = 0;
						var arrOpt = item.OptionText.Split(new string[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
						foreach (var opt in arrOpt)
						{
							index++;
							_user.Predicates.addSetting(item.ID + "_opt_" + index, "");
						}
					}
					if (!String.IsNullOrEmpty(item.Payload))
					{
						_user.Predicates.addSetting("med_get_info_patient_" + item.ID, "");
						_user.Predicates.addSetting("med_get_info_patient_check_" + item.ID, "false");
					}
				}
			}
			_user.Predicates.addSetting("isChkMdGetInfoPatient", "false");
			_user.Predicates.addSetting("ThreadMdGetInfoPatientId", "");

			_user.Predicates.addSetting("isChkMdSearch", "false");
			_user.Predicates.addSetting("ThreadMdSearchID", "");

			_user.Predicates.addSetting("isChkOTP", "false");
			_user.Predicates.addSetting("isChkMdVoucher", "false");
			_user.Predicates.addSetting("ThreadMdVoucherID", "");

			// text
			_user.Predicates.addSetting("content_message", "");

			var lstAttribute = _attributeService.GetListAttributeSystemByBotId(botID).ToList();
			if (lstAttribute.Count() != 0)
			{
				foreach (var attr in lstAttribute)
				{
					_user.Predicates.addSetting(attr.Name, "");
				}
			}

			SettingsDictionaryViewModel settingDic = new SettingsDictionaryViewModel();
			settingDic.Count = _user.Predicates.Count;
			settingDic.orderedKeys = _user.Predicates.orderedKeys;
			settingDic.settingsHash = _user.Predicates.settingsHash;
			settingDic.SettingNames = _user.Predicates.SettingNames;
			userBot.SettingDicstionary = settingDic;
			Session[CommonConstants.SessionUserBot] = userBot;

			Session[CommonConstants.SessionResultBot] = null;
			return View(settingVm);
		}

        public JsonResult chatbot(string text, string token, string botId, string titlePostback = "")
        {
            //string nameBotAIML = "User_" + token + "_BotID_" + botId;
            //string fullPathAIML = pathAIML + nameBotAIML;
            //_botService.loadAIMLFromFiles(fullPathAIML);

            _botService = _aimlBotService.GetServerBot(botId);

            string txtOriginal = "";
            List<SearchSymptomViewModel> _lstSymptomVm = new List<SearchSymptomViewModel>();
            List<SearchNlpQnAViewModel> _lstQnAVm = new List<SearchNlpQnAViewModel>();
            isHaveSymptomAndMsgNotMatch = false;
            int valBotID = Int32.Parse(botId);
            try
            {
                if (!String.IsNullOrEmpty(text))
                {
                    text = Regex.Replace(text, @"<(.|\n)*?>", "").Trim();
                    text = Regex.Replace(text, @"\p{Cs}", "").Trim();// remove emoji
                    txtOriginal = text;
                }
                if (Session[CommonConstants.SessionUserBot] == null)
                {
                    return Json(new
                    {
                        message = new List<string>() { "Session Timeout" },
                        postback = new List<string>() { null },
                        messageLstSearchNLP = _lstQnAVm,
                        messageLstSymptoms = _lstSymptomVm,
                        isSearchNLP = isMdSearch
                    }, JsonRequestBehavior.AllowGet);
                }

                //get new predicate from session user bot request
                var userBot = (UserBotViewModel)Session[CommonConstants.SessionUserBot];
                _user = _aimlBotService.loadUserBot(userBot.ID,_botService);
                _user.Predicates.Count = userBot.SettingDicstionary.Count;
                _user.Predicates.SettingNames = userBot.SettingDicstionary.SettingNames;
                _user.Predicates.orderedKeys = userBot.SettingDicstionary.orderedKeys;
                _user.Predicates.settingsHash = userBot.SettingDicstionary.settingsHash;
                isMdSearch = bool.Parse(_user.Predicates.settingsHash["ISMDSEARCH"].ToLower());


                if (!String.IsNullOrEmpty(userBot.StopWord))
                {
                    string[] arrStopWord = userBot.StopWord.Split(',');
                    if (arrStopWord.Length != 0)
                    {
                        foreach (var w in arrStopWord)
                        {
                            text = Regex.Replace(text, "\\b" + Regex.Escape(w) + "\\b", String.Empty).Trim();
                        }
                    }
                }

                //List<string> lstSymptomp = new List<string>();

                if (text.Contains("postback") == false)
                {
                    _user.Predicates.addSetting("content_message", text);
                }

                HistoryViewModel hisVm = new HistoryViewModel();
                hisVm.BotID = Int32.Parse(botId);
                hisVm.CreatedDate = DateTime.Now;
                hisVm.UserSay = txtOriginal;
                hisVm.UserName = userBot.ID;

                if (bool.Parse(_user.Predicates.grabSetting("cardConditionCheck")))
                {
                    if (text.Contains("postback") == false)
                    {
                        string cardPayload = _user.Predicates.grabSetting("cardConditionPattern");
                        List<string> lstConditionClick = new List<string>();
                        string tplIsConditionClick = tempText("Anh/chị vui lòng chọn lại thông tin bên dưới");
                        lstConditionClick.Add(tplIsConditionClick);
                        Session[CommonConstants.SessionResultBot] = lstConditionClick;
                        return chatbot(cardPayload, token, botId);
                    }
                }

                if (bool.Parse(_user.Predicates.grabSetting("IsHaveSetAttributeSystem")))
                {
                    if (text.Contains("postback") == false)
                    {
                        _user.Predicates.addSetting(_user.Predicates.grabSetting("AttributeName"), text);
                    }
                    else
                    {
                        // lấy ký tự postback
                        _user.Predicates.addSetting(_user.Predicates.grabSetting("AttributeName"), titlePostback);
                    }
                }

                if (bool.Parse(_user.Predicates.grabSetting("IsConditionWithInputText")))
                {
                    if (text.Contains("postback") == false)
                    {
                        _user.Predicates.addSetting("IsConditionWithInputText", "false");
                        _user.Predicates.addSetting("IsHaveSetAttributeSystem", "false");
                        return chatbot(_user.Predicates.grabSetting("CardConditionWithInputTextPattern"), token, botId);
                    }
                }

                // Module lấy thông tin bệnh
                #region Module lấy thông tin bệnh
                // nếu là true
                if (bool.Parse(_user.Predicates.grabSetting("isChkMdGetInfoPatient")))
                {
                    if (text.Contains("module_patient"))
                    {
                        string MdGetInfoPatientId = _user.Predicates.grabSetting("ThreadMdGetInfoPatientId");
                        _user.Predicates.addSetting("med_get_info_patient_check_" + MdGetInfoPatientId, "false");
                        _user.Predicates.addSetting("isChkMdGetInfoPatient", "false");

                        _user.Predicates.addSetting("ThreadMdGetInfoPatientId", "");

                        //hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_004;
                        //AddHistory(hisVm);

                        return chatbot(text, token, botId);
                    }
                    else
                    {
                        string MdGetInfoPatientId = _user.Predicates.grabSetting("ThreadMdGetInfoPatientId");
                        var handlePatient = _handleMdService.HandleIsModuleKnowledgeInfoPatient("postback_module_med_get_info_patient_" + MdGetInfoPatientId + "", valBotID, "Tôi không hiểu, vui lòng chọn lại thông tin bên dưới.");

                        hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_004;
                        AddHistory(hisVm);
                        return Json(new
                        {
                            message = new List<string>() { handlePatient.Message },
                            postback = new List<string>() { null },
                            messageLstSearchNLP = _lstQnAVm,
                            messageLstSymptoms = _lstSymptomVm,
                            isSearchNLP = isMdSearch
                        }, JsonRequestBehavior.AllowGet);
                    }
                }
                if (text.Contains("postback_module_med_get_info_patient"))
                {
                    var handlePatient = _handleMdService.HandleIsModuleKnowledgeInfoPatient(text, valBotID, "");

                    string MdGetInfoPatientId = text.Replace("postback_module_med_get_info_patient_", "");
                    _user.Predicates.addSetting("ThreadMdGetInfoPatientId", MdGetInfoPatientId);

                    _user.Predicates.addSetting("med_get_info_patient_check_" + MdGetInfoPatientId, "true");

                    _user.Predicates.addSetting("isChkMdGetInfoPatient", "true");

                    hisVm.UserSay = "[Lấy thông tin]";
                    hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_003;
                    AddHistory(hisVm);

                    return Json(new
                    {
                        message = new List<string>() { handlePatient.Message },
                        postback = new List<string>() { null },
                        messageLstSearchNLP = _lstQnAVm,
                        messageLstSymptoms = _lstSymptomVm,
                        isSearchNLP = isMdSearch
                    }, JsonRequestBehavior.AllowGet);
                }
                #endregion

                // Module tìm kiếm 
                #region Module Tìm kiếm với api
                if (bool.Parse(_user.Predicates.grabSetting("isChkMdSearch")))
                {
                    if (text.Contains("postback_card"))
                    {
                        _user.Predicates.addSetting("isChkMdSearch", "false");
                        _user.Predicates.addSetting("ThreadMdSearchID", "");
                        return chatbot(text, token, botId);
                    }
                    string mdSearchId = _user.Predicates.grabSetting("ThreadMdSearchID");
                    var handleMdSearch = _handleMdService.HandleIsSearchAPI(text, mdSearchId, "");
                    bool chkAPI = !String.IsNullOrEmpty(handleMdSearch.ResultAPI) == true ? false : true;

                    hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_005;
                    AddHistory(hisVm);

                    return Json(new
                    {
                        message = new List<string>() { handleMdSearch.Message },
                        postback = new List<string>() { handleMdSearch.Postback },
                        messageLstSearchNLP = _lstQnAVm,
                        messageLstSymptoms = _lstSymptomVm,
                        isCheck = chkAPI
                    }, JsonRequestBehavior.AllowGet);
                }

                if (text.Contains("postback_module_api_search"))
                {
                    string mdSearchId = text.Replace(".", String.Empty).Replace("postback_module_api_search_", "");
                    var handleMdSearch = _handleMdService.HandleIsSearchAPI(text, mdSearchId, "");
                    _user.Predicates.addSetting("ThreadMdSearchID", mdSearchId);
                    //_user.Predicates.addSetting("api_search_check_" + mdSearchId, "true");
                    _user.Predicates.addSetting("isChkMdSearch", "true");

                    hisVm.UserSay = "[Tra cứu]";
                    hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_003;
                    AddHistory(hisVm);

                    return Json(new
                    {
                        message = new List<string>() { handleMdSearch.Message },
                        postback = new List<string>() { handleMdSearch.Postback },
                        messageLstSearchNLP = _lstQnAVm,
                        messageLstSymptoms = _lstSymptomVm,
                        isSearchNLP = isMdSearch
                    }, JsonRequestBehavior.AllowGet);
                }
                #endregion

                // Xử lý module phone
                #region 
                bool isCheckPhone = bool.Parse(_user.Predicates.grabSetting("phonecheck"));
                if (isCheckPhone)
                {
                    var handlePhone = _handleMdService.HandleIsPhoneNumber(text, valBotID);

                    hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_004;
                    AddHistory(hisVm);
                    if (handlePhone.Status)// đúng số dt
                    {
                        _user.Predicates.addSetting("phonecheck", "false");
                        _user.Predicates.addSetting("phone", text);
                        if (!String.IsNullOrEmpty(handlePhone.Postback))
                        {
                            return chatbot(handlePhone.Postback, token, botId);
                        }
                    }
                    return Json(new
                    {
                        message = new List<string>() { handlePhone.Message },
                        postback = new List<string>() { null },
                        messageLstSearchNLP = _lstQnAVm,
                        messageLstSymptoms = _lstSymptomVm,
                        isSearchNLP = isMdSearch
                    }, JsonRequestBehavior.AllowGet);
                }
                if (text.Contains("postback_module_phone"))
                {
                    string numberPhone = _user.Predicates.grabSetting("phone");
                    _user.Predicates.addSetting("phonecheck", "true");
                    var handlePhone = _handleMdService.HandleIsPhoneNumber(text, valBotID);

                    hisVm.UserSay = "[Số điện thoại]";
                    hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_003;
                    AddHistory(hisVm);
                    if (!String.IsNullOrEmpty(numberPhone))// hiển thị số điện thoại nếu đã cung cấp trước đó
                    {
                        string sbPostback = tempNodeBtnModule(numberPhone);

                        return Json(new
                        {
                            message = new List<string>() { handlePhone.Message },
                            postback = new List<string>() { sbPostback.ToString() },
                            messageLstSearchNLP = _lstQnAVm,
                            messageLstSymptoms = _lstSymptomVm,
                            isSearchNLP = isMdSearch
                        }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new
                    {
                        message = new List<string>() { handlePhone.Message },
                        postback = new List<string>() { null },
                        messageLstSearchNLP = _lstQnAVm,
                        messageLstSymptoms = _lstSymptomVm,
                        isSearchNLP = isMdSearch
                    }, JsonRequestBehavior.AllowGet);
                }
                #endregion

                // Xử lý email
                #region
                bool isCheckMail = bool.Parse(_user.Predicates.grabSetting("emailcheck"));
                if (isCheckMail)
                {
                    var handleEmail = _handleMdService.HandledIsEmail(text, valBotID);
                    hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_004;
                    AddHistory(hisVm);
                    if (handleEmail.Status)// đúng email
                    {
                        _user.Predicates.addSetting("emailcheck", "false");
                        _user.Predicates.addSetting("email", text);
                        if (!String.IsNullOrEmpty(handleEmail.Postback))
                        {
                            return chatbot(handleEmail.Postback, token, botId);
                        }
                    }
                    return Json(new
                    {
                        message = new List<string>() { handleEmail.Message },
                        postback = new List<string>() { null },
                        messageLstSearchNLP = _lstQnAVm,
                        messageLstSymptoms = _lstSymptomVm,
                        isSearchNLP = isMdSearch
                    }, JsonRequestBehavior.AllowGet);
                }
                if (text.Contains("postback_module_email"))
                {
                    string email = _user.Predicates.grabSetting("email");
                    _user.Predicates.addSetting("emailcheck", "true");
                    var handleEmail = _handleMdService.HandledIsEmail(text, valBotID);
                    hisVm.UserSay = "[Email]";
                    hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_003;
                    AddHistory(hisVm);
                    if (!String.IsNullOrEmpty(email))// hiển thị email đã cung cấp trước đó
                    {
                        string sbPostback = tempNodeBtnModule(email);
                        return Json(new
                        {
                            message = new List<string>() { handleEmail.Message },
                            postback = new List<string>() { sbPostback.ToString() },
                            messageLstSearchNLP = _lstQnAVm,
                            messageLstSymptoms = _lstSymptomVm,
                            isSearchNLP = isMdSearch
                        }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new
                    {
                        message = new List<string>() { handleEmail.Message },
                        postback = new List<string>() { null },
                        messageLstSearchNLP = _lstQnAVm,
                        messageLstSymptoms = _lstSymptomVm,
                        isSearchNLP = isMdSearch
                    }, JsonRequestBehavior.AllowGet);
                }
                #endregion

                // Xử lý age
                #region
                bool isCheckAge = bool.Parse(_user.Predicates.grabSetting("agecheck"));
                if (isCheckAge)
                {
                    var handleAge = _handleMdService.HandledIsAge(text, valBotID);
                    hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_004;
                    AddHistory(hisVm);
                    if (handleAge.Status)// đúng age
                    {
                        _user.Predicates.addSetting("agecheck", "false");
                        _user.Predicates.addSetting("age", text);
                        if (!String.IsNullOrEmpty(handleAge.Postback))
                        {
                            return chatbot(handleAge.Postback, token, botId);
                        }
                    }
                    return Json(new
                    {
                        message = new List<string>() { handleAge.Message },
                        postback = new List<string>() { null },
                        messageLstSearchNLP = _lstQnAVm,
                        messageLstSymptoms = _lstSymptomVm,
                        isSearchNLP = isMdSearch
                    }, JsonRequestBehavior.AllowGet);
                }
                if (text.Contains("postback_module_age"))// nếu check đi từ đây trước
                {
                    string age = _user.Predicates.grabSetting("age");
                    _user.Predicates.addSetting("agecheck", "true");
                    var handleAge = _handleMdService.HandledIsAge(text, valBotID);

                    hisVm.UserSay = "[Tuổi]";
                    hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_003;
                    AddHistory(hisVm);
                    if (!String.IsNullOrEmpty(age))// hiển thị age đã cung cấp trước đó
                    {
                        string sbPostback = tempNodeBtnModule(age);
                        return Json(new
                        {
                            message = new List<string>() { handleAge.Message },
                            postback = new List<string>() { sbPostback.ToString() },
                            messageLstSearchNLP = "",
                            isSearchNLP = isMdSearch
                        }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new
                    {
                        message = new List<string>() { handleAge.Message },
                        postback = new List<string>() { null },
                        messageLstSearchNLP = _lstQnAVm,
                        messageLstSymptoms = _lstSymptomVm,
                        isSearchNLP = isMdSearch
                    }, JsonRequestBehavior.AllowGet);
                }
                #endregion

                // Xử lý Voucher
                #region Module Voucher
                if (bool.Parse(_user.Predicates.grabSetting("isChkMdVoucher")))
                {
                    string mdVoucherId = _user.Predicates.grabSetting("ThreadMdVoucherID");
                    if (text.Contains("postback_card"))
                    {
                        _user.Predicates.addSetting("isChkMdVoucher", "false");
                        _user.Predicates.addSetting("ThreadMdVoucherID", "");
                        return chatbot(text, token, botId);
                    }

                    var handleMdVoucher = _handleMdService.HandleIsVoucher(text, mdVoucherId, "", "", "");

                    hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_007;
                    AddHistory(hisVm);
                    if (handleMdVoucher.Status)
                    {
                        string telePhoneNumber = text;
                        string[] strArrSpecial = new string[] { "+", "-", " ", ",", ":" };
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

                        _user.Predicates.addSetting("phone", telePhoneNumber);
                        _user.Predicates.addSetting("isChkOTP", "true");
                        _user.Predicates.addSetting("isChkMdVoucher", "false");
                    }
                    return Json(new
                    {
                        message = new List<string>() { handleMdVoucher.Message },
                        postback = new List<string>() { handleMdVoucher.Postback },
                        messageLstSearchNLP = _lstQnAVm,
                        messageLstSymptoms = _lstSymptomVm,
                        isSearchNLP = isMdSearch
                    }, JsonRequestBehavior.AllowGet);
                }

                if (text.Contains("postback_module_voucher"))
                {
                    string mdVoucherId = text.Replace(".", String.Empty).Replace("postback_module_voucher_", "");
                    var handleMdVoucher = _handleMdService.HandleIsVoucher(text, mdVoucherId, "", "", "");
                    _user.Predicates.addSetting("ThreadMdVoucherID", mdVoucherId);
                    _user.Predicates.addSetting("isChkMdVoucher", "true");

                    hisVm.UserSay = "[Voucher]";
                    hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_003;
                    AddHistory(hisVm);

                    return Json(new
                    {

                        message = new List<string>() { handleMdVoucher.Message },
                        postback = new List<string>() { handleMdVoucher.Postback },
                        messageLstSearchNLP = _lstQnAVm,
                        messageLstSymptoms = _lstSymptomVm,
                        isSearchNLP = isMdSearch
                    }, JsonRequestBehavior.AllowGet);
                }
                #endregion

                // Xử lý OTP từ Voucher
                if (bool.Parse(_user.Predicates.grabSetting("isChkOTP")))
                {
                    string mdVoucherId = _user.Predicates.grabSetting("ThreadMdVoucherID");
                    string numberPhone = _user.Predicates.grabSetting("phone");
                    if (text.Contains("postback_card"))
                    {
                        _user.Predicates.addSetting("isChkOTP", "false");
                        _user.Predicates.addSetting("isChkMdVoucher", "false");
                        _user.Predicates.addSetting("ThreadMdVoucherID", "");
                        return chatbot(text, token, botId);
                    }
                    var handleOTP = _handleMdService.HandleIsCheckOTP(text, numberPhone, mdVoucherId);
                    if (handleOTP.Status)
                    {
                        _user.Predicates.addSetting("isChkOTP", "false");
                        _user.Predicates.addSetting("isChkMdVoucher", "false");
                        _user.Predicates.addSetting("ThreadMdVoucherID", "");
                    }
                    return Json(new
                    {
                        message = new List<string>() { handleOTP.Message },
                        postback = new List<string>() { handleOTP.Postback },
                        messageLstSearchNLP = _lstQnAVm,
                        messageLstSymptoms = _lstSymptomVm,
                        isSearchNLP = isMdSearch
                    }, JsonRequestBehavior.AllowGet);
                }

                // Lấy target from knowledge base QnA trained mongodb
                //if (text.Contains("postback") == false || text.Contains("module") == false)
                //{
                //    string target = _apiNLR.GetPrecidictTextClass(text, valBotID);
                //    if (!String.IsNullOrEmpty(target))
                //    {
                //        target = Regex.Replace(target, "\n", "").Replace("\"", "");
                //        QuesTargetViewModel quesTarget = new QuesTargetViewModel();
                //        quesTarget = _qnaService.GetQuesByTarget(target, valBotID);
                //        if (quesTarget != null)
                //        {
                //            text = quesTarget.ContentText;
                //        }
                //        hisVm.BotUnderStands = target;
                //    }
                //}

                AIMLbot.Result aimlBotResult = _aimlBotService.Chat(text, _user,_botService);
                string result = aimlBotResult.OutputSentences[0].ToString();

                //if (result.Contains("NOT_MATCH"))
                //{
                //if (text.Contains("postback") == false || text.Contains("module") == false)
                //{
                //    // Lấy target from knowledge base QnA trained mongodb
                //    string target = _apiNLR.GetPrecidictTextClass(text, valBotID);
                //    if (!String.IsNullOrEmpty(target))
                //    {
                //        target = Regex.Replace(target, "\n", "").Replace("\"", "");
                //        QuesTargetViewModel quesTarget = new QuesTargetViewModel();
                //        quesTarget = _qnaService.GetQuesByTarget(target, valBotID);
                //        if (quesTarget != null)
                //        {
                //            text = quesTarget.ContentText;
                //        }
                //        hisVm.BotUnderStands = target;

                //        aimlBotResult = _botService.Chat(text, _user);
                //        result = aimlBotResult.OutputSentences[0].ToString();
                //    }
                //}
                //}

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
                // nếu aiml bot có template trả thẳng ra module k thông qua button text module
                if (result.Replace("\r\n", "").Trim().Contains("postback_module"))
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
                                return chatbot(contains, token, botId);
                            }

                            List<string> msg = new List<string>();
                            msg.Add(aimlBotResult.OutputHtmlMessage[0].Replace(contains, ""));
                            if (contains == "postback_module_api_search")
                            {
                                return chatbot(txtModule, token, botId);
                            }
                            if (contains == "postback_module_med_get_info_patient")
                            {
                                return chatbot(txtModule, token, botId);
                            }
                            if (contains == "postback_module_age")
                            {
                                _user.Predicates.addSetting("agecheck", "true");
                                var handleAge = _handleMdService.HandledIsAge(contains, valBotID);
                                msg.Add(handleAge.Message);
                            }
                            if (contains == "postback_module_email")
                            {
                                _user.Predicates.addSetting("emailcheck", "true");
                                var handleEmail = _handleMdService.HandledIsEmail(contains, valBotID);
                                msg.Add(handleEmail.Message);
                            }
                            if (contains == "postback_module_phone")
                            {
                                _user.Predicates.addSetting("phonecheck", "true");
                                var handlePhone = _handleMdService.HandleIsPhoneNumber(contains, valBotID);
                                msg.Add(handlePhone.Message);
                            }
                            return Json(new
                            {
                                message = msg,
                                postback = aimlBotResult.OutputHtmlPostback,
                                messageLstSearchNLP = _lstQnAVm,
                                messageLstSymptoms = _lstSymptomVm,
                                isSearchNLP = isMdSearch
                            }, JsonRequestBehavior.AllowGet);
                        }
                        //return chatbot(txtModule, group, token, botId, isMdSearch);
                    }
                }
                // K tìm thấy trong Rule gọi tới module tri thức
                if (result.Contains("NOT_MATCH"))
                {
                    if (isMdSearch)
                    {
                        if (userBot.SystemConfigViewModel.Count() != 0)
                        {
                            string nameFuncAPI = "";
                            string valueBotId = "";
                            string number = "";
                            string field = "";
                            foreach (var item in userBot.SystemConfigViewModel)
                            {
                                if (item.Code == "UrlAPI")
                                    nameFuncAPI = item.ValueString;
                                if (item.Code == "ParamBotID")
                                    valueBotId = item.ValueString;
                                if (item.Code == "ParamAreaID")
                                    field = item.ValueString;
                                if (item.Code == "ParamNumberResponse")
                                    number = item.ValueString;
                            }
                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_006;
                            AddHistory(hisVm);
                            result = GetRelatedQuestion(nameFuncAPI, text, field, number, valueBotId);
                            if (!String.IsNullOrEmpty(result))
                            {
                                _lstQnAVm = new JavaScriptSerializer
                                {
                                    MaxJsonLength = Int32.MaxValue,
                                    RecursionLimit = 100
                                }.Deserialize<List<SearchNlpQnAViewModel>>(result);
                            }
                        }
                        else
                        {
                            return Json(new
                            {
                                message = new List<string>() { "Service API not found" },
                                postback = new List<string>() { null },
                                messageLstSearchNLP = _lstQnAVm,
                                messageLstSymptoms = _lstSymptomVm,
                                isSearchNLP = isMdSearch
                            }, JsonRequestBehavior.AllowGet);
                        }

                        if (String.IsNullOrEmpty(result))
                        {
                            //result = NOT_MATCH[res.OutputSentences[0]];
                            //result = aimlBotResult.OutputSentences[0].ToString();
                            hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_002;
                            AddHistory(hisVm);
                        }

                        if (botId == "3019")
                        {
                            string resultSymptomp = _apiNLR.GetListSymptoms(text, 1);
                            if (!String.IsNullOrEmpty(resultSymptomp))
                            {
                                _lstSymptomVm = new JavaScriptSerializer
                                {
                                    MaxJsonLength = Int32.MaxValue,
                                    RecursionLimit = 100
                                }.Deserialize<List<SearchSymptomViewModel>>(resultSymptomp);
                            }
                        }
                    }
                }

                if (text.Contains("postback_card"))
                {
                    var cardDb = _cardService.GetSingleCondition(text.Replace(".", String.Empty));
                    if (cardDb != null)
                    {
                        if (cardDb.IsHaveCondition)
                        {
                            _user.Predicates.addSetting("cardConditionCheck", "true");
                            _user.Predicates.addSetting("cardConditionPattern", text.Replace(".", String.Empty));
                        }
                        else
                        {
                            _user.Predicates.addSetting("cardConditionCheck", "false");
                            _user.Predicates.addSetting("cardConditionPattern", "");
                        }
                        if (!String.IsNullOrEmpty(cardDb.AttributeSystemName))
                        {
                            _user.Predicates.addSetting("IsHaveSetAttributeSystem", "true");
                            _user.Predicates.addSetting("AttributeName", cardDb.AttributeSystemName);
                        }
                        else
                        {
                            _user.Predicates.addSetting("IsHaveSetAttributeSystem", "false");
                            _user.Predicates.addSetting("AttributeName", "");
                        }
                        if (cardDb.CardStepID != null)
                        {
                            if (cardDb.IsConditionWithInputText)// yêu cầu nhập text để chuyển sang card step
                            {
                                _user.Predicates.addSetting("IsConditionWithInputText", "true");
                                _user.Predicates.addSetting("CardConditionWithInputTextPattern", "postback_card_" + cardDb.CardStepID);
                            }
                            else
                            {
                                _user.Predicates.addSetting("IsConditionWithInputText", "false");
                                _user.Predicates.addSetting("CardConditionWithInputTextPattern", "");
                            }
                        }
                        if (cardDb.CardStepID != null && cardDb.IsConditionWithInputText == false)
                        {
                            Session[CommonConstants.SessionResultBot] = aimlBotResult.OutputHtmlMessage;

                            return chatbot("postback_card_" + cardDb.CardStepID, token, botId);
                        }
                    }
                }
                // nếu nhập text trả lời ra postback
                string strTempPostback = aimlBotResult.SubQueries[0].Template;
                bool isPostback = Regex.Match(strTempPostback, "<template><srai>postback_card_(\\d+)</srai></template>").Success;
                if (isPostback)
                {
                    strTempPostback = Regex.Replace(strTempPostback, @"<(.|\n)*?>", "").Trim();
                    var cardDb = _cardService.GetSingleCondition(strTempPostback.Replace(".", String.Empty));
                    if (cardDb != null)
                    {
                        if (cardDb.IsHaveCondition)
                        {
                            _user.Predicates.addSetting("cardConditionCheck", "true");
                            _user.Predicates.addSetting("cardConditionPattern", strTempPostback.Replace(".", String.Empty));
                        }
                        else
                        {
                            _user.Predicates.addSetting("cardConditionCheck", "false");
                            _user.Predicates.addSetting("cardConditionPattern", "");
                        }
                        if (!String.IsNullOrEmpty(cardDb.AttributeSystemName))
                        {
                            _user.Predicates.addSetting("IsHaveSetAttributeSystem", "true");
                            _user.Predicates.addSetting("AttributeName", cardDb.AttributeSystemName);
                        }
                        else
                        {
                            _user.Predicates.addSetting("IsHaveSetAttributeSystem", "false");
                            _user.Predicates.addSetting("AttributeName", "");
                        }
                        if (cardDb.CardStepID != null)
                        {
                            if (cardDb.IsConditionWithInputText)// yêu cầu nhập text để chuyển sang card step
                            {
                                _user.Predicates.addSetting("IsConditionWithInputText", "true");
                                _user.Predicates.addSetting("CardConditionWithInputTextPattern", "postback_card_" + cardDb.CardStepID);
                            }
                            else
                            {
                                _user.Predicates.addSetting("IsConditionWithInputText", "false");
                                _user.Predicates.addSetting("CardConditionWithInputTextPattern", "");
                            }
                        }
                        if (cardDb.CardStepID != null && cardDb.IsConditionWithInputText == false)
                        {
                            Session[CommonConstants.SessionResultBot] = aimlBotResult.OutputHtmlMessage;

                            return chatbot("postback_card_" + cardDb.CardStepID, token, botId);
                        }
                    }
                }

                bool isPostbackAnswer = Regex.Match(strTempPostback, "<template><srai>postback_answer_(\\d+)</srai></template>").Success;
                if (isPostbackAnswer)
                {
                    if (result.Contains("postback"))
                    {
                        var cardDb = _cardService.GetSingleCondition(result.Replace(".", String.Empty));
                        if (cardDb != null)
                        {
                            // Nếu có yêu cầu click thẻ để đi theo luồng
                            if (cardDb.IsHaveCondition)
                            {
                                _user.Predicates.addSetting("cardConditionCheck", "true");
                                _user.Predicates.addSetting("cardConditionPattern", strTempPostback.Replace(".", String.Empty));
                            }
                            else
                            {
                                _user.Predicates.addSetting("cardConditionCheck", "false");
                                _user.Predicates.addSetting("cardConditionPattern", "");
                            }
                            if (!String.IsNullOrEmpty(cardDb.AttributeSystemName))
                            {
                                _user.Predicates.addSetting("IsHaveSetAttributeSystem", "true");
                                _user.Predicates.addSetting("AttributeName", cardDb.AttributeSystemName);
                            }
                            else
                            {
                                _user.Predicates.addSetting("IsHaveSetAttributeSystem", "false");
                                _user.Predicates.addSetting("AttributeName", "");
                            }
                            if (cardDb.CardStepID != null)
                            {
                                if (cardDb.IsConditionWithInputText)// yêu cầu nhập text để chuyển sang card step
                                {
                                    _user.Predicates.addSetting("IsConditionWithInputText", "true");
                                    _user.Predicates.addSetting("CardConditionWithInputTextPattern", "postback_card_" + cardDb.CardStepID);
                                }
                                else
                                {
                                    _user.Predicates.addSetting("IsConditionWithInputText", "false");
                                    _user.Predicates.addSetting("CardConditionWithInputTextPattern", "");
                                }
                            }
                            if (cardDb.CardStepID != null && cardDb.IsConditionWithInputText == false)
                            {
                                // tạo biến session lưu aimlBotResult.OutputHtmlMessage của lần trước
                                Session[CommonConstants.SessionResultBot] = aimlBotResult.OutputHtmlMessage;
                                return chatbot("postback_card_" + cardDb.CardStepID, token, botId);
                            }
                            return chatbot(result.Replace(".", String.Empty), token, botId);
                        }
                    }
                }
                //set new predicate to session user bot request
                SettingsDictionaryViewModel settingDic = new SettingsDictionaryViewModel();
                settingDic.Count = aimlBotResult.user.Predicates.Count;
                settingDic.orderedKeys = aimlBotResult.user.Predicates.orderedKeys;
                settingDic.settingsHash = aimlBotResult.user.Predicates.settingsHash;
                settingDic.SettingNames = aimlBotResult.user.Predicates.SettingNames;
                userBot.SettingDicstionary = settingDic;
                Session[CommonConstants.SessionUserBot] = userBot;
                //var lstMsg = aimlBotResult.OutputHtmlMessage;
                List<string> lstOutputMsg = new List<string>();
                List<string> lstMsg = new List<string>();
                lstMsg = aimlBotResult.OutputHtmlMessage;

                if (lstMsg.Count() == 1 && _lstSymptomVm.Count() != 0)
                {
                    foreach (var item in lstMsg)
                    {
                        if (item.Contains("NOT_MATCH"))
                        {
                            lstMsg = new List<string>();
                            isHaveSymptomAndMsgNotMatch = true;
                        }
                    }
                }

                if (lstMsg.Count() == 1)
                {
                    foreach (var item in lstMsg)
                    {
                        if (item.Contains("NOT_MATCH"))
                        {
                            lstMsg = new List<string>();
                            lstMsg.Add(aimlBotResult.RawOutput.ToString().Replace(".", String.Empty));
                        }
                    }
                }

                if (Session[CommonConstants.SessionResultBot] != null)
                {
                    var lstAimlResult = (List<string>)Session[CommonConstants.SessionResultBot];
                    if (lstAimlResult != null && lstAimlResult.Count() != 0)
                    {
                        foreach (string item in lstAimlResult)
                        {
                            string msg = item;
                            if (item.Contains("{{"))
                            {
                                foreach (var i in settingDic.settingsHash)
                                {
                                    string value = String.IsNullOrEmpty(i.Value) == true ? "N/A" : i.Value;
                                    string key = i.Key.ToLower();
                                    if (key == "sender_name")
                                    {
                                        value = "bạn";
                                    }
                                    msg = Regex.Replace(msg, "{{" + key + "}}", value);
                                }
                            }
                            lstOutputMsg.Add(msg);
                        }
                    }
                }
                if (lstMsg.Count() != 0)
                {
                    foreach (string item in lstMsg)
                    {
                        string msg = item;
                        if (item.Contains("{{"))
                        {
                            foreach (var i in settingDic.settingsHash)
                            {
                                string value = String.IsNullOrEmpty(i.Value) == true ? "N/A" : i.Value;
                                string key = i.Key.ToLower();
                                if (key == "sender_name")
                                {
                                    value = "bạn";
                                }
                                msg = Regex.Replace(msg, "{{" + key + "}}", value);
                            }
                        }
                        lstOutputMsg.Add(msg);
                        if (botId == "3019")
                        {
                            if (item.Contains("Nguyên nhân") || item.Contains("bác sĩ") || item.Contains("Bác sĩ"))
                            {
                                // Hiển thị thêm thông tin về triệu chứng đó
                                string resultSymptomp = _apiNLR.GetListSymptoms(_user.Predicates.grabSetting("content_message"), 1);
                                if (!String.IsNullOrEmpty(resultSymptomp))
                                {
                                    _lstSymptomVm = new JavaScriptSerializer
                                    {
                                        MaxJsonLength = Int32.MaxValue,
                                        RecursionLimit = 100
                                    }.Deserialize<List<SearchSymptomViewModel>>(resultSymptomp);
                                }
                            }
                        }
                    }
                }
                Session[CommonConstants.SessionResultBot] = null;
                return Json(new
                {
                    message = lstOutputMsg,
                    postback = aimlBotResult.OutputHtmlPostback,
                    messageLstSearchNLP = _lstQnAVm,
                    messageLstSymptoms = _lstSymptomVm,
                    isSearchNLP = isMdSearch,
                    isHaveSymptomsAndNotMatch = isHaveSymptomAndMsgNotMatch
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return Json(new
                {
                    message = new List<string>() { "Error" },
                    postback = new List<string>() { null },
                    messageLstSearchNLP = new List<string>() { },
                    messageLstSymptoms = new List<string>() { },
                    isSearchNLP = isMdSearch,
                    isHaveSymptomsAndNotMatch = isHaveSymptomAndMsgNotMatch
                }, JsonRequestBehavior.AllowGet);
            }
        }

        public string tempNodeBtnModule(string valModule)
		{
			StringBuilder sbPostback = new StringBuilder();
			sbPostback.AppendLine("<div class=\"_6biu\">");
			sbPostback.AppendLine("                                                                    <div  class=\"_23n- form_carousel\">");
			sbPostback.AppendLine("                                                                        <div class=\"_4u-c\">");
			sbPostback.AppendLine("                                                                            <div index=\"0\" class=\"_a28 lst_btn_carousel\">");
			sbPostback.AppendLine("                                                                                <div class=\"_a2e\">");
			sbPostback.AppendLine(" <div class=\"_2zgz _2zgz_postback\">");
			sbPostback.AppendLine("      <div class=\"_4bqf _6biq _6bir\" tabindex=\"0\" role=\"button\" data-postback =\"" + valModule + "\" style=\"border-color: {{color}} color: {{color}}\">+ " + valModule + "</div>");
			sbPostback.AppendLine(" </div>");
			sbPostback.AppendLine("                                                                                </div>");
			sbPostback.AppendLine("                                                                            </div>");
			sbPostback.AppendLine("                                                                            <div class=\"_4u-f\">");
			sbPostback.AppendLine("                                                                                <iframe aria-hidden=\"true\" class=\"_1_xb\" tabindex=\"-1\"></iframe>");
			sbPostback.AppendLine("                                                                            </div>");
			sbPostback.AppendLine("                                                                        </div>");
			return sbPostback.ToString();
		}
		private static string tempText(string text)
		{
			text = Regex.Replace(text, "  ", "<br/>");
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("<div class=\"_4xkn clearfix\">");
			sb.AppendLine("     <div class=\"profilePictureColumn\" style=\"bottom: 0px;\">");
			sb.AppendLine("         <div class=\"_4cqr\">");
			sb.AppendLine("             <img class=\"profilePicture img\" src=\"{{image_logo}}\" alt=\"\">");
			sb.AppendLine("             <div class=\"clearfix\"></div>");
			sb.AppendLine("         </div>");
			sb.AppendLine("     </div>");
			sb.AppendLine("     <div class=\"messages\">");
			sb.AppendLine("         <div class=\"_21c3\">");
			sb.AppendLine("             <div class=\"clearfix _2a0-\">");

			sb.AppendLine("<div class=\"_4xko _4xkr _tmpB\" tabindex=\"0\" role=\"button\" style=\"background-color:rgb(241, 240, 240); font-family: Segoe UI Light\">");
			sb.AppendLine("     <span>");
			sb.AppendLine("         <span>" + text + "</span>");
			sb.AppendLine("     </span>");
			sb.AppendLine("</div>");
			sb.AppendLine("</div>");
			sb.AppendLine("</div>");
			sb.AppendLine("</div>");
			sb.AppendLine("</div>");
			return sb.ToString();
		}

		//#region --SEARCH ENGINE ELASTICSEARCH--
		//public JsonResult GetAll(int page = 1, int pageSize = 10)
		//{
		//    int totalRow = 0;
		//    int from = (page - 1) * pageSize;
		//    var lstData = _elastic.GetAll(from, pageSize);

		//    if (lstData.Count() != 0)
		//    {
		//        totalRow = lstData[0].Total;
		//    }

		//    var paginationSet = new PaginationSet<SearchEngine.Data.Question>()
		//    {
		//        Items = lstData,
		//        Page = page,
		//        TotalCount = totalRow,
		//        MaxPage = pageSize,
		//        TotalPages = (int)Math.Ceiling((decimal)totalRow / pageSize)
		//    };

		//    return Json(paginationSet, JsonRequestBehavior.AllowGet);
		//}

		//public JsonResult Search(string text, bool isAccentVN = false)
		//{
		//    if (!String.IsNullOrEmpty(text))
		//        text = Regex.Replace(HttpUtility.HtmlDecode(text), @"<(.|\n)*?>", "");

		//    if (isAccentVN)
		//    {
		//        //text = _accentService.GetAccentVN(text);
		//    }

		//    var lstData = _elastic.Search(text);

		//    var paginationSet = new PaginationSet<SearchEngine.Data.Question>()
		//    {
		//        Items = lstData,
		//        Page = 1,
		//        TotalCount = 1,
		//        TotalPages = 1
		//    };

		//    return Json(paginationSet, JsonRequestBehavior.AllowGet);
		//}

		//public JsonResult Suggest(string text, bool isAccentVN = false)
		//{
		//    if (!String.IsNullOrEmpty(text))
		//        text = Regex.Replace(HttpUtility.HtmlDecode(text), @"<(.|\n)*?>", "");

		//    if (isAccentVN)
		//    {
		//        //text = _accentService.GetAccentVN(text);
		//    }

		//    var lstSuggest = _elastic.AutoComplete(text);
		//    return Json(lstSuggest, JsonRequestBehavior.AllowGet);
		//}

		//public JsonResult AddQnA(string question, string answer)
		//{
		//    string message = "";
		//    if (!String.IsNullOrEmpty(question))
		//    {
		//        return Json(message, JsonRequestBehavior.AllowGet);
		//    }
		//    var result = _elastic.Create(question, answer);
		//    return Json(result, JsonRequestBehavior.AllowGet);
		//}

		public JsonResult ImportExcelQnA()
		{
			if (Request.Files.Count > 0)
				try
				{
					HttpPostedFileBase file = Request.Files[0];
					string pathtempt = Helper.ReadString("ExcelTemplateTemptPath1");
					if (!Directory.Exists(pathtempt))
					{
						Directory.CreateDirectory(pathtempt);
					}
					string path = System.IO.Path.Combine(pathtempt, DateTime.Now.Ticks.ToString() + "_" + file.FileName);
					file.SaveAs(path);

					var formBotId = System.Web.HttpContext.Current.Request.Unvalidated.Form["botId"];
					var botId = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue, RecursionLimit = 100 }.Deserialize<string>(formBotId);

					System.Data.DataTable dt = ReadExcelFileToDataTable(path);
					return ReadFileExcelQnA(dt, botId);
				}
				catch (Exception ex)
				{
					return Json("ERROR:" + ex.Message.ToString());
				}
			else
			{
				return Json("Bạn chưa chọn file để tải lên.");
			}
		}

		private System.Data.DataTable ReadExcelFileToDataTable(string path)
		{
			string POCpath = @"" + path;
			POCpath = POCpath.Replace("\\\\", "\\");
			IExcelDataReader dataReader;
			FileStream fileStream = new FileStream(POCpath, FileMode.Open);
			if (path.EndsWith(".xls"))
			{
				dataReader = ExcelReaderFactory.CreateBinaryReader(fileStream);
			}
			else
			{
				dataReader = ExcelReaderFactory.CreateOpenXmlReader(fileStream);
			}
			DataSet result = dataReader.AsDataSet();

			System.Data.DataTable dt = result.Tables[0];
			return dt;
		}

		public JsonResult ReadFileExcelQnA(System.Data.DataTable dt, string botId)
		{
			try
			{
				if (dt.Rows.Count > 0)
				{
					var quesList = new List<ModuleQnAViewModel>();
					for (var i = 1; i < dt.Rows.Count; i++)
					{
						var mdQnA = new ModuleQnAViewModel();
						var item = dt.Rows[i];
						mdQnA.QuesID = i;
						mdQnA.QuesContent = item[0] == null ? "" : item[0].ToString();
						mdQnA.AnsContent = item[1] == null ? "" : item[1].ToString();
						mdQnA.AnsContentHTML = item[2] == null ? "" : item[2].ToString();
						mdQnA.AreaName = item[3] == null ? "0" : item[3].ToString();
						if (String.IsNullOrEmpty(mdQnA.AreaName))
						{
							mdQnA.AreaName = "0";
						}
						if (mdQnA.AreaName.Trim().ToLower().Contains("sở hữu trí tuệ"))
						{
							mdQnA.AreaName = "1";
						}
						if (mdQnA.AreaName.Trim().ToLower().Contains("thuế"))
						{
							mdQnA.AreaName = "2";
						}
						string mess = "";
						bool flag = false;

						if (string.IsNullOrEmpty(mdQnA.QuesContent))
						{
							mess += "Câu hỏi không được để trống";
							flag = true;
						}
						if (flag)
						{
							return Json(new { status = false, msg = "File Excel có lỗi ở dòng thứ " + (i + 1) + ":<br/>" + mess });
						}

						//quesList.Add(mdQnA);

						MdQuestion mdQuesDb = new MdQuestion();
						MdAnswer mdAnsDb = new MdAnswer();
						ApiQnaNLRService apiNLR = new ApiQnaNLRService();
						// add Ques
						mdQuesDb.UpdateModuleQuestion(mdQnA);
						mdQuesDb.BotID = Int32.Parse(botId);
						mdQuesDb.AreaID = Int32.Parse(mdQnA.AreaName);
						mdQuesDb.ContentHTML = mdQnA.QuesContent;
						_moduleSearchEngineService.CreateQuestion(mdQuesDb);
						_moduleSearchEngineService.Save();
						// add Ans
						mdAnsDb.UpdateModuleAnswer(mdQnA);
						mdAnsDb.MQuestionID = mdQuesDb.ID;
						mdAnsDb.ContentHTML = mdQnA.AnsContentHTML;
						mdAnsDb.BotID = Int32.Parse(botId);
						_moduleSearchEngineService.CreateAnswer(mdAnsDb);

						_moduleSearchEngineService.Save();

						string rsAPI = apiNLR.AddQues(mdQuesDb.ID.ToString(), mdQuesDb.ContentText, mdAnsDb.ContentText, mdQnA.AreaName, mdQuesDb.ContentHTML, mdQnA.AnsContentHTML, botId);
						if (!String.IsNullOrEmpty(rsAPI))
						{
							mdQuesDb.IsTrained = true;
							_moduleSearchEngineService.UpdateQuestion(mdQuesDb);
							_moduleSearchEngineService.Save();
						}
						//CreateQuesForImport("", ques.AreaTitle, ques.ContentsText, ques.AnsContents, null, null, null, null
						//, null, null, null);
					}
					return Json(new { status = true });
				}
				else
				{
					return Json(new { status = false, msg = "File không có dữ liệu" });
				}
			}
			catch (Exception ex)
			{
				return Json(new { status = false, msg = "Lỗi: " + ex.Message.ToString() });
			}
		}



		#region --DATA SOURCE API--

		private string apiRelateQA = "/api/qa_for_all/get_related_pair";

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
		public string GetRelatedQuestion(string nameFuncAPI, string question, string field, string number, string botId)
		{
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
			if (responseString != null)
			{
				var lstQues = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue, RecursionLimit = 100 }.Deserialize<List<dynamic>>(responseString);
			}
			return responseString;
		}

		#endregion
		public class Result
		{
			public string Item { get; set; }
			public string[] ArrItems { get; set; }
		}

		public class ObjQnaAPI
		{
			public string _id { set; get; }
			public string question { set; get; }
			public string answer { set; get; }
			public string html { set; get; }
			public string field { set; get; }
			public int id { set; get; }
		}

		public UserBotViewModel UserBotInfo
		{
			get
			{
				if (Session != null)
				{
					if (Session[CommonConstants.SessionUserBot] != null)
					{
						return (UserBotViewModel)Session[CommonConstants.SessionUserBot];
					}
				}
				return null;
			}
			set
			{
				if (value == null)
					Session.Remove(CommonConstants.SessionUserBot);
				else
					Session[CommonConstants.SessionUserBot] = value;
			}
		}

		private void LogError(Exception ex)
		{
			try
			{
				BotProject.Model.Models.Error error = new BotProject.Model.Models.Error();
				error.CreatedDate = DateTime.Now;
				error.Message = ex.Message;
				error.StackTrace = ex.StackTrace;
				_errorService.Create(error);
				_errorService.Save();
			}
			catch
			{
			}
		}

		public void AddHistory(HistoryViewModel hisVm)
		{
			History hisDb = new History();
			hisDb.UpdateHistory(hisVm);
			_historyService.Create(hisDb);
			_historyService.Save();
		}
	}
}