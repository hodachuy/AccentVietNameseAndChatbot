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

namespace BotProject.Web.API_Livechat
{
	[RoutePrefix("api/lc_bot")]
	public class LiveChatBotController : ApiControllerBase
	{
		// Thẻ chat với chuyên viên
		public string _contactAdmin = Helper.ReadString("AdminContact");
		public string _titlePayloadContactAdmin = Helper.ReadString("TitlePayloadAdminContact");

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

		private ApiQnaNLRService _apiNLR;
		private IErrorService _errorService;
        private BotService _botService;
        private ISettingService _settingService;
		private ICardService _cardService;
		private IAIMLFileService _aimlFileService;
		private IBotService _botDbService;
		private User _user;
		private Dictionary<string, string> _dicNotMatch;
		private Dictionary<string, string> _dicAttributeUser;
		private IApplicationPlatformUserService _appPlatformUser;
		private IAttributeSystemService _attributeService;
		private IHandleModuleServiceService _handleMdService;
		private IHistoryService _historyService;

		//Accent Vietnamese
		private AccentService _accentService;

		public List<string> _lstBotReplyResponse = new List<string>();
		public LiveChatBotController(IErrorService errorService,
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
            _botService = BotService.BotInstance;
            _botDbService = botDbService;
			_settingService = settingService;
			_cardService = cardService;
			_aimlFileService = aimlFileService;
			_appPlatformUser = appPlatformUser;
			_attributeService = attributeService;
			_handleMdService = handleMdService;
			_historyService = historyService;
			_apiNLR = new ApiQnaNLRService();
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

                //var settingDb = _settingService.GetSettingByBotID(botId);
                //var systemConfig = _settingService.GetListSystemConfigByBotId(botId);
                //var lstAttribute = _attributeService.GetListAttributePlatform(senderId, botId).ToList();

                var lstAIML = _aimlFileService.GetActiveByBotId(botId);
                var lstAIMLVm = Mapper.Map<IEnumerable<AIMLFileViewModel>, IEnumerable<AIMLViewModel>>(lstAIML);
                _botService.loadAIMLFromDatabase(lstAIMLVm);

                _user = _botService.loadUserBot(message.senderId);

    //            if (lstAttribute.Count() != 0)
				//{
				//	_dicAttributeUser = new Dictionary<string, string>();
				//	foreach (var attr in lstAttribute)
				//	{
				//		_dicAttributeUser.Add(attr.AttributeKey, attr.AttributeValue);
				//	}
				//}
				// get list message response
				var lstMsgResponse = MessageResponse(text, senderId, botId).Result;
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
				response = request.CreateResponse(HttpStatusCode.OK, new
				{
					status = "2",
					message = "Thành công",
					data = lstResult
				});
				return response;
			});
		}
		private async Task<List<string>> MessageResponse(string text, string senderId, int botId, bool isHavePreviousResponse = false)
		{
			if (!isHavePreviousResponse)
			{
				_lstBotReplyResponse = new List<string>();
			}
			if (String.IsNullOrEmpty(text))
			{
				_lstBotReplyResponse = new List<string>();
				return await Task.FromResult<List<string>>(_lstBotReplyResponse);
			}

			text = HttpUtility.HtmlDecode(text);
			text = Regex.Replace(text, @"<(.|\n)*?>", "").Trim();
			text = Regex.Replace(text, @"\p{Cs}", "").Trim(); //remove emoji

			if (String.IsNullOrEmpty(text))
			{
				_lstBotReplyResponse = new List<string>();
				return await Task.FromResult<List<string>>(_lstBotReplyResponse);
			}

			ApplicationPlatformUser plUserDb = new ApplicationPlatformUser();
			plUserDb = _appPlatformUser.GetByUserId(senderId);


			if (!text.Contains("postback") && !text.Contains(_contactAdmin))
			{
				//_accentService = new AccentService(); 
				_accentService = AccentService.SingleInstance;
				string textAccentVN = _accentService.GetAccentVN(text);
				if (textAccentVN != text)
				{
					string msg = FacebookTemplate.GetMessageTemplateText("Ý bạn là: " + textAccentVN + "", senderId).ToString();
					_lstBotReplyResponse.Add(HandleMessageJson(msg, senderId));
				}
				text = textAccentVN;
				//AddAttributeDefault(senderId, botId, "content_message", text);
				//_dicAttributeUser.Remove("content_message");
				//_dicAttributeUser.Add("content_message", text);
			}

			// Xét payload postback nếu postback từ quickreply sẽ chứa thêm sperator - và tiêu đề nút
			string attributeValueFromPostback = "";
			if (text.Contains("postback"))
			{
				var arrPostback = Regex.Split(text, "-");
				if (arrPostback.Length > 1)
				{
					attributeValueFromPostback = arrPostback[1];
				}
				text = arrPostback[0];
			}

			//HistoryViewModel hisVm = new HistoryViewModel();
			//hisVm.BotID = botId;
			//hisVm.CreatedDate = DateTime.Now;
			//hisVm.UserSay = text;
			//hisVm.UserName = senderId;
			//hisVm.Type = CommonConstants.TYPE_KIOSK;

			DateTime dStartedTime = DateTime.Now;
			DateTime dTimeOut = DateTime.Now.AddSeconds(_timeOut);

			try
			{
				if (plUserDb != null)
				{
					// Chat với admin
					if (plUserDb.PredicateName == "Admin_Contact")
					{
						var handleAdminContact = _handleMdService.HandleIsAdminContact(text, botId);
						//hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_004;
						//AddHistory(hisVm);
						if (text.Contains("postback") || text.Contains(_contactAdmin))
						{
							plUserDb.IsHavePredicate = false;
							plUserDb.PredicateName = "";
							plUserDb.PredicateValue = "";
							plUserDb.IsHaveCardCondition = false;
							plUserDb.CardConditionPattern = "";
							plUserDb.IsConditionWithAreaButton = false;
							plUserDb.CardConditionAreaButtonPattern = "";
							plUserDb.CardStepPattern = "";
							plUserDb.AttributeName = "";
							plUserDb.IsHaveSetAttributeSystem = false;
							plUserDb.IsConditionWithInputText = false;
							plUserDb.CardConditionWithInputTextPattern = "";
							plUserDb.TypeDevice = "kiosk";
							_appPlatformUser.Update(plUserDb);
							_appPlatformUser.Save();
							return await MessageResponse(text, senderId, botId);
						}
						// Tin nhắn vắng mặt
						if (_isHaveMessageAbsent)
						{
							if (HelperMethods.IsTimeInWorks() == false)
							{
								//await SendMessageTask(FacebookTemplate.GetMessageTemplateTextAndQuickReply(_messageAbsent, "{{senderId}}", _patternCardPayloadProactive, _titleCardPayloadProactive).ToString(),sender);
								//return new HttpResponseMessage(HttpStatusCode.OK);

								return await Task.FromResult<List<string>>(_lstBotReplyResponse);
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
									string tempJson = HandleMessageJson(temp, senderId);
									_lstBotReplyResponse.Add(tempJson);
									//await SendMessageTask(tempJson, sender);
								}
								//return new HttpResponseMessage(HttpStatusCode.OK);
							}
						}
						return await Task.FromResult<List<string>>(_lstBotReplyResponse);
					}
				}
				else
				{
					plUserDb = new ApplicationPlatformUser();
					plUserDb.UserId = senderId;
					plUserDb.IsHavePredicate = false;
					plUserDb.IsProactiveMessage = false;
					plUserDb.TimeOut = dTimeOut;
					plUserDb.CreatedDate = DateTime.Now;
					plUserDb.StartedOn = dStartedTime;
					plUserDb.FirstName = "N/A"; //profileUser.first_name;
					plUserDb.Age = 0; //"N/A";
					plUserDb.LastName = "N/A"; //profileUser.last_name;
					plUserDb.UserName = "bạn"; //profileUser.first_name + " " + profileUser.last_name;
					plUserDb.Gender = true; //"N/A";
					_appPlatformUser.Add(plUserDb);
					_appPlatformUser.Save();

					// add attribute default user platform
					//AddAttributeDefault(senderId, botId, "sender_id", plUserDb.UserId);
					//AddAttributeDefault(senderId, botId, "sender_name", plUserDb.UserName);
					//AddAttributeDefault(senderId, botId, "sender_first_name", plUserDb.FirstName);
					//AddAttributeDefault(senderId, botId, "sender_last_name", plUserDb.LastName);
					//AddAttributeDefault(senderId, botId, "gender", "N/A");
				}

				plUserDb.StartedOn = dStartedTime;
				plUserDb.TimeOut = dTimeOut;
				// Nếu có yêu cầu click thẻ để đi theo luồng
				if (plUserDb.IsHaveCardCondition)
				{
					if (!text.Contains("postback") && !text.Contains(_contactAdmin))
					{
						var cardDb = _cardService.GetCardByPattern(plUserDb.CardConditionPattern);
						if (cardDb == null)
						{
							return await Task.FromResult<List<string>>(_lstBotReplyResponse);
						}
						string tempJsonFacebook = cardDb.TemplateJsonFacebook;
						if (!String.IsNullOrEmpty(tempJsonFacebook))
						{
							tempJsonFacebook = tempJsonFacebook.Trim();
							string[] strArrayJson = Regex.Split(tempJsonFacebook, "split");
							if (strArrayJson.Length != 0)
							{
								//await SendMessageTask(FacebookTemplate.GetMessageTemplateText("Anh/chị vui lòng chọn lại thông tin bên dưới", sender).ToString(), sender);
								string msg = FacebookTemplate.GetMessageTemplateText("Anh/chị vui lòng chọn lại thông tin bên dưới", senderId).ToString();
								_lstBotReplyResponse.Add(msg);
								var strArray = strArrayJson.Where(x => !string.IsNullOrEmpty(x)).ToArray();
								foreach (var temp in strArray)
								{
									//string tempJson = temp;
									string tempJson = HandleMessageJson(temp, senderId);
									_lstBotReplyResponse.Add(tempJson);
									//await SendMessageTask(tempJson, sender);
								}
								//return new HttpResponseMessage(HttpStatusCode.OK);
								return await Task.FromResult<List<string>>(_lstBotReplyResponse);
							}
						}
					}
				}

				// AttributePlatform nếu thẻ trước có biến cần lưu
				if (plUserDb.IsHaveSetAttributeSystem)
				{
					AttributePlatformUser attPlUser = new AttributePlatformUser();
					attPlUser.AttributeKey = plUserDb.AttributeName;
					//attFbUser.AttributeValue = text;
					attPlUser.BotID = botId;
					attPlUser.UserID = senderId;
					if (!text.Contains("postback") && !text.Contains(_contactAdmin))
					{
						attPlUser.AttributeValue = text;
					}
					else if (text.Contains("postback"))
					{
						attPlUser.AttributeValue = attributeValueFromPostback.Trim();
					}
					if (attPlUser.AttributeKey == "age")
					{
						bool isAge = Regex.Match(text, NumberPattern).Success;
						if (isAge)
						{
							attPlUser.AttributeValue = text;
						}
						else
						{
							//await SendMessageTask(FacebookTemplate.GetMessageTemplateText("Ký tự phải là số, Anh/chị vui lòng nhập lại độ tuổi", sender).ToString(), sender);
							//return new HttpResponseMessage(HttpStatusCode.OK);
							string msg = FacebookTemplate.GetMessageTemplateText("Ký tự phải là số, Anh/chị vui lòng nhập lại độ tuổi", senderId).ToString();
							_lstBotReplyResponse.Add(HandleMessageJson(msg, senderId));
							return await Task.FromResult<List<string>>(_lstBotReplyResponse);
						}
					}

					_dicAttributeUser.Remove(attPlUser.AttributeKey);
					_dicAttributeUser.Add(attPlUser.AttributeKey, attPlUser.AttributeValue);

					var att = _attributeService.CreateUpdateAttributePlatform(attPlUser);

					if (attPlUser.AttributeKey == "age")
					{
						plUserDb.Age = Int32.Parse(text);
						plUserDb.IsHaveSetAttributeSystem = false;
						plUserDb.AttributeName = "";
						_appPlatformUser.Update(plUserDb);
						_appPlatformUser.Save();

						_dicAttributeUser.Remove("age");
						_dicAttributeUser.Add("age", text);
						//return await ExcuteMessage("postback_card_8927", sender, botId); //postback_card_8927 thẻ thông tin người dùng
					}
				}

				// Nhập text để đi luồng tiếp theo nhưng CardStepID không được rỗng
				if (plUserDb.IsConditionWithInputText)
				{
					if (!text.Contains("postback") && !text.Contains(_contactAdmin))
					{
						plUserDb.IsHaveSetAttributeSystem = false;
						plUserDb.AttributeName = "";
						plUserDb.IsConditionWithInputText = false;
						plUserDb.IsHaveCardCondition = false;
						_appPlatformUser.Update(plUserDb);
						_appPlatformUser.Save();
						return await MessageResponse(plUserDb.CardConditionWithInputTextPattern, senderId, botId);
						//return await ExcuteMessage(fbUserDb.CardConditionWithInputTextPattern, sender, botId); //postback_card_8927 thẻ thông tin người dùng
					}
				}

				// Nếu có yêu cầu query text theo lĩnh vực button
				// Click button -> card (tên card nên đặt như tên lĩnh vực ngắn gọn)
				// Build lại kịch bản với từ khoán ngắn gọn + tên lĩnh vực
				// ví dụ: thủ tục cấp phép, thủ tục giạn + tên lĩnh vực
				if (plUserDb.IsConditionWithAreaButton)
				{
					if (!text.Contains("postback") && !text.Contains(_contactAdmin))
					{
						var cardDb = _cardService.GetCardByPattern(plUserDb.CardConditionAreaButtonPattern);
						if (cardDb == null)
						{
							_lstBotReplyResponse = new List<string>();
							return await Task.FromResult<List<string>>(_lstBotReplyResponse);
							//return new HttpResponseMessage(HttpStatusCode.OK);
						}
						string area = cardDb.Name;
						text = text + " " + area;// + thêm tên lĩnh vực button và nội dung trong form QnA có chứa từ lĩnh vực
					}
				}

				// Điều kiện xử lý module
				if (plUserDb.IsHavePredicate)
				{
					var predicateName = plUserDb.PredicateName;
					if (predicateName == "ApiSearch")
					{
						if (text.Contains("postback_card") || text.Contains(_contactAdmin))// nều còn điều kiện search mà chọn postback
						{
							plUserDb.IsHavePredicate = false;
							plUserDb.PredicateName = "";
							plUserDb.PredicateValue = "";
							plUserDb.IsHaveCardCondition = false;
							plUserDb.CardConditionPattern = "";
							plUserDb.IsConditionWithAreaButton = false;
							plUserDb.CardConditionAreaButtonPattern = "";
							plUserDb.CardStepPattern = "";
							plUserDb.AttributeName = "";
							plUserDb.IsHaveSetAttributeSystem = false;
							plUserDb.IsConditionWithInputText = false;
							plUserDb.CardConditionWithInputTextPattern = "";
							_appPlatformUser.Update(plUserDb);
							_appPlatformUser.Save();
							return await MessageResponse(text, senderId, botId);
							//return await ExcuteMessage(text, sender, botId);
						}

						string predicateValue = plUserDb.PredicateValue;
						var handleMdSearch = _handleMdService.HandleIsSearchAPI(text, predicateValue, "");

						//hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_005;
						//AddHistory(hisVm);

						_lstBotReplyResponse.Add(HandleMessageJson(handleMdSearch.TemplateJsonFacebook, senderId));

						return await Task.FromResult<List<string>>(_lstBotReplyResponse);
						//return await SendMessage(handleMdSearch.TemplateJsonFacebook, sender);
					}
				}
				else // Input: Khởi tạo module được chọn
				{
					if (text.Contains(CommonConstants.ModuleAdminContact))
					{
						var handleAdminContact = _handleMdService.HandleIsAdminContact(text, botId);

						plUserDb.IsHavePredicate = true;
						plUserDb.PredicateName = "Admin_Contact";
						plUserDb.PredicateValue = "";
						plUserDb.IsHaveCardCondition = false;
						plUserDb.CardConditionPattern = "";
						plUserDb.IsConditionWithAreaButton = false;
						plUserDb.CardConditionAreaButtonPattern = "";
						plUserDb.CardStepPattern = "";
						plUserDb.IsHaveSetAttributeSystem = false;
						plUserDb.AttributeName = "";
						plUserDb.IsConditionWithInputText = false;
						plUserDb.CardConditionWithInputTextPattern = "";
						_appPlatformUser.Update(plUserDb);
						_appPlatformUser.Save();

						//hisVm.UserSay = "[Chat với chuyên viên]";
						//hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_003;
						//AddHistory(hisVm);

						// Tin nhắn vắng mặt
						if (_isHaveMessageAbsent)
						{
							if (HelperMethods.IsTimeInWorks() == false)
							{
								string msg = FacebookTemplate.GetMessageTemplateTextAndQuickReply(_messageAbsent, "{{senderId}}", _patternCardPayloadProactive, _titleCardPayloadProactive).ToString();
								_lstBotReplyResponse.Add(HandleMessageJson(msg, senderId));
								//await SendMessageTask(FacebookTemplate.GetMessageTemplateTextAndQuickReply(_messageAbsent, "{{senderId}}", _patternCardPayloadProactive, _titleCardPayloadProactive).ToString(), sender);
								//return new HttpResponseMessage(HttpStatusCode.OK);
								return await Task.FromResult<List<string>>(_lstBotReplyResponse);
							}
						}
						string[] strArrayJson = Regex.Split(handleAdminContact.TemplateJsonFacebook, "split");//nhớ thêm bên formcard xử lý lục trên face
						if (strArrayJson.Length != 0)
						{
							var strArray = strArrayJson.Where(x => !string.IsNullOrEmpty(x)).ToArray();
							foreach (var temp in strArray)
							{
								//string tempJson = temp;
								//await SendMessageTask(tempJson, sender);
								string tempJson = HandleMessageJson(temp, senderId);
								_lstBotReplyResponse.Add(tempJson);
							}
						}
						return await Task.FromResult<List<string>>(_lstBotReplyResponse);
						//return new HttpResponseMessage(HttpStatusCode.OK);
					}

					if (text.Contains(CommonConstants.ModuleSearchAPI))
					{
						string mdSearchId = text.Replace(".", String.Empty).Replace("postback_module_api_search_", "");
						var handleMdSearch = _handleMdService.HandleIsSearchAPI(text, mdSearchId, "");
						plUserDb.IsHavePredicate = true;
						plUserDb.PredicateName = "ApiSearch";
						plUserDb.PredicateValue = mdSearchId;
						plUserDb.IsHaveCardCondition = false;
						plUserDb.CardConditionPattern = "";
						plUserDb.IsConditionWithAreaButton = false;
						plUserDb.CardConditionAreaButtonPattern = "";
						plUserDb.CardStepPattern = "";
						plUserDb.IsHaveSetAttributeSystem = false;
						plUserDb.AttributeName = "";
						plUserDb.IsConditionWithInputText = false;
						plUserDb.CardConditionWithInputTextPattern = "";
						_appPlatformUser.Update(plUserDb);
						_appPlatformUser.Save();

						//hisVm.UserSay = "[Tra cứu]";
						//hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_003;
						//AddHistory(hisVm);

						//return await SendMessage(handleMdSearch.TemplateJsonFacebook, sender);
						string msg = HandleMessageJson(handleMdSearch.TemplateJsonFacebook, senderId);
						_lstBotReplyResponse.Add(msg);
						return await Task.FromResult<List<string>>(_lstBotReplyResponse);
					}
				}


				AIMLbot.Result aimlBotResult = _botService.Chat(text, _user);
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
								return await MessageResponse(contains, senderId, botId);
								//return await ExcuteMessage(contains, sender, botId);
							}

							string rsHandle = "";

							if (contains == "postback_module_api_search")
							{
								return await MessageResponse(txtModule, senderId, botId);
								//return await ExcuteMessage(txtModule, sender, botId);
							}
							if (contains == "postback_module_med_get_info_patient")
							{
								return await MessageResponse(txtModule, senderId, botId);

								//return await ExcuteMessage(txtModule, sender, botId);
							}
							if (contains == "postback_module_age")
							{
								plUserDb.PredicateName = "Age";
								var handleAge = _handleMdService.HandledIsAge(contains, botId);
								rsHandle = handleAge.TemplateJsonFacebook;
							}
							if (contains == "postback_module_email")
							{
								plUserDb.PredicateName = "Email";
								var handleEmail = _handleMdService.HandledIsEmail(contains, botId);
								rsHandle = handleEmail.TemplateJsonFacebook;
							}
							if (contains == "postback_module_phone")
							{
								plUserDb.PredicateName = "Phone";
								var handlePhone = _handleMdService.HandleIsPhoneNumber(contains, botId);
								rsHandle = handlePhone.TemplateJsonFacebook;
							}
							plUserDb.IsHavePredicate = true;
							plUserDb.PredicateValue = "";
							plUserDb.IsHaveCardCondition = false;
							plUserDb.CardConditionPattern = "";
							plUserDb.CardStepPattern = "";
							plUserDb.IsHaveSetAttributeSystem = false;
							plUserDb.AttributeName = "";
							plUserDb.IsConditionWithInputText = false;
							plUserDb.CardConditionWithInputTextPattern = "";
							_appPlatformUser.Update(plUserDb);
							_appPlatformUser.Save();

							string msg = HandleMessageJson(rsHandle, senderId);
							_lstBotReplyResponse.Add(msg);
							return await Task.FromResult<List<string>>(_lstBotReplyResponse);
							//return await SendMessage(rsHandle, sender);
						}
					}
				}

				if (result.Contains("NOT_MATCH"))
				{
					//hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_002;
					//AddHistory(hisVm);
					try
					{
						plUserDb.IsHaveCardCondition = false;
						plUserDb.CardConditionPattern = "";
						plUserDb.IsConditionWithAreaButton = false;
						plUserDb.CardConditionAreaButtonPattern = "";
						plUserDb.CardStepPattern = "";
						plUserDb.IsHaveSetAttributeSystem = false;
						plUserDb.AttributeName = "";
						plUserDb.IsConditionWithInputText = false;
						plUserDb.CardConditionWithInputTextPattern = "";
						_appPlatformUser.Update(plUserDb);
						_appPlatformUser.Save();
					}
					catch (Exception ex)
					{
						LogError("RS NOT MATCH:" + ex.Message);
					}

					//if (_isSearchAI) //_isSearchAI
					//{
					//	var systemConfigDb = _settingService.GetListSystemConfigByBotId(botId);
					//	var systemConfigVm = Mapper.Map<IEnumerable<BotProject.Model.Models.SystemConfig>, IEnumerable<SystemConfigViewModel>>(systemConfigDb);
					//	if (systemConfigVm.Count() == 0)
					//	{
					//		string msgTemp = FacebookTemplate.GetMessageTemplateText("Xin lỗi, tôi chưa được học để hiểu nội dung này", senderId).ToString();

					//		_lstBotReplyResponse.Add(HandleMessageJson(msgTemp, senderId));
					//		return await Task.FromResult<List<string>>(_lstBotReplyResponse);
					//		//return await SendMessage(FacebookTemplate.GetMessageTemplateText("Tìm kiếm xử lý ngôn ngữ tự nhiên hiện không hoạt động, bạn vui lòng thử lại sau nhé!", sender));// not match
					//	}
					//	string nameFunctionAPI = "";
					//	string number = "";
					//	string field = "";
					//	string valueBotId = "";
					//	foreach (var item in systemConfigVm)
					//	{
					//		if (item.Code == "UrlAPI")
					//			nameFunctionAPI = item.ValueString;
					//		if (item.Code == "ParamBotID")
					//			valueBotId = item.ValueString;
					//		if (item.Code == "ParamAreaID")
					//			field = item.ValueString;
					//		if (item.Code == "ParamNumberResponse")
					//			number = item.ValueString;
					//	}
					//	hisVm.BotHandle = MessageBot.BOT_HISTORY_HANDLE_006;
					//	AddHistory(hisVm);
					//	string resultAPI = GetRelatedQuestionToFacebook(nameFunctionAPI, text, field, "5", valueBotId);
					//	if (!String.IsNullOrEmpty(resultAPI))
					//	{
					//		var lstQnaAPI = new JavaScriptSerializer
					//		{
					//			MaxJsonLength = Int32.MaxValue,
					//			RecursionLimit = 100
					//		}.Deserialize<List<SearchNlpQnAViewModel>>(resultAPI);
					//		// render template json generic
					//		int totalQnA = lstQnaAPI.Count();
					//		string totalFind = "Tôi tìm thấy " + totalQnA + " câu hỏi liên quan đến câu hỏi của bạn";
					//		string msgTempTotalSearch = FacebookTemplate.GetMessageTemplateText(totalFind, senderId).ToString();
					//		_lstBotReplyResponse.Add(HandleMessageJson(msgTempTotalSearch, senderId));
					//		string strTemplateGenericRelatedQuestion = FacebookTemplate.GetMessageTemplateGenericByList(senderId, lstQnaAPI).ToString();
					//		_lstBotReplyResponse.Add(HandleMessageJson(strTemplateGenericRelatedQuestion, senderId));
					//	}

					//	if (_lstBotReplyResponse.Count() != 0)
					//	{
					//		return await Task.FromResult<List<string>>(_lstBotReplyResponse);
					//	}
					//}

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

					string msg = FacebookTemplate.GetMessageTemplateTextAndQuickReply(strDefaultNotMatch, senderId, _contactAdmin, _titlePayloadContactAdmin).ToString();
					_lstBotReplyResponse.Add(HandleMessageJson(msg, senderId));
					return await Task.FromResult<List<string>>(_lstBotReplyResponse);
					//await SendMessage(FacebookTemplate.GetMessageTemplateTextAndQuickReply(strDefaultNotMatch, sender, _contactAdmin, _titlePayloadContactAdmin));// not match
					//return new HttpResponseMessage(HttpStatusCode.OK);
				}


				// input là postback
				if (text.Contains("postback_card"))
				{
					var cardDb = _cardService.GetCardByPattern(text.Replace(".", String.Empty));
					if (!String.IsNullOrEmpty(cardDb.AttributeSystemName))
					{
						plUserDb.IsHaveSetAttributeSystem = true;
						plUserDb.AttributeName = cardDb.AttributeSystemName;
					}
					else
					{
						plUserDb.IsHaveSetAttributeSystem = false;
						plUserDb.AttributeName = "";
					}

					if (cardDb.CardStepID != null)
					{
						plUserDb.CardStepPattern = "postback_card_" + cardDb.CardStepID;
						if (cardDb.IsConditionWithInputText)// yêu cầu nhập text để chuyển sang card step
						{
							plUserDb.IsConditionWithInputText = true;
							plUserDb.CardConditionWithInputTextPattern = plUserDb.CardStepPattern;
						}
						else
						{
							plUserDb.IsConditionWithInputText = false;
							plUserDb.CardConditionWithInputTextPattern = "";
						}
					}
					else
					{
						plUserDb.CardStepPattern = "";
					}
					if (cardDb.IsHaveCondition)
					{
						plUserDb.IsHaveCardCondition = true;
						plUserDb.CardConditionPattern = text.Replace(".", String.Empty);
					}
					else
					{
						plUserDb.IsHaveCardCondition = false;
						plUserDb.CardConditionPattern = "";
					}

					if (cardDb.IsConditionWithAreaButton)
					{
						plUserDb.IsConditionWithAreaButton = true;
						plUserDb.CardConditionAreaButtonPattern = text.Replace(".", String.Empty);
					}
					else
					{
						plUserDb.IsConditionWithAreaButton = false;
						plUserDb.CardConditionAreaButtonPattern = "";
					}
					_appPlatformUser.Update(plUserDb);
					_appPlatformUser.Save();
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
								string msg = HandleMessageJson(tempJson, senderId);
								_lstBotReplyResponse.Add(msg);
								//await SendMessageTask(tempJson, sender);
							}
						}
					}
					if (cardDb.CardStepID != null && cardDb.IsConditionWithInputText == false)
					{
						plUserDb.CardStepPattern = "postback_card_" + cardDb.CardStepID;
						return await MessageResponse(plUserDb.CardStepPattern, senderId, botId, true);
					}
					//_lstBotReplyResponse = new List<string>();
					return await Task.FromResult<List<string>>(_lstBotReplyResponse);
				}
				if (text.Contains(_contactAdmin))//chat admin
				{
					plUserDb.IsHaveCardCondition = false;
					plUserDb.CardConditionPattern = "";
					plUserDb.CardStepPattern = "";
					plUserDb.IsHaveSetAttributeSystem = false;

					_appPlatformUser.Update(plUserDb);
					_appPlatformUser.Save();

					string strTempPostbackContactAdmin = aimlBotResult.SubQueries[0].Template;
					bool isPostbackContactAdmin = Regex.Match(strTempPostbackContactAdmin, "<template><srai>postback_card_(\\d+)</srai></template>").Success;
					if (isPostbackContactAdmin)
					{
						strTempPostbackContactAdmin = Regex.Replace(strTempPostbackContactAdmin, @"<(.|\n)*?>", "").Trim();
						var cardDb = _cardService.GetCardByPattern(strTempPostbackContactAdmin.Replace(".", String.Empty));
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
									string msg = HandleMessageJson(tempJson, senderId);
									_lstBotReplyResponse.Add(msg);
									//await SendMessageTask(tempJson, sender);
								}
								return await Task.FromResult<List<string>>(_lstBotReplyResponse);
								//return new HttpResponseMessage(HttpStatusCode.OK);
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
						if (plUserDb.Age == 0)//nếu thông tin tuổi chưa có trả về thẻ hỏi thông tin
						{
							return await MessageResponse("postback_card_8917", senderId, botId);
							//return await ExcuteMessage("postback_card_8917", sender, botId);
						}
					}

					if (!String.IsNullOrEmpty(cardDb.AttributeSystemName))
					{
						plUserDb.IsHaveSetAttributeSystem = true;
						plUserDb.AttributeName = cardDb.AttributeSystemName;
					}
					else
					{
						plUserDb.IsHaveSetAttributeSystem = false;
						plUserDb.AttributeName = "";
					}

					if (cardDb.CardStepID != null)
					{
						plUserDb.CardStepPattern = "postback_card_" + cardDb.CardStepID;
						if (cardDb.IsConditionWithInputText)// yêu cầu nhập text để chuyển sang card step
						{
							plUserDb.IsConditionWithInputText = true;
							plUserDb.CardConditionWithInputTextPattern = plUserDb.CardStepPattern;
						}
						else
						{
							plUserDb.IsConditionWithInputText = false;
							plUserDb.CardConditionWithInputTextPattern = "";
						}
					}
					else
					{
						plUserDb.CardStepPattern = "";
					}

					if (cardDb.IsHaveCondition)
					{
						plUserDb.IsHaveCardCondition = true;
						plUserDb.CardConditionPattern = text.Replace(".", String.Empty);
					}
					else
					{
						plUserDb.IsHaveCardCondition = false;
						plUserDb.CardConditionPattern = "";
					}

					if (cardDb.IsConditionWithAreaButton)
					{
						plUserDb.IsConditionWithAreaButton = true;
						plUserDb.CardConditionAreaButtonPattern = text.Replace(".", String.Empty);
					}
					else
					{
						plUserDb.IsConditionWithAreaButton = false;
						plUserDb.CardConditionAreaButtonPattern = "";
					}
					_appPlatformUser.Update(plUserDb);
					_appPlatformUser.Save();
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
								string msg = HandleMessageJson(tempJson, senderId);
								_lstBotReplyResponse.Add(msg);
								//await SendMessageTask(tempJson, sender);
							}
						}
					}
					if (cardDb.CardStepID != null && cardDb.IsConditionWithInputText == false)
					{
						plUserDb.CardStepPattern = "postback_card_" + cardDb.CardStepID;
						return await MessageResponse(plUserDb.CardStepPattern, senderId, botId, true);
						//return await ExcuteMessage(fbUserDb.CardStepPattern, sender, botId);
					}

					//_lstBotReplyResponse = new List<string>();
					return await Task.FromResult<List<string>>(_lstBotReplyResponse);
					//return new HttpResponseMessage(HttpStatusCode.OK);
				}

				//trường hợp trả về câu hỏi random chứa postback
				bool isPostbackAnswer = Regex.Match(strTempPostback, "<template><srai>postback_answer_(\\d+)</srai></template>").Success;
				if (isPostbackAnswer)
				{
					if (result.Contains("postback_card"))
					{
						var cardDb = _cardService.GetCardByPattern(result.Replace(".", String.Empty));
						if (!String.IsNullOrEmpty(cardDb.AttributeSystemName))
						{
							plUserDb.IsHaveSetAttributeSystem = true;
							plUserDb.AttributeName = cardDb.AttributeSystemName;
						}
						else
						{
							plUserDb.IsHaveSetAttributeSystem = false;
							plUserDb.AttributeName = "";
						}

						if (cardDb.CardStepID != null)
						{
							plUserDb.CardStepPattern = "postback_card_" + cardDb.CardStepID;
							if (cardDb.IsConditionWithInputText)// yêu cầu nhập text để chuyển sang card step
							{
								plUserDb.IsConditionWithInputText = true;
								plUserDb.CardConditionWithInputTextPattern = plUserDb.CardStepPattern;
							}
							else
							{
								plUserDb.IsConditionWithInputText = false;
								plUserDb.CardConditionWithInputTextPattern = "";
							}
						}
						else
						{
							plUserDb.CardStepPattern = "";
						}

						if (cardDb.IsHaveCondition)
						{
							plUserDb.IsHaveCardCondition = true;
							plUserDb.CardConditionPattern = text.Replace(".", String.Empty);
						}
						else
						{
							plUserDb.IsHaveCardCondition = false;
							plUserDb.CardConditionPattern = "";
						}

						if (cardDb.IsConditionWithAreaButton)
						{
							plUserDb.IsConditionWithAreaButton = true;
							plUserDb.CardConditionAreaButtonPattern = text.Replace(".", String.Empty);
						}
						else
						{
							plUserDb.IsConditionWithAreaButton = false;
							plUserDb.CardConditionAreaButtonPattern = "";
						}
						_appPlatformUser.Update(plUserDb);
						_appPlatformUser.Save();
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
									string msg = HandleMessageJson(tempJson, senderId);
									_lstBotReplyResponse.Add(msg);
									//await SendMessageTask(tempJson, sender);
								}
							}
						}
						if (cardDb.CardStepID != null && cardDb.IsConditionWithInputText == false)
						{
							//plUserDb.CardStepPattern = "postback_card_" + cardDb.CardStepID;
							//return await ExcuteMessage(fbUserDb.CardStepPattern, sender, botId);
							plUserDb.CardStepPattern = "postback_card_" + cardDb.CardStepID;
							return await MessageResponse(plUserDb.CardStepPattern, senderId, botId, true);
						}

						//_lstBotReplyResponse = new List<string>();
						return await Task.FromResult<List<string>>(_lstBotReplyResponse);
						//return new HttpResponseMessage(HttpStatusCode.OK);
					}
				}
				// trả lời text bình thường
				string msgText = HandleMessageJson(FacebookTemplate.GetMessageTemplateText(result, senderId).ToString(), senderId);
				_lstBotReplyResponse.Add(msgText);
				return await Task.FromResult<List<string>>(_lstBotReplyResponse);
				//return await SendMessage(FacebookTemplate.GetMessageTemplateText(result, sender));
			}
			catch (Exception ex)
			{
				_lstBotReplyResponse = new List<string>();
			}
			return await Task.FromResult<List<string>>(_lstBotReplyResponse);
		}

		private string HandleMessageJson(string msgJson, string senderId)
		{
			msgJson = msgJson.Replace("{{senderId}}", senderId);
			//msgJson = Regex.Replace(msgJson, "File/", Domain + "File/");
			msgJson = Regex.Replace(msgJson, "<br />", "\\n");
			msgJson = Regex.Replace(msgJson, "<br/>", "\\n");
			msgJson = Regex.Replace(msgJson, @"\\n\\n", "\\n");
			msgJson = Regex.Replace(msgJson, @"\\n\\r\\n", "\\n");
			if (msgJson.Contains("{{"))
			{
				if (_dicAttributeUser != null && _dicAttributeUser.Count() != 0)
				{
					foreach (var item in _dicAttributeUser)
					{
						string val = String.IsNullOrEmpty(item.Value) == true ? "N/A" : item.Value;
						msgJson = msgJson.Replace("{{" + item.Key + "}}", val);
					}
				}
			}
			return msgJson;
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

		private void AddAttributeDefault(string userId, int botId, string key, string value)
		{
			AttributePlatformUser attPlatformUser = new AttributePlatformUser();
			attPlatformUser.UserID = userId;
			attPlatformUser.BotID = botId;
			attPlatformUser.AttributeKey = key;
			attPlatformUser.AttributeValue = value;
			attPlatformUser.TypeDevice = "livechat";
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
			hisDb.Type = "livechat";
			_historyService.Create(hisDb);
			_historyService.Save();
		}

		public class Message
		{
			public string senderId { set; get; }//K2542621755855091
			public string botId { set; get; }
			public string text { set; get; }
            public long channelGroupId { set; get; }
            public long threadId { set; get; }
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
	}
}
