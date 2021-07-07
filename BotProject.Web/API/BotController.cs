using BotProject.Web.Infrastructure.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using BotProject.Service;
using AutoMapper;
using BotProject.Model.Models;
using BotProject.Web.Models;
using BotProject.Web.Infrastructure.Extensions;
using System.Configuration;
using BotProject.Common;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using BotProject.Web.Infrastructure.Log4Net;
using System.Text;
using System.Text.RegularExpressions;
using BotProject.Service.Livechat;
using System.Xml;
using System.Web;
using System.Net.Http.Headers;

namespace BotProject.Web.API
{
	[RoutePrefix("api/bot")] 
	public class BotController : ApiControllerBase
	{
		private IBotService _botService;
        private ICardService _cardService;
        private IGroupCardService _groupCardService;
        private ISettingService _settingService;
        private ICommonCardService _commonCardService;
        private IQnAService _qnaService;
        private IModuleService _moduleService;
        private IAttributeSystemService _attributeSystemService;
        private IAIMLFileService _aimlFileService;
		private IChannelService _channelService;
        private ApiQnaNLRService _apiNLR;


        private string[] _userSayStart = new string[]
        {
                    CommonConstants.UserSay_IsStartDefault,
                    CommonConstants.UserSay_IsStartFirst,
                    CommonConstants.UserSay_IsStartLast,
                    CommonConstants.UserSay_IsStartDouble
        };

        public BotController(IErrorService errorService,
            IBotService botService,
            ICardService cardService,
            ISettingService settingService,
            ICommonCardService commonCardService,
            IGroupCardService groupCardService,
            IQnAService qnaService,
            IModuleService moduleService,
            IAIMLFileService aimlFileService,
            IAttributeSystemService attributeSystemService,
			IChannelService channelService) : base(errorService)
		{
			_botService = botService;
            _settingService = settingService;
            _cardService = cardService;
            _commonCardService = commonCardService;
            _qnaService = qnaService;
            _groupCardService = groupCardService;
            _moduleService = moduleService;
            _attributeSystemService = attributeSystemService;
            _aimlFileService = aimlFileService;
			_channelService = channelService;

            _apiNLR = new ApiQnaNLRService();


        }       

        [Route("getall")]
		[HttpGet]
		public HttpResponseMessage GetBotByUserID(HttpRequestMessage request, string userID)
		{
			return CreateHttpResponse(request, () =>
			{
				HttpResponseMessage response;
				if (String.IsNullOrEmpty(userID))
				{
					response = request.CreateResponse(HttpStatusCode.NotFound);
					return response;
				}

				var lstBot = _botService.GetListBotByUserID(userID);
				var lstBotVm = Mapper.Map<IEnumerable<Bot>, IEnumerable<BotViewModel>>(lstBot);
                if (lstBotVm.Any())
                {
                    foreach (var item in lstBotVm)
                    {
                        var formQnaDb = _qnaService.GetListFormByBotID(item.ID);
                        item.FormQuestionAnswers = Mapper.Map<IEnumerable<FormQuestionAnswer>, IEnumerable<FormQuestionAnswerViewModel>>(formQnaDb);
                    }
                }
                response = request.CreateResponse(HttpStatusCode.OK, lstBotVm);

				return response;
			});
		}

		[Route("getBotActiveLiveChat")]
		[HttpPost]
		public HttpResponseMessage GetBotActiveByChannelGroupID(HttpRequestMessage request, JObject jsonData)
		{
			return CreateHttpResponse(request, () =>
			{
				HttpResponseMessage response;
				dynamic json = jsonData;
				int channelGroupId = json.channelGroupId;

				var channelGroup = _channelService.GetChannelGroupById(channelGroupId);
				if(channelGroup != null)
				{
					response = request.CreateResponse(HttpStatusCode.OK, new { status = true, botId = channelGroup.BotID });
					return response;
				}

				response = request.CreateResponse(HttpStatusCode.OK, new { status = false});
				return response;
			});
		}

		[Route("getById")]
        [HttpGet]
        public HttpResponseMessage GetBotById(HttpRequestMessage request, int botId)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response;
                if (botId == 0)
                {
                    response = request.CreateResponse(HttpStatusCode.NotFound);
                    return response;
                }
                var botDb = _botService.GetByID(botId);
                var lstBotVm = Mapper.Map<Bot, BotViewModel>(botDb);
                var formQnaDb = _qnaService.GetListFormByBotID(lstBotVm.ID);
                lstBotVm.FormQuestionAnswers = Mapper.Map<IEnumerable<FormQuestionAnswer>, IEnumerable<FormQuestionAnswerViewModel>>(formQnaDb);
                response = request.CreateResponse(HttpStatusCode.OK, lstBotVm);
                return response;
            });
        }

        [Route("create")]
        [HttpPost]
        public HttpResponseMessage Create(HttpRequestMessage request, BotViewModel botVm)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                if (String.IsNullOrEmpty(botVm.UserID))
                {
                    response = request.CreateResponse(HttpStatusCode.NotFound);
                    return response;
                }
                Bot botDb = new Bot();
                botDb.UpdateBot(botVm);
                var botReturn = _botService.Create(ref botDb);
				try
				{
					// create file bot aiml
					//string pathFolderAIML = ConfigurationManager.AppSettings["AIMLPath"];
                    string pathFolderAIML = PathServer.PathAIML;
                    string nameFolderAIML = "User_" +botVm.UserID + "_BotID_" + botReturn.ID;
					string pathString = System.IO.Path.Combine(pathFolderAIML, nameFolderAIML);
					System.IO.Directory.CreateDirectory(pathString);
				}
				catch (Exception ex)
				{

				}
                Setting settingDb = new Setting();
                settingDb.BotID = botDb.ID;
                settingDb.Color = "rgb(75, 90, 148);";
                settingDb.UserID = botVm.UserID;
                settingDb.Logo = "assets/images/user_bot.jpg";
                _settingService.Create(settingDb);
                _settingService.Save();
                var reponseData = Mapper.Map<Bot, BotViewModel>(botReturn);
                response = request.CreateResponse(HttpStatusCode.OK, reponseData);
                return response;
            });
        }


        [Route("deletebot")]
        [HttpPost]
        public HttpResponseMessage DeleteBot(HttpRequestMessage request, JObject jsonData)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                dynamic json = jsonData;
                int botID = json.botId;

                var botDb = _botService.GetByID(botID);
                botDb.Status = false;
                _botService.Update(botDb);
                _botService.Save();

                //delete database
                //var lstCard = _cardService.GetListCardByBotID(botID).ToList();
                //if (lstCard.Count() != 0)
                //{
                //    foreach (var item in lstCard)
                //    {
                //        _commonCardService.DeleteCard(item.ID);
                //    }
                //}

                //var lstBotQnAnswer = _qnaService.GetListBotQnAnswerByBotID(botID);
                //if (lstBotQnAnswer != null && lstBotQnAnswer.Count() != 0)
                //{
                //    foreach (var botQnAnswer in lstBotQnAnswer)
                //    {
                //        var lstQuesGroup = _qnaService.GetListQuestionGroupByBotQnAnswerID(botQnAnswer.ID);
                //        if (lstQuesGroup != null && lstQuesGroup.Count() != 0)
                //        {
                //            foreach (var quesGroup in lstQuesGroup)
                //            {
                //                _qnaService.DeleteQuesByQuestionGroup(quesGroup.ID);
                //                _qnaService.DeleteAnswerByQuestionGroup(quesGroup.ID);

                //            }
                //        }
                //    }
                //}
                response = request.CreateResponse(HttpStatusCode.OK, true);
                return response;
            });
        }

        [Route("training")]
        [HttpPost]
        public HttpResponseMessage TrainingBot(HttpRequestMessage request, JObject jsonData)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                try
                {
                    dynamic json = jsonData;
                    int botID = json.botId;

                    var lstAIMLBotFiles = _aimlFileService.GetActiveByBotId(botID);

                    var aimlBot = new AIMLbot.Bot();
                    if (lstAIMLBotFiles.Any())
                    {
                        foreach (var item in lstAIMLBotFiles)
                        {
                            try
                            {
                                XmlDocument doc = new XmlDocument();
                                // Encode các dấu &,<,>,\ trong url tới xml
                                item.Content = Regex.Replace(item.Content, "<url>(.*?)</url>", m => String.Format("<url><![CDATA[{0}]]></url>", HttpUtility.HtmlEncode(m.Groups[1].Value)));

                                doc.LoadXml(item.Content);
                                aimlBot.loadAIMLFromXML(doc, item.Src);

                            }
                            catch (Exception ex)
                            {
                                string msg = item.Content + ex.Message;
                                BotLog.Info(msg);
                                response = request.CreateResponse(HttpStatusCode.OK, new { status = false, message = "Vui lòng kiểm tra tập tin tri thức, dữ liệu không được trống" });
                                return response;
                            }
                        }
                        string pathFolderAIML2Graphmaster = ConfigurationManager.AppSettings["AIML2GraphmasterPath"] + "BotID_" + botID.ToString() + ".bin";
                        aimlBot.saveToBinaryFile(pathFolderAIML2Graphmaster);
                    }
                }
                catch(Exception ex)
                {
                    BotLog.Info(ex.Message);
                    response = request.CreateResponse(HttpStatusCode.OK, new { status = false, message = "Huấn luyện không thành công" });
                    return response;
                }
                
                response = request.CreateResponse(HttpStatusCode.OK, new {status = true, message = "Huấn luyện thành công" });
                return response;
            });
        }

        /// <summary>
        /// Sao chép dữ liệu bot
        /// </summary>
        /// <param name="request"></param>
        /// <param name="jsonData"></param>
        /// <returns></returns>
        [Route("clone")]
        [HttpPost]
        public async Task<HttpResponseMessage> CloneBot(HttpRequestMessage request, JObject jsonData)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                dynamic json = jsonData;
                int botParentID = json.botId;
                string botName = json.botName;
                string userId = json.userId;
                string botAlias = json.botAlias;
                // create bot
                Bot botdB = new Bot();
                try
                {
                    botdB = _botService.GetByID(botParentID);
                    botdB.Alias = botAlias;
                    botdB.Name = botName;
                    botdB.UserID = userId;
                    botdB.IsTemplate = false;
                    botdB.BotCloneParentID = botParentID;

                    _botService.Create(ref botdB);
                    _botService.Save();

                    Setting settingDb = new Setting();
                    settingDb = _settingService.GetSettingByBotID(botParentID);
                    settingDb.BotID = botdB.ID;
                    //settingDb.Color = settingDb.Color;//"rgb(75, 90, 148);";
                    settingDb.UserID = botdB.UserID;
                    settingDb.FormName = botName;
                    settingDb.PathCssCustom = "";
                    settingDb.FacebookAppSecrect = "";
                    settingDb.FacebookPageToken = "";
                    settingDb.ZaloAppSecrect = "";
                    settingDb.ZaloPageToken = "";
                    settingDb.ZaloQRCode = "";
                    _settingService.Create(settingDb);
                    _settingService.Save();

                    //  module 
                    var lstModule = _moduleService.GetAllModuleByBotID(botParentID).ToList();
                    if (lstModule != null && lstModule.Count() != 0)
                    {
                        foreach (var module in lstModule)
                        {
                            Module moduleDb = new Module();
                            moduleDb = module;
                            moduleDb.BotID = botdB.ID;
                            _moduleService.Create(moduleDb);
                            _moduleService.Save();
                        }
                    }

                    // attribute setting
                    var lstAttributeSystem = _attributeSystemService.GetListAttributeSystemByBotId(botParentID).ToList();
                    if (lstAttributeSystem != null && lstAttributeSystem.Count() != 0)
                    {
                        foreach (var attribute in lstAttributeSystem)
                        {
                            AttributeSystem attSystemDb = new AttributeSystem();
                            attSystemDb = attribute;
                            attSystemDb.BotID = botdB.ID;
                            _attributeSystemService.Create(attSystemDb);
                            _attributeSystemService.Save();
                        }
                    }

                    // get list groupcard
                    var lstGroupCard = _groupCardService.GetListGroupCardByBotID(botParentID).ToList();
                    if (lstGroupCard != null && lstGroupCard.Count() != 0)
                    {
                        foreach (var groupCard in lstGroupCard)
                        {
                            int groupCardCloneID = groupCard.ID;

                            GroupCard groupCardDb = new GroupCard();
                            groupCardDb = groupCard;
                            groupCardDb.BotID = botdB.ID;
                            _groupCardService.Create(groupCardDb);
                            _groupCardService.Save();

                            // get list card
                            var lstCard = _cardService.GetListCardByGroupCardID(groupCardCloneID).ToList();
                            if (lstCard != null && lstCard.Count() != 0)
                            {
                                foreach (var card in lstCard)
                                {
                                    int cardCloneID = card.ID;
                                    // get full detail card
                                    Card cardDb = new Card();
                                    cardDb = _commonCardService.GetFullDetailCard(cardCloneID);
                                    cardDb.BotID = botdB.ID;
                                    cardDb.GroupCardID = groupCardDb.ID;
                                    cardDb.CardCloneParentID = cardCloneID;
                                    _cardService.Create(cardDb);
                                    _cardService.Save();

                                    // Save Template generic group
                                    var lstTemplateGenericGroup = card.TemplateGenericGroups.ToList();
                                    if (lstTemplateGenericGroup != null && lstTemplateGenericGroup.Count() != 0)
                                    {
                                        foreach (var templateGenericGroup in lstTemplateGenericGroup)
                                        {
                                            int templateGenericGroupCloneID = templateGenericGroup.ID;
                                            TemplateGenericGroup tempGenericGroupDb = new TemplateGenericGroup();
                                            tempGenericGroupDb = templateGenericGroup;
                                            tempGenericGroupDb.CardID = cardDb.ID;
                                            _commonCardService.AddTempGnrGroup(tempGenericGroupDb);
                                            _commonCardService.Save();
                                            var lstTemplateGenericItems = templateGenericGroup.TemplateGenericItems.ToList();
                                            if (lstTemplateGenericItems != null && lstTemplateGenericItems.Count() != 0)
                                            {
                                                foreach (var templateGenericItem in lstTemplateGenericItems)
                                                {
                                                    int templateGenericItemCloneID = templateGenericItem.ID;
                                                    TemplateGenericItem templateGenericItemDb = new TemplateGenericItem();
                                                    templateGenericItemDb = templateGenericItem;
                                                    templateGenericItemDb.CardID = cardDb.ID;
                                                    templateGenericItemDb.TempGnrGroupID = tempGenericGroupDb.ID;
                                                    _commonCardService.AddTempGnrItem(templateGenericItemDb);
                                                    _commonCardService.Save();

                                                    var lstButtonLink = templateGenericItem.ButtonLinks.ToList();
                                                    if (lstButtonLink != null && lstButtonLink.Count() != 0)
                                                    {
                                                        foreach (var buttonLink in lstButtonLink)
                                                        {
                                                            ButtonLink btnLinkDb = new ButtonLink();
                                                            btnLinkDb = buttonLink;
                                                            btnLinkDb.TempGnrItemID = templateGenericItemDb.ID;
                                                            btnLinkDb.CardID = cardDb.ID;
                                                            _commonCardService.AddButtonLink(btnLinkDb);
                                                            _commonCardService.Save();
                                                        }
                                                    }

                                                    var lstButtonPostback = templateGenericItem.ButtonPostbacks.ToList();
                                                    if (lstButtonPostback != null && lstButtonPostback.Count() != 0)
                                                    {
                                                        foreach (var buttonPostback in lstButtonPostback)
                                                        {
                                                            ButtonPostback btnPostbackDb = new ButtonPostback();
                                                            btnPostbackDb = buttonPostback;
                                                            btnPostbackDb.TempGnrItemID = templateGenericItemDb.ID;
                                                            btnPostbackDb.CardID = cardDb.ID;
                                                            // so sánh cardpayload với cardcloneparentid để update lại
                                                            _commonCardService.AddButtonPostback(btnPostbackDb);
                                                            _commonCardService.Save();
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    // Save Template text
                                    var lstTemplateText = card.TemplateTexts.ToList();
                                    if (lstTemplateText != null && lstTemplateText.Count() != 0)
                                    {
                                        foreach (var templateText in lstTemplateText)
                                        {
                                            int templateTextCloneID = templateText.ID;
                                            TemplateText templateTextDb = new TemplateText();
                                            templateTextDb = templateText;
                                            templateTextDb.CardID = cardDb.ID;
                                            _commonCardService.AddTempText(templateTextDb);
                                            _commonCardService.Save();

                                            var lstButtonLink = templateText.ButtonLinks.ToList();
                                            if (lstButtonLink != null && lstButtonLink.Count() != 0)
                                            {
                                                foreach (var buttonLink in lstButtonLink)
                                                {
                                                    ButtonLink btnLinkDb = new ButtonLink();
                                                    btnLinkDb = buttonLink;
                                                    btnLinkDb.TempTxtID = templateTextDb.ID;
                                                    btnLinkDb.CardID = cardDb.ID;
                                                    _commonCardService.AddButtonLink(btnLinkDb);
                                                    _commonCardService.Save();
                                                }
                                            }

                                            var lstButtonPostback = templateTextDb.ButtonPostbacks.ToList();
                                            if (lstButtonPostback != null && lstButtonPostback.Count() != 0)
                                            {
                                                foreach (var buttonPostback in lstButtonPostback)
                                                {
                                                    ButtonPostback btnPostbackDb = new ButtonPostback();
                                                    btnPostbackDb = buttonPostback;
                                                    btnPostbackDb.TempTxtID = templateTextDb.ID;
                                                    btnPostbackDb.CardID = cardDb.ID;
                                                    // so sánh cardpayload với cardcloneparentid để update lại
                                                    _commonCardService.AddButtonPostback(btnPostbackDb);
                                                    _commonCardService.Save();
                                                }
                                            }
                                        }
                                    }

                                    // Get list module of card
                                    //var lstModuleFollowCard = card.ModuleFollowCards;
                                    //if(lstModuleFollowCard != null && lstModuleFollowCard.Count() != 0)
                                    //{
                                    //    foreach(var moduleFollowCard in lstModuleFollowCard)
                                    //    {
                                    //        ModuleFollowCard mdFollowCard = new ModuleFollowCard();
                                    //        mdFollowCard.BotID = botdB.ID;
                                    //        mdFollowCard.CardID = cardDb.ID;
                                    //    }
                                    //}

                                    // Image
                                    var lstImage = card.Images.ToList();
                                    if (lstImage != null && lstImage.Count() != 0)
                                    {
                                        foreach (var image in lstImage)
                                        {
                                            Image imageDb = new Image();
                                            imageDb = image;
                                            imageDb.BotID = botdB.ID;
                                            imageDb.CardID = cardDb.ID;
                                            _commonCardService.AddImage(imageDb);
                                            _commonCardService.Save();
                                        }
                                    }

                                    // Quickreply
                                    var lstQuickReply = card.QuickReplys.ToList();
                                    if (lstQuickReply != null && lstQuickReply.Count() != 0)
                                    {
                                        foreach (var quickReply in lstQuickReply)
                                        {
                                            Model.Models.QuickReply quickReplyDb = new Model.Models.QuickReply();
                                            quickReplyDb = quickReply;
                                            quickReplyDb.CardID = cardDb.ID;
                                            _commonCardService.AddQuickReply(quickReplyDb);
                                            _commonCardService.Save();
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Get list new card and get list button and quickreply update off new Bot
                    var lstNewCard = _cardService.GetListCardByBotID(botdB.ID).ToList();
                    if (lstNewCard != null && lstNewCard.Count() != 0)
                    {
                        foreach (var newCard in lstNewCard)
                        {
                            Card newCardDb = new Card();
                            newCardDb = newCard;
                            if (newCardDb.CardStepID != null)
                            {
                                newCardDb.CardStepID = lstNewCard.SingleOrDefault(x => x.CardCloneParentID == newCardDb.CardStepID).ID;
                            }
                            newCardDb.PatternText = "postback_card_" + newCardDb.ID;
                            _cardService.Update(newCardDb);
                            _cardService.Save();

                            var lstButtonPostback = _commonCardService.GetListButtonPostbackByCardID(newCard.ID).ToList();
                            if (lstButtonPostback != null && lstButtonPostback.Count() != 0)
                            {
                                foreach (var buttonPostback in lstButtonPostback)
                                {
                                    ButtonPostback btnPostbackDb = new ButtonPostback();
                                    btnPostbackDb = buttonPostback;
                                    // Tìm CardPayloadID của ID card mới tạo tương ứng với
                                    // cardpayload của card clone trước đó
                                    int newCardPayloadID = lstNewCard.SingleOrDefault(x => x.CardCloneParentID == btnPostbackDb.CardPayloadID).ID;
                                    btnPostbackDb.CardPayloadID = newCardPayloadID;
                                    btnPostbackDb.Payload = "postback_card_" + newCardPayloadID;
                                    _commonCardService.UpdateButtonPostback(btnPostbackDb);
                                    _commonCardService.Save();
                                }
                            }
                            var lstQuickReply = _commonCardService.GetListQuickReplyByCardID(newCard.ID).ToList();
                            if (lstQuickReply != null && lstQuickReply.Count() != 0)
                            {
                                foreach (var quickReply in lstQuickReply)
                                {
                                    Model.Models.QuickReply quickReplyDb = new Model.Models.QuickReply();
                                    quickReplyDb = quickReply;
                                    if(quickReplyDb.CardPayloadID != null)
                                    {
                                        int newCardPayloadID = lstNewCard.SingleOrDefault(x => x.CardCloneParentID == quickReplyDb.CardPayloadID).ID;
                                        quickReplyDb.CardPayloadID = newCardPayloadID;
                                        quickReplyDb.Payload = "postback_card_" + newCardPayloadID;
                                    }

                                    _commonCardService.UpdateQuickReply(quickReplyDb);
                                    _commonCardService.Save();
                                }
                            }
                        }

                        // Create new file aiml card
                        foreach (var card in lstNewCard)
                        {
                            CreateAIMLFileByCardID(card.ID, botdB.UserID);
                        }
                    }

                    // update setting card getstarted
                    if (settingDb.CardID != null)
                    {
                        int settingCardID = lstNewCard.SingleOrDefault(x => x.CardCloneParentID == settingDb.CardID).ID;
                        settingDb.CardID = settingCardID;
                        _settingService.Update(settingDb);
                        _settingService.Save();
                    }

                    // get list qna
                    var lstFormQnA = _qnaService.GetListFormByBotID(botParentID).ToList();
                    if(lstFormQnA.Count() != 0)
                    {
                        foreach(var formQnA in lstFormQnA)
                        {
                            int formQnACloneID = formQnA.ID;

                            FormQuestionAnswer formQnaDb = new FormQuestionAnswer();
                            formQnaDb = formQnA;
                            formQnaDb.BotID = botdB.ID;
                            _qnaService.AddFormQnAnswer(ref formQnaDb);
                            _qnaService.Save();

                            var lstQuestionGroup = _qnaService.GetListQuestionGroupByFormQnAnswerPagination(formQnACloneID,1,999999999).ToList();
                            if(lstQuestionGroup.Count() != 0)
                            {
                                foreach(var questionGroup in lstQuestionGroup)
                                {
                                    int questionGroupCloneID = questionGroup.ID;
                                    QuestionGroup questionGroupDb = new QuestionGroup();

                                    questionGroupDb.FormQuestionAnswerID = formQnaDb.ID;
                                    questionGroupDb.BotID = botdB.ID;
                                    questionGroupDb.CreatedDate = questionGroup.CreatedDate;
                                    questionGroupDb.IsKeyword = questionGroup.IsKeyword;
                                    questionGroupDb.Index = questionGroup.Index;
                                    _qnaService.AddQuesGroup(questionGroupDb);
                                    _qnaService.Save();

                                    var lstQuestion = _qnaService.GetListQuestionByGroupID(questionGroupCloneID).ToList();
                                    if(lstQuestion.Count() != 0)
                                    {
                                        foreach(var question in lstQuestion)
                                        {
                                            Question questionDb = new Question();
                                            questionDb = question;
                                            questionDb.QuestionGroupID = questionGroupDb.ID;
                                            _qnaService.AddQuestion(questionDb);
                                            _qnaService.Save();
                                        }
                                    }

                                    var lstAnswer = _qnaService.GetListAnswerByGroupID(questionGroupCloneID).ToList();
                                    if (lstAnswer.Count() != 0)
                                    {
                                        foreach (var answer in lstAnswer)
                                        {
                                            Answer answerDb = new Answer();
                                            answerDb = answer;
                                            if(answerDb.CardID != null)
                                            {
                                                answerDb.CardID = lstNewCard.SingleOrDefault(x => x.CardCloneParentID == answerDb.CardID).ID;
                                                answerDb.CardPayload = "postback_card_" + answerDb.CardID;
                                            }

                                            answerDb.QuestionGroupID = questionGroupDb.ID;
                                            _qnaService.AddAnswer(answerDb);
                                            _qnaService.Save();
                                        }
                                    }
                                }
                            }
                        }
                    }

                    //Create new file aiml formQna
                    var lstNewFormQna = _qnaService.GetListFormByBotID(botdB.ID).ToList();
                    if(lstNewFormQna.Count() != 0)
                    {
                        foreach(var formQnA in lstNewFormQna)
                        {
                            CreateAIMLFileFormQnaByCardID(formQnA.ID, formQnA.Name, botdB.UserID, botdB.ID);                          
                        }
                    }
                    response = request.CreateResponse(HttpStatusCode.OK, new {status = true, data = botdB });
                }
                catch(Exception ex)
                {
                    BotLog.Error(ex.StackTrace + " " + ex.InnerException.Message + ex.Message);
                    botdB.Status = false;
                    _botService.Update(botdB);
                    _botService.Save();
                    return request.CreateResponse(HttpStatusCode.OK, new { status = false, data = ex.Message});
                }
                return response;
            });
        }

        public void CreateAIMLFileByCardID(int cardId, string userId)
        {
            var card = _commonCardService.GetFullDetailCard(cardId);
            StringBuilder sbAIML = new StringBuilder();
            try
            {
                //StreamWriter sw = new StreamWriter(pathString, true, Encoding.UTF8);
                sbAIML.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                sbAIML.AppendLine("<aiml>");
                sbAIML.AppendLine("<category>");
                sbAIML.AppendLine("<pattern>" + card.PatternText + "</pattern>");
                sbAIML.AppendLine("<template>");

                //sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                //sw.WriteLine("<aiml>");
                //sw.WriteLine("<category>");
                //sw.WriteLine("<pattern>"+card.PatternText+"</pattern>");
                //sw.WriteLine("<template>");
                if (card.TemplateTexts != null && card.TemplateTexts.Count() != 0)
                {
                    var lstTemplateText = card.TemplateTexts;
                    foreach (var item in lstTemplateText)
                    {
                        // text
                        string contentText = Regex.Replace(item.Text, "&", "và");
                        sbAIML.AppendLine("" + contentText + "");

                        //sw.WriteLine(""+item.Text+"");
                        if (item.ButtonPostbacks != null && item.ButtonPostbacks.Count() != 0)
                        {
                            var lstButtonPostbacks = item.ButtonPostbacks;
                            foreach (var itemBtnPostback in lstButtonPostbacks)
                            {
                                //sw.WriteLine("<button>");
                                //sw.WriteLine("<text>"+itemBtnPostback.Title+"</text>");
                                //sw.WriteLine("<menu>" + itemBtnPostback.Payload + "</menu>");
                                //sw.WriteLine("</button>");

                                sbAIML.AppendLine("<button>");
                                sbAIML.AppendLine("<text>" + HelperMethods.EscapeXml(itemBtnPostback.Title) + "</text>");
                                sbAIML.AppendLine("<menu>" + itemBtnPostback.Payload + "</menu>");
                                sbAIML.AppendLine("</button>");
                            }
                        }
                        if (item.ButtonLinks != null && item.ButtonLinks.Count() != 0)
                        {
                            var lstButtonLinks = item.ButtonLinks;
                            foreach (var itemBtnLink in lstButtonLinks)
                            {
                                //sw.WriteLine("<button>");
                                //sw.WriteLine("<text>" + itemBtnLink.Title + "</text>");
                                //sw.WriteLine("<url>" + itemBtnLink.Url + "</url>");
                                //sw.WriteLine("</button>");

                                sbAIML.AppendLine("<button>");
                                sbAIML.AppendLine("<text>" + HelperMethods.EscapeXml(itemBtnLink.Title) + "</text>");
                                sbAIML.AppendLine("<url>" + itemBtnLink.Url + "</url>");
                                sbAIML.AppendLine("</button>");
                            }
                        }
                        if (item.ButtonModules != null && item.ButtonModules.Count() != 0)
                        {
                            var lstButtonModules = item.ButtonModules;
                            foreach (var itemBtnModule in lstButtonModules)
                            {
                                //sw.WriteLine("<button>");
                                //sw.WriteLine("<text>" + itemBtnModule.Title + "</text>");

                                sbAIML.AppendLine("<button>");
                                sbAIML.AppendLine("<text>" + HelperMethods.EscapeXml(itemBtnModule.Title) + "</text>");
                                if (itemBtnModule.ModuleKnowledgeID != null && itemBtnModule.ModuleKnowledgeID != 0)
                                {
                                    sbAIML.AppendLine("<module>" + itemBtnModule.Payload + "_" + itemBtnModule.ModuleKnowledgeID + "</module>");
                                    //sw.WriteLine("<module>" + itemBtnModule.Payload + "_" + itemBtnModule.ModuleKnowledgeID + "</module>");
                                }
                                else if (itemBtnModule.MdSearchID != null && itemBtnModule.MdSearchID != 0)
                                {
                                    sbAIML.AppendLine("<module>" + itemBtnModule.Payload + "_" + itemBtnModule.MdSearchID + "</module>");
                                    //sw.WriteLine("<module>" + itemBtnModule.Payload + "_" + itemBtnModule.MdSearchID + "</module>");
                                }
                                else if (itemBtnModule.MdVoucherID != null && itemBtnModule.MdVoucherID != 0)
                                {
                                    sbAIML.AppendLine("<module>" + itemBtnModule.Payload + "_" + itemBtnModule.MdVoucherID + "</module>");
                                    //sw.WriteLine("<module>" + itemBtnModule.Payload + "_" + itemBtnModule.MdSearchID + "</module>");
                                }
                                else
                                {
                                    sbAIML.AppendLine("<module>" + itemBtnModule.Payload + "</module>");
                                    //sw.WriteLine("<module>" + itemBtnModule.Payload + "</module>");
                                }
                                //sw.WriteLine("</button>");
                                sbAIML.AppendLine("</button>");
                            }
                        }
                    }
                }
                if (card.TemplateGenericGroups != null && card.TemplateGenericGroups.Count() != 0)
                {
                    var lstTempGnrGroup = card.TemplateGenericGroups;
                    foreach (var item in lstTempGnrGroup)
                    {
                        if (item.TemplateGenericItems != null && item.TemplateGenericItems.Count() != 0)
                        {
                            var lstTemGnrItem = item.TemplateGenericItems;
                            StringBuilder sb = new StringBuilder();
                            StringBuilder sbAIMLChild = new StringBuilder();
                            foreach (var itemGnr in lstTemGnrItem)
                            {
                                sbAIMLChild.AppendLine("<card>");
                                sb.AppendLine("<card>");
                                if (!String.IsNullOrEmpty(itemGnr.Image))
                                {
                                    sbAIMLChild.AppendLine("<image>" + itemGnr.Image + "</image>");
                                    sb.AppendLine("<image>" + itemGnr.Image + "</image>");
                                }
                                sb.AppendLine("<title>" + HelperMethods.EscapeXml(itemGnr.Title) + "</title>");
                                sb.AppendLine("<subtitle>" + HelperMethods.EscapeXml(itemGnr.SubTitle) + "</subtitle>");
                                sb.AppendLine("<link>");
                                sb.AppendLine("<text>" + itemGnr.Url + "</text>");
                                sb.AppendLine("<url>" + itemGnr.Url + "</url>");
                                sb.AppendLine("</link>");

                                sbAIMLChild.AppendLine("<title>" + HelperMethods.EscapeXml(itemGnr.Title) + "</title>");
                                sbAIMLChild.AppendLine("<subtitle>" + HelperMethods.EscapeXml(itemGnr.SubTitle) + "</subtitle>");
                                sbAIMLChild.AppendLine("<link>");
                                sbAIMLChild.AppendLine("<text>" + itemGnr.Url + "</text>");
                                sbAIMLChild.AppendLine("<url>" + itemGnr.Url + "</url>");
                                sbAIMLChild.AppendLine("</link>");
                                if (itemGnr.ButtonPostbacks != null && itemGnr.ButtonPostbacks.Count() != 0)
                                {
                                    var lstButtonPostbacks = itemGnr.ButtonPostbacks;
                                    foreach (var itemBtnPostback in lstButtonPostbacks)
                                    {
                                        sb.AppendLine("<button>");
                                        sb.AppendLine("<text>" + HelperMethods.EscapeXml(itemBtnPostback.Title) + "</text>");
                                        sb.AppendLine("<menu>" + itemBtnPostback.Payload + "</menu>");
                                        sb.AppendLine("</button>");

                                        sbAIMLChild.AppendLine("<button>");
                                        sbAIMLChild.AppendLine("<text>" + HelperMethods.EscapeXml(itemBtnPostback.Title) + "</text>");
                                        sbAIMLChild.AppendLine("<menu>" + itemBtnPostback.Payload + "</menu>");
                                        sbAIMLChild.AppendLine("</button>");
                                    }
                                }
                                if (itemGnr.ButtonLinks != null && itemGnr.ButtonLinks.Count() != 0)
                                {
                                    var lstButtonLinks = itemGnr.ButtonLinks;
                                    foreach (var itemBtnLink in lstButtonLinks)
                                    {
                                        sb.AppendLine("<button>");
                                        sb.AppendLine("<text>" + HelperMethods.EscapeXml(itemBtnLink.Title) + "</text>");
                                        sb.AppendLine("<url>" + itemBtnLink.Url + "</url>");
                                        sb.AppendLine("</button>");

                                        sbAIMLChild.AppendLine("<button>");
                                        sbAIMLChild.AppendLine("<text>" + HelperMethods.EscapeXml(itemBtnLink.Title) + "</text>");
                                        sbAIMLChild.AppendLine("<url>" + itemBtnLink.Url + "</url>");
                                        sbAIMLChild.AppendLine("</button>");
                                    }
                                }
                                if (itemGnr.ButtonModules != null && itemGnr.ButtonModules.Count() != 0)
                                {
                                    var lstButtonModules = itemGnr.ButtonModules;
                                    foreach (var itemBtnModule in lstButtonModules)
                                    {
                                        sbAIMLChild.AppendLine("<button>");
                                        sbAIMLChild.AppendLine("<text>" + HelperMethods.EscapeXml(itemBtnModule.Title) + "</text>");

                                        sb.AppendLine("<button>");
                                        sb.AppendLine("<text>" + HelperMethods.EscapeXml(itemBtnModule.Title) + "</text>");
                                        if (itemBtnModule.ModuleKnowledgeID != null && itemBtnModule.ModuleKnowledgeID != 0)
                                        {
                                            sbAIML.AppendLine("<module>" + itemBtnModule.Payload + "_" + itemBtnModule.ModuleKnowledgeID + "</module>");
                                            //sw.WriteLine("<module>" + itemBtnModule.Payload + "_" + itemBtnModule.ModuleKnowledgeID + "</module>");
                                        }
                                        else if (itemBtnModule.MdSearchID != null && itemBtnModule.MdSearchID != 0)
                                        {
                                            sbAIML.AppendLine("<module>" + itemBtnModule.Payload + "_" + itemBtnModule.MdSearchID + "</module>");
                                            //sw.WriteLine("<module>" + itemBtnModule.Payload + "_" + itemBtnModule.MdSearchID + "</module>");
                                        }
                                        else if (itemBtnModule.MdVoucherID != null && itemBtnModule.MdVoucherID != 0)
                                        {
                                            sbAIML.AppendLine("<module>" + itemBtnModule.Payload + "_" + itemBtnModule.MdVoucherID + "</module>");
                                            //sw.WriteLine("<module>" + itemBtnModule.Payload + "_" + itemBtnModule.MdSearchID + "</module>");
                                        }
                                        else
                                        {
                                            sbAIML.AppendLine("<module>" + itemBtnModule.Payload + "</module>");
                                            //sw.WriteLine("<module>" + itemBtnModule.Payload + "</module>");
                                        }
                                        sb.AppendLine("</button>");
                                        sbAIMLChild.AppendLine("</button>");
                                    }
                                }
                                sb.AppendLine("</card>");
                                sbAIMLChild.AppendLine("</card>");
                            }

                            if (lstTemGnrItem.Count() > 1)
                            {
                                //carousel
                                //sw.WriteLine("<carousel>");
                                //sw.WriteLine(sb.ToString());
                                //sw.WriteLine("</carousel>");

                                sbAIML.AppendLine("<carousel>");
                                sbAIML.AppendLine(sbAIMLChild.ToString());
                                sbAIML.AppendLine("</carousel>");
                            }
                            else
                            {
                                //card
                                //sw.WriteLine(sb.ToString());
                                sbAIML.AppendLine(sbAIMLChild.ToString());
                            }
                        }
                    }
                }
                if (card.Images != null && card.Images.Count() != 0)
                {
                    foreach (var itemImg in card.Images)
                    {
                        sbAIML.AppendLine("<image>" + itemImg.Url + "</image>");
                        //sw.WriteLine("<image>"+itemImg.Url+"</image>");
                    }
                }
                if (card.FileDocuments != null && card.FileDocuments.Count() != 0)
                {
                    foreach (var itemFile in card.FileDocuments)
                    {
                        sbAIML.AppendLine("<file>" + itemFile.Url + "</file>");
                        //sw.WriteLine("<file>"+itemFile.Url+"</file>");
                    }
                }
                if (card.ModuleFollowCards != null && card.ModuleFollowCards.Count() != 0)
                {
                    foreach (var itemMdFollowCards in card.ModuleFollowCards)
                    {
                        string patternText = itemMdFollowCards.PartternText;
                        if (itemMdFollowCards.ModuleInfoPatientID != null && itemMdFollowCards.ModuleInfoPatientID != 0)
                        {
                            patternText = patternText + "_" + itemMdFollowCards.ModuleInfoPatientID;
                        }
                        if (itemMdFollowCards.MdSearchID != null && itemMdFollowCards.MdSearchID != 0)
                        {
                            patternText = patternText + "_" + itemMdFollowCards.MdSearchID;
                        }
                        if (itemMdFollowCards.MdVoucherID != null && itemMdFollowCards.MdVoucherID != 0)
                        {
                            patternText = patternText + "_" + itemMdFollowCards.MdVoucherID;
                        }
                        sbAIML.AppendLine(patternText);
                        //sw.WriteLine(patternText);
                    }
                }
                if (card.QuickReplys != null && card.QuickReplys.Count() != 0)
                {
                    var lstQuickReply = card.QuickReplys;
                    foreach (var itemQ in lstQuickReply)
                    {
                        //sw.WriteLine("<button>");
                        //sw.WriteLine("<text>" + itemQ.Title + "</text>");
                        //sw.WriteLine("<postback>" + itemQ.Payload + "</postback>");
                        //sw.WriteLine("</button>");

                        sbAIML.AppendLine("<button>");
                        sbAIML.AppendLine("<text>" + HelperMethods.EscapeXml(itemQ.Title) + "</text>");
                        sbAIML.AppendLine("<postback>" + itemQ.Payload + "</postback>");
                        sbAIML.AppendLine("</button>");
                    }
                }
                //sw.WriteLine("</template>");
                //sw.WriteLine("</category>");

                sbAIML.AppendLine("</template>");
                sbAIML.AppendLine("</category>");

                //if (card.ModuleFollowCards != null && card.ModuleFollowCards.Count() != 0)
                //{
                //    foreach (var itemMdFollowCards in card.ModuleFollowCards)
                //    {
                //        if (itemMdFollowCards.ModuleInfoPatientID != null && itemMdFollowCards.ModuleInfoPatientID != 0)
                //        {
                //            var mdGetInfoPatientDb = _mdKnowledgeService.GetByMdMedInfoPatientID(itemMdFollowCards.ModuleInfoPatientID ?? default(int));
                //            if (!String.IsNullOrEmpty(mdGetInfoPatientDb.Payload))
                //            {
                //                //sw.WriteLine("<category>");
                //                //sw.WriteLine("<pattern>module_patient_" + mdGetInfoPatientDb.Payload + "</pattern>");
                //                //sw.WriteLine("<template>");
                //                //sw.WriteLine("<srai>" + mdGetInfoPatientDb.Payload + "</srai>");
                //                //sw.WriteLine("</template>");
                //                //sw.WriteLine("</category>");

                //                sbAIML.AppendLine("<category>");
                //                sbAIML.AppendLine("<pattern>module_patient_" + mdGetInfoPatientDb.Payload + "</pattern>");
                //                sbAIML.AppendLine("<template>");
                //                sbAIML.AppendLine("<srai>" + mdGetInfoPatientDb.Payload + "</srai>");
                //                sbAIML.AppendLine("</template>");
                //                sbAIML.AppendLine("</category>");
                //            }
                //        }
                //        if (itemMdFollowCards.MdSearchID != null && itemMdFollowCards.MdSearchID != 0)
                //        {
                //            var mdSearchDb = _mdSearchService.GetByID(itemMdFollowCards.MdSearchID ?? default(int));
                //            if (!String.IsNullOrEmpty(mdSearchDb.Payload))
                //            {
                //                //sw.WriteLine("<category>");
                //                //sw.WriteLine("<pattern>module_api_search" + mdSearchDb.Payload + "</pattern>");
                //                //sw.WriteLine("<template>");
                //                //sw.WriteLine("<srai>" + mdSearchDb.Payload + "</srai>");
                //                //sw.WriteLine("</template>");
                //                //sw.WriteLine("</category>");

                //                sbAIML.AppendLine("<category>");
                //                sbAIML.AppendLine("<pattern>module_api_search" + mdSearchDb.Payload + "</pattern>");
                //                sbAIML.AppendLine("<template>");
                //                sbAIML.AppendLine("<srai>" + mdSearchDb.Payload + "</srai>");
                //                sbAIML.AppendLine("</template>");
                //                sbAIML.AppendLine("</category>");
                //            }
                //        }
                //        if (itemMdFollowCards.MdVoucherID != null && itemMdFollowCards.MdVoucherID != 0)
                //        {
                //            var mdVoucherDb = _mdVoucherService.GetByID(itemMdFollowCards.MdVoucherID ?? default(int));
                //            if (!String.IsNullOrEmpty(mdVoucherDb.Payload))
                //            {
                //                //sw.WriteLine("<category>");
                //                //sw.WriteLine("<pattern>module_api_search" + mdSearchDb.Payload + "</pattern>");
                //                //sw.WriteLine("<template>");
                //                //sw.WriteLine("<srai>" + mdSearchDb.Payload + "</srai>");
                //                //sw.WriteLine("</template>");
                //                //sw.WriteLine("</category>");

                //                sbAIML.AppendLine("<category>");
                //                sbAIML.AppendLine("<pattern>module_voucher" + mdVoucherDb.Payload + "</pattern>");
                //                sbAIML.AppendLine("<template>");
                //                sbAIML.AppendLine("<srai>" + mdVoucherDb.Payload + "</srai>");
                //                sbAIML.AppendLine("</template>");
                //                sbAIML.AppendLine("</category>");
                //            }
                //        }
                //    }
                //}
                sbAIML.AppendLine("</aiml>");
                //sw.WriteLine("</aiml>");
                //sw.Close();

                AIMLFile aimlFileDb = new AIMLFile();
                aimlFileDb.CardID = card.ID;
                aimlFileDb.UserID = userId;
                aimlFileDb.BotID = card.BotID;
                aimlFileDb.Extension = "aiml";
                aimlFileDb.Name = card.Name;
                aimlFileDb.Content = sbAIML.ToString();
                aimlFileDb.Status = true;
                _aimlFileService.Create(aimlFileDb);
                _aimlFileService.Save();
            }
            catch(Exception ex)
            {
                BotLog.Error(ex.StackTrace + " " + ex.InnerException.Message + ex.Message);
            }

        }

        public void CreateAIMLFileFormQnaByCardID(int formQnaID,string formName, string userId, int botID)
        {
            var lstQnaGroup = _qnaService.GetListQuesGroupToAimlByFormQnAnswerID(formQnaID).ToList();
            StringBuilder sbFormDb = new StringBuilder();
            try
            {
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
                        if (lstQuestion.Count() != 0 && lstAnswer.Count() != 0)
                        {
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
                                foreach (var item in lstQuestion)
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

                AIMLFile aimlFileDb = new AIMLFile();
                aimlFileDb.BotID = botID;
                aimlFileDb.FormQnAnswerID = formQnaID;
                aimlFileDb.Extension = "aiml";
                aimlFileDb.Status = true;
                aimlFileDb.Name = formName;
                aimlFileDb.UserID = userId;
                aimlFileDb.Content = sbFormDb.ToString();
                _aimlFileService.Create(aimlFileDb);
                _aimlFileService.Save();
            }
            catch (Exception ex)
            {
                BotLog.Error(ex.StackTrace + " " + ex.InnerException.Message + ex.Message);
            }
        }
    }
}
