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
using BotProject.Common;
using BotProject.Web.Infrastructure.Extensions;
using AutoMapper;
using System.Configuration;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using BotProject.Common.SendSmsMsgService;
using System.Threading;
using BotProject.Common.ViewModels;
using System.Web;

namespace BotProject.Web.API
{
    [RoutePrefix("api/card")]
    public class CardController : ApiControllerBase
    {
        private IErrorService _errorService;
        private ICardService _cardService;
        private IImageService _imageService;
        private ICommonCardService _commonCardService;
        private IFileCardService _fileCardService;
        private IMdSearchService _mdSearchService;
        private IMdVoucherService _mdVoucherService;
        private IModuleKnowledegeService _mdKnowledgeService;
        private IAIMLFileService _aimlService;
        private IHandleModuleServiceService _hService;

        public CardController(IErrorService errorService,
                            ICardService cardService,
                            IImageService imageService,
                            ICommonCardService commonCardService,
                            IModuleKnowledegeService mdKnowledgeService,
                            IFileCardService fileCardService,
                            IAIMLFileService aimlService,
                            IMdVoucherService mdVoucherService,
                            IHandleModuleServiceService hService,
                            IMdSearchService mdSearchService) : base(errorService)
        {
            _cardService = cardService;
            _imageService = imageService;
            _commonCardService = commonCardService;
            _errorService = errorService;
            _mdKnowledgeService = mdKnowledgeService;
            _fileCardService = fileCardService;
            _mdSearchService = mdSearchService;
            _aimlService = aimlService;
            _mdVoucherService = mdVoucherService;
            _hService = hService;

        }


        [Route("getbyid")]
        [HttpGet]
        public HttpResponseMessage GetById(HttpRequestMessage request, int cardId)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                var card = _commonCardService.GetFullDetailCard(cardId);
                response = request.CreateResponse(HttpStatusCode.OK, card);
                return response;
            });
        }

        [Route("delete")]
        [HttpPost]
        public HttpResponseMessage Delete(HttpRequestMessage request, JObject jsonData)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                dynamic json = jsonData;
                int cardId = json.cardId;
                if (cardId == 0)
                {
                    return request.CreateResponse(HttpStatusCode.NoContent);
                }
                var card = _cardService.GetByID(cardId);
                card.IsDelete = true;
                _cardService.Update(card);
                _cardService.Save();
                response = request.CreateResponse(HttpStatusCode.OK, card);
                return response;
            });
        }

        [Route("getbygroupcard")]
        [HttpGet]
        public HttpResponseMessage GetAllByGroupCard(HttpRequestMessage request, int groupCardId)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                var lstCard = _cardService.GetListCardByGroupCardID(groupCardId);
                var lstCardVm = Mapper.Map<IEnumerable<Card>, IEnumerable<CardViewModel>>(lstCard);
                response = request.CreateResponse(HttpStatusCode.OK, lstCardVm);
                return response;
            });
        }

        [Route("getbybot")]
        [HttpGet]
        public HttpResponseMessage GetAllByBot(HttpRequestMessage request, int botId)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                var lstCard = _cardService.GetListCardByBotID(botId);
                var lstCardVm = Mapper.Map<IEnumerable<Card>, IEnumerable<CardViewModel>>(lstCard);
                //if(lstCardVm.Any())
                //{
                //    lstCardVm = lstCardVm.Select(x => new CardViewModel
                //    {
                //        ID = x.ID,
                //        Alias = x.Alias,
                //        Name = x.Name,
                //        IsDelete = x.IsDelete,
                //        Status = x.Status,
                //        IsHaveCondition = x.IsHaveCondition,
                //        IsConditionWithAreaButton = x.IsConditionWithAreaButton,
                //        UserID = x.UserID,
                //        BotID = x.BotID,
                //        GroupCardID = x.GroupCardID
                //    });
                //}
                response = request.CreateResponse(HttpStatusCode.OK, lstCardVm);
                return response;
            });
        }


        [Route("getListCardSelect")]
        [HttpGet]
        public HttpResponseMessage GetListCardSelect(HttpRequestMessage request,string cardName, int botId, int page, int pageSize)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                int totalRow = 0;
                string filter = "c.BotID = " + botId;
                if (!String.IsNullOrEmpty(cardName))
                {
                    filter += " AND " + "(c.Name LIKE N'%" + cardName + "%'" + " OR " + "gc.Name LIKE N'%" + cardName + "%')";
                }
                var lstSPCard = _cardService.GetListCard(filter, "", page, pageSize, null).ToList();

                if (lstSPCard.Count() != 0)
                {
                    totalRow = lstSPCard[0].Total;
                }
                var paginationSet = new PaginationSet<SPCardViewModel>()
                {
                    Items = lstSPCard,
                    Page = page,
                    TotalCount = totalRow,
                    MaxPage = pageSize,
                    TotalPages = (int)Math.Ceiling((decimal)totalRow / pageSize)
                };
                response = request.CreateResponse(HttpStatusCode.OK, paginationSet);
                return response;
            });
        }


        [Route("addupdate")]
        [HttpPost]
        public HttpResponseMessage AddUpdate(HttpRequestMessage request, CardViewModel cardVm)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                if (!ModelState.IsValid)
                {
                    response = request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }
                try
                {
                    ResultCard rs = new ResultCard();
                    Card cardDb = new Card();
                    if (cardVm.ID == null)
                    {
                        cardDb.UpdateCard(cardVm);
                        _cardService.Create(cardDb);
                        _cardService.Save();
                        cardDb.PatternText = CommonConstants.PostBackCard + cardDb.ID.ToString();//use to card #
                        _cardService.Update(cardDb);
                        _cardService.Save();
                        rs.IsActionDb = true;

                        // create file card aiml
                        //string pathFolderAIML = ConfigurationManager.AppSettings["AIMLPath"] + "\\" + "User_" + cardVm.UserID + "_BotID_" + cardVm.BotID;
                        string pathFolderAIML = PathServer.PathAIML + "User_" + cardVm.UserID + "_BotID_" + cardVm.BotID;
                        string nameFolderAIML = "Card_ID_" + cardDb.ID + "_" + cardDb.Alias + ".aiml";
                        string pathString = System.IO.Path.Combine(pathFolderAIML, nameFolderAIML);
                        if (!System.IO.File.Exists(pathString))
                        {
                            try
                            {
                                //Open the File
                                StreamWriter sw = new StreamWriter(pathString, true, Encoding.UTF8);
                                sw.Close();
                            }
                            catch (Exception e)
                            {

                            }
                            finally
                            {

                            }
                        }
                        else
                        {
                            string message = "Không tìm thấy đường dẫn " + ConfigurationManager.AppSettings["AIMLPath"] + "\\" + "User_" + cardVm.UserID + "_BotID_" + cardVm.BotID;
                            response = request.CreateErrorResponse(HttpStatusCode.BadRequest, message);
                            return response;
                        }
                    }
                    else
                    {
                        cardDb.UpdateCard(cardVm);
                        cardDb.PatternText = CommonConstants.PostBackCard + cardVm.ID.ToString();//use to card #
                        cardDb.ID = cardVm.ID.GetValueOrDefault();
                        _cardService.Update(cardDb);
                        _cardService.Save();
                        rs.IsActionDb = false;
                        // delete all
                        _commonCardService.DeleteFullContentCard(cardDb.ID);
                        //_imageService.DeleteMutiImage(cardDb.ID);
                        //if (cardVm.FileAttachs != null && cardVm.FileAttachs.Count != 0)
                        //{
                        //    string pathImgServer = ConfigurationManager.AppSettings["ImagePath"];
                        //    string[] Files = Directory.GetFiles(pathImgServer);
                        //    foreach (var item in cardVm.FileAttachs)
                        //    {
                        //        var fileFileCard = _fileCardService.Delete(item.attachment_id);
                        //        string fileName = Regex.Replace(item.attachment_url, "File/Images/Card/", "");
                        //        foreach (string file in Files)
                        //        {
                        //            if (file.ToUpper().Contains(fileName.ToUpper()))
                        //            {
                        //                File.Delete(file);
                        //            }
                        //        }
                        //    }
                        //    _fileCardService.Save();
                        //}
                    }
                    //xác định index của template => đánh số index trên div class="content" card="text" 
                    if (cardVm.CardContents != null && cardVm.CardContents.Count() != 0)
                    {
                        foreach (var item in cardVm.CardContents)
                        {
                            var message = item.Message;
                            // Template generic
                            if (message.TemplateGenericGroupViewModel != null)
                            {
                                var temGnrGroupVm = message.TemplateGenericGroupViewModel;
                                TemplateGenericGroup tmpGrnGroupDb = new TemplateGenericGroup();
                                tmpGrnGroupDb.UpdateTemplateGenericGroup(temGnrGroupVm);
                                tmpGrnGroupDb.CardID = cardDb.ID;
                                _commonCardService.AddTempGnrGroup(tmpGrnGroupDb);
                                _commonCardService.Save();
                                if (temGnrGroupVm.TemplateGenericItemViewModels.Count() != 0)
                                {
                                    var lstTempGrnItemVm = temGnrGroupVm.TemplateGenericItemViewModels;
                                    foreach (var tempGnrItemVm in lstTempGrnItemVm)
                                    {
                                        TemplateGenericItem tempGnrItemDb = new TemplateGenericItem();
                                        tempGnrItemDb.UpdateTemplateGenericItem(tempGnrItemVm);
                                        tempGnrItemDb.TempGnrGroupID = tmpGrnGroupDb.ID;
                                        tempGnrItemDb.CardID = cardDb.ID;
                                        _commonCardService.AddTempGnrItem(tempGnrItemDb);
                                        _commonCardService.Save();
                                        if (tempGnrItemVm.ButtonLinkViewModels.Count() != 0)
                                        {
                                            var lstBtnLinkVm = tempGnrItemVm.ButtonLinkViewModels;
                                            foreach (var btnLinkVm in lstBtnLinkVm)
                                            {
                                                ButtonLink btnLinkDb = new ButtonLink();
                                                btnLinkDb.UpdateButtonLink(btnLinkVm);
                                                btnLinkDb.CardID = cardDb.ID;
                                                btnLinkDb.TempGnrItemID = tempGnrItemDb.ID;
                                                _commonCardService.AddButtonLink(btnLinkDb);
                                                _commonCardService.Save();
                                            }
                                        }
                                        if (tempGnrItemVm.ButtonPostbackViewModels.Count() != 0)
                                        {
                                            var lstBtnPostbackVm = tempGnrItemVm.ButtonPostbackViewModels;
                                            foreach (var btnPostbackVm in lstBtnPostbackVm)
                                            {
                                                ButtonPostback btnPostbackDb = new ButtonPostback();
                                                btnPostbackDb.UpdateButtonPostback(btnPostbackVm);
                                                btnPostbackDb.CardID = cardDb.ID;
                                                btnPostbackDb.TempGnrItemID = tempGnrItemDb.ID;
                                                _commonCardService.AddButtonPostback(btnPostbackDb);
                                                _commonCardService.Save();
                                            }
                                        }
                                        if (tempGnrItemVm.ButtonModuleViewModels.Count() != 0)
                                        {
                                            var lstBtnModuleVm = tempGnrItemVm.ButtonModuleViewModels;
                                            foreach (var btnModuleVm in lstBtnModuleVm)
                                            {
                                                ButtonModule btnModuleDb = new ButtonModule();
                                                btnModuleDb.UpdateButtonModule(btnModuleVm);
                                                btnModuleDb.CardID = cardDb.ID;
                                                btnModuleDb.TempGnrItemID = tempGnrItemDb.ID;
                                                btnModuleDb.ModuleKnowledgeID = btnModuleVm.ModuleKnowledgeID;
                                                btnModuleDb.Payload = btnModuleVm.Payload;// + "_" + btnModuleVm.ModuleKnowledgeID
                                                btnModuleDb.MdSearchID = btnModuleVm.MdSearchID;
                                                btnModuleDb.MdVoucherID = btnModuleVm.MdVoucherID;
                                                _commonCardService.AddButtonModule(btnModuleDb);
                                                _commonCardService.Save();

                                                // update button id tới module med get info patient
                                                if (btnModuleVm.ModuleKnowledgeID != null && btnModuleVm.ModuleKnowledgeID != 0)
                                                {
                                                    var mdMedGetInfoPatientDb = _mdKnowledgeService.GetByMdMedInfoPatientID(btnModuleVm.ModuleKnowledgeID ?? default(int));
                                                    mdMedGetInfoPatientDb.ButtonModuleID = btnModuleDb.ID;
                                                    _mdKnowledgeService.UpdateMdKnowledfeMedInfoPatient(mdMedGetInfoPatientDb);
                                                    _mdKnowledgeService.Save();
                                                }
                                                if (btnModuleVm.MdSearchID != null && btnModuleVm.MdSearchID != 0)
                                                {
                                                    var mdSearchDb = _mdSearchService.GetByID(btnModuleVm.MdSearchID ?? default(int));
                                                    mdSearchDb.ButtonModuleID = btnModuleDb.ID;
                                                    _mdSearchService.Update(mdSearchDb);
                                                    _mdSearchService.Save();
                                                }
                                                if (btnModuleVm.MdVoucherID != null && btnModuleVm.MdVoucherID != 0)
                                                {
                                                    var mdVoucherDb = _mdVoucherService.GetByID(btnModuleVm.MdVoucherID ?? default(int));
                                                    mdVoucherDb.ButtonModuleID = btnModuleDb.ID;
                                                    _mdVoucherService.Update(mdVoucherDb);
                                                    _mdVoucherService.Save();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            // Template Text
                            if (message.TemplateTextViewModel != null)
                            {
                                var tempTextVm = message.TemplateTextViewModel;
                                TemplateText tempTextDb = new TemplateText();
                                tempTextDb.UpdateTemplateText(tempTextVm);
                                tempTextDb.CardID = cardDb.ID;
                                _commonCardService.AddTempText(tempTextDb);
                                _commonCardService.Save();
                                if (tempTextVm.ButtonLinkViewModels.Count() != 0)
                                {
                                    var lstBtnLinkVm = tempTextVm.ButtonLinkViewModels;
                                    foreach (var btnLinkVm in lstBtnLinkVm)
                                    {
                                        ButtonLink btnLinkDb = new ButtonLink();
                                        btnLinkDb.UpdateButtonLink(btnLinkVm);
                                        btnLinkDb.CardID = cardDb.ID;
                                        btnLinkDb.TempTxtID = tempTextDb.ID;
                                        _commonCardService.AddButtonLink(btnLinkDb);
                                        _commonCardService.Save();
                                    }
                                }
                                if (tempTextVm.ButtonPostbackViewModels.Count() != 0)
                                {
                                    var lstBtnPostbackVm = tempTextVm.ButtonPostbackViewModels;
                                    foreach (var btnPostbackVm in lstBtnPostbackVm)
                                    {
                                        ButtonPostback btnPostbackDb = new ButtonPostback();
                                        btnPostbackDb.UpdateButtonPostback(btnPostbackVm);
                                        btnPostbackDb.CardID = cardDb.ID;
                                        btnPostbackDb.TempTxtID = tempTextDb.ID;
                                        _commonCardService.AddButtonPostback(btnPostbackDb);
                                        _commonCardService.Save();
                                    }
                                }
                                if (tempTextVm.ButtonModuleViewModels.Count() != 0)
                                {
                                    var lstBtnModuleVm = tempTextVm.ButtonModuleViewModels;
                                    foreach (var btnModuleVm in lstBtnModuleVm)
                                    {
                                        ButtonModule btnModuleDb = new ButtonModule();
                                        btnModuleDb.UpdateButtonModule(btnModuleVm);
                                        btnModuleDb.CardID = cardDb.ID;
                                        btnModuleDb.TempTxtID = tempTextDb.ID;
                                        btnModuleDb.ModuleKnowledgeID = btnModuleVm.ModuleKnowledgeID;
                                        btnModuleDb.MdSearchID = btnModuleVm.MdSearchID;
                                        btnModuleDb.MdVoucherID = btnModuleVm.MdVoucherID;
                                        btnModuleDb.Payload = btnModuleVm.Payload;// + "_" + btnModuleVm.ModuleKnowledgeID
                                        _commonCardService.AddButtonModule(btnModuleDb);
                                        _commonCardService.Save();

                                        // update button id tới module med get info patient
                                        if (btnModuleVm.ModuleKnowledgeID != null && btnModuleVm.ModuleKnowledgeID != 0)
                                        {
                                            var mdMedGetInfoPatientDb = _mdKnowledgeService.GetByMdMedInfoPatientID(btnModuleVm.ModuleKnowledgeID ?? default(int));
                                            mdMedGetInfoPatientDb.ButtonModuleID = btnModuleDb.ID;
                                            _mdKnowledgeService.UpdateMdKnowledfeMedInfoPatient(mdMedGetInfoPatientDb);
                                            _mdKnowledgeService.Save();
                                        }
                                        if (btnModuleVm.MdSearchID != null && btnModuleVm.MdSearchID != 0)
                                        {
                                            var mdSearchDb = _mdSearchService.GetByID(btnModuleVm.MdSearchID ?? default(int));
                                            mdSearchDb.ButtonModuleID = btnModuleDb.ID;
                                            _mdSearchService.Update(mdSearchDb);
                                            _mdSearchService.Save();
                                        }
                                        if (btnModuleVm.MdVoucherID != null && btnModuleVm.MdVoucherID != 0)
                                        {
                                            var mdVoucherDb = _mdVoucherService.GetByID(btnModuleVm.MdVoucherID ?? default(int));
                                            mdVoucherDb.ButtonModuleID = btnModuleDb.ID;
                                            _mdVoucherService.Update(mdVoucherDb);
                                            _mdVoucherService.Save();
                                        }

                                    }
                                }
                            }
                            // Image
                            if (message.ImageViewModel != null)
                            {
                                var imgVm = message.ImageViewModel;
                                Image imgDb = new Image();
                                imgDb.CardID = cardDb.ID;
                                imgDb.BotID = cardVm.BotID;
                                imgDb.Url = imgVm.Url;
                                imgDb.Index = imgVm.Index;
                                _commonCardService.AddImage(imgDb);
                                _commonCardService.Save();
                            }

                            // File Document
                            if (message.FileDocumentViewModel != null)
                            {
                                var fileDocVm = message.FileDocumentViewModel;
                                FileDocument fileDocDb = new FileDocument();
                                fileDocDb.CardID = cardDb.ID;
                                fileDocDb.BotID = cardVm.BotID;
                                fileDocDb.Url = fileDocVm.Url;
                                fileDocDb.Index = fileDocVm.Index;
                                fileDocDb.TokenZalo = fileDocVm.TokenZalo;
                                fileDocDb.TokenFacebook = fileDocVm.TokenFacebook;
                                _commonCardService.AddFileDocument(fileDocDb);
                                _commonCardService.Save();
                            }

                            // module follow card
                            if (message.ModuleFollowCardViewModel != null)
                            {
                                var mdFollowCardVm = message.ModuleFollowCardViewModel;
                                ModuleFollowCard mdFCardDb = new ModuleFollowCard();
                                mdFCardDb.CardID = cardDb.ID;
                                mdFCardDb.BotID = cardVm.BotID;
                                mdFCardDb.PartternText = mdFollowCardVm.PartternText;//postback_moudle_phone...//
                                mdFCardDb.ModuleInfoPatientID = mdFollowCardVm.ModuleInfoPatientID == null ? 0 : mdFollowCardVm.ModuleInfoPatientID;
                                mdFCardDb.MdSearchID = mdFollowCardVm.MdSearchID == null ? 0 : mdFollowCardVm.MdSearchID;
                                mdFCardDb.MdVoucherID = mdFollowCardVm.MdVoucherID == null ? 0 : mdFollowCardVm.MdVoucherID;
                                if (mdFollowCardVm.PartternText != "postback_module_med_get_info_patient")
                                {
                                    if (mdFollowCardVm.ModuleInfoPatientID != null && mdFollowCardVm.ModuleInfoPatientID != 0)
                                    {
                                        _mdKnowledgeService.DeleteMdMedInfoPatient(mdFollowCardVm.ModuleInfoPatientID ?? default(int));
                                        mdFCardDb.ModuleInfoPatientID = 0;
                                    }
                                }
                                if (mdFollowCardVm.PartternText != "postback_module_api_search")
                                {
                                    if (mdFollowCardVm.MdSearchID != null && mdFollowCardVm.MdSearchID != 0)
                                    {
                                        _mdSearchService.Delete(mdFollowCardVm.MdSearchID ?? default(int));
                                        mdFCardDb.MdSearchID = 0;
                                    }
                                }
                                if (mdFollowCardVm.PartternText != "postback_module_voucher")
                                {
                                    if (mdFollowCardVm.MdVoucherID != null && mdFollowCardVm.MdVoucherID != 0)
                                    {
                                        _mdVoucherService.Delete(mdFollowCardVm.MdVoucherID ?? default(int));
                                        mdFCardDb.MdVoucherID = 0;
                                    }
                                }

                                mdFCardDb.Index = mdFollowCardVm.Index;
                                _commonCardService.AddModuleFollowCard(mdFCardDb);
                                _commonCardService.Save();
                                // update
                                if (mdFollowCardVm.PartternText == "postback_module_med_get_info_patient")
                                {
                                    var mdMedGetInfoPatientDb = _mdKnowledgeService.GetByMdMedInfoPatientID(mdFCardDb.ModuleInfoPatientID ?? default(int));
                                    mdMedGetInfoPatientDb.ModuleFollowCardID = mdFCardDb.ID;
                                    _mdKnowledgeService.UpdateMdKnowledfeMedInfoPatient(mdMedGetInfoPatientDb);
                                    _mdKnowledgeService.Save();
                                }
                                // update
                                if (mdFollowCardVm.PartternText == "postback_module_api_search")
                                {
                                    var mdSearchDb = _mdSearchService.GetByID(mdFCardDb.MdSearchID ?? default(int));
                                    mdSearchDb.ModuleFollowCardID = mdFCardDb.ID;
                                    _mdSearchService.Update(mdSearchDb);
                                    _mdSearchService.Save();
                                }
                                // update
                                if (mdFollowCardVm.PartternText == "postback_module_voucher")
                                {
                                    var mdVoucherDb = _mdVoucherService.GetByID(mdFCardDb.MdVoucherID ?? default(int));
                                    mdVoucherDb.ModuleFollowCardID = mdFCardDb.ID;
                                    _mdVoucherService.Update(mdVoucherDb);
                                    _mdVoucherService.Save();
                                }
                            }
                        }
                        // trả lời nhanh
                        if (cardVm.QuickReplyViewModels != null && cardVm.QuickReplyViewModels.Count() != 0)
                        {
                            var lstQuickReplyVm = cardVm.QuickReplyViewModels;
                            foreach (var itemQuickReplyVm in lstQuickReplyVm)
                            {
                                BotProject.Model.Models.QuickReply quickReplyDb = new BotProject.Model.Models.QuickReply();
                                quickReplyDb.UpdateQuickReply(itemQuickReplyVm);
                                quickReplyDb.CardID = cardDb.ID;
                                _commonCardService.AddQuickReply(quickReplyDb);
                                _commonCardService.Save();
                            }
                        }
                    }

                    rs.Card = cardDb;
                    response = request.CreateResponse(HttpStatusCode.OK, rs);
                }
                catch (Exception ex)
                {
                    Error err = new Error();
                    err.Message = ex.Message;
                    err.CreatedDate = DateTime.Now;
                    _errorService.Create(err);

                    response = request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message);
                }
                return response;
            });
        }
        [Route("getaimlcard")]
        [HttpGet]
        public HttpResponseMessage GetAimlCard(HttpRequestMessage request, int cardId, string userId)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;

                var card = _commonCardService.GetFullDetailCard(cardId);
                // open file card aiml
                //string pathFolderAIML = ConfigurationManager.AppSettings["AIMLPath"] + "\\" + "User_" + userId + "_BotID_" + card.BotID;
                string pathFolderAIML = PathServer.PathAIML;
                string nameFolderAIML = "User_" + userId + "_BotID_" + card.BotID + "\\Card_ID_" + card.ID + "_" + card.Alias + ".aiml";
                //string pathString = System.IO.Path.Combine(pathFolderAIML, nameFolderAIML);

                var aimlDb = _aimlService.GetByCardId(cardId);

                StringBuilder sbAIML = new StringBuilder();

                //if (System.IO.File.Exists(pathString))
                //{
                //File.WriteAllText(pathString, string.Empty);
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
                                    sbAIML.AppendLine("<url>" + HttpUtility.HtmlEncode(itemBtnLink.Url) + "</url>");
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
                                    sbAIMLChild.AppendLine("<text>" + HttpUtility.HtmlEncode(itemGnr.Url) + "</text>");
                                    sbAIMLChild.AppendLine("<url>" + HttpUtility.HtmlEncode(itemGnr.Url) + "</url>");
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
                                            sb.AppendLine("<url>" + HttpUtility.HtmlEncode(itemBtnLink.Url) + "</url>");
                                            sb.AppendLine("</button>");

                                            sbAIMLChild.AppendLine("<button>");
                                            sbAIMLChild.AppendLine("<text>" + HelperMethods.EscapeXml(itemBtnLink.Title) + "</text>");
                                            sbAIMLChild.AppendLine("<url>" + HttpUtility.HtmlEncode(itemBtnLink.Url) + "</url>");
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

                    if (card.ModuleFollowCards != null && card.ModuleFollowCards.Count() != 0)
                    {
                        foreach (var itemMdFollowCards in card.ModuleFollowCards)
                        {
                            if (itemMdFollowCards.ModuleInfoPatientID != null && itemMdFollowCards.ModuleInfoPatientID != 0)
                            {
                                var mdGetInfoPatientDb = _mdKnowledgeService.GetByMdMedInfoPatientID(itemMdFollowCards.ModuleInfoPatientID ?? default(int));
                                if (!String.IsNullOrEmpty(mdGetInfoPatientDb.Payload))
                                {
                                    //sw.WriteLine("<category>");
                                    //sw.WriteLine("<pattern>module_patient_" + mdGetInfoPatientDb.Payload + "</pattern>");
                                    //sw.WriteLine("<template>");
                                    //sw.WriteLine("<srai>" + mdGetInfoPatientDb.Payload + "</srai>");
                                    //sw.WriteLine("</template>");
                                    //sw.WriteLine("</category>");

                                    sbAIML.AppendLine("<category>");
                                    sbAIML.AppendLine("<pattern>module_patient_" + mdGetInfoPatientDb.Payload + "</pattern>");
                                    sbAIML.AppendLine("<template>");
                                    sbAIML.AppendLine("<srai>" + mdGetInfoPatientDb.Payload + "</srai>");
                                    sbAIML.AppendLine("</template>");
                                    sbAIML.AppendLine("</category>");
                                }
                            }
                            if (itemMdFollowCards.MdSearchID != null && itemMdFollowCards.MdSearchID != 0)
                            {
                                var mdSearchDb = _mdSearchService.GetByID(itemMdFollowCards.MdSearchID ?? default(int));
                                if (!String.IsNullOrEmpty(mdSearchDb.Payload))
                                {
                                    //sw.WriteLine("<category>");
                                    //sw.WriteLine("<pattern>module_api_search" + mdSearchDb.Payload + "</pattern>");
                                    //sw.WriteLine("<template>");
                                    //sw.WriteLine("<srai>" + mdSearchDb.Payload + "</srai>");
                                    //sw.WriteLine("</template>");
                                    //sw.WriteLine("</category>");

                                    sbAIML.AppendLine("<category>");
                                    sbAIML.AppendLine("<pattern>module_api_search" + mdSearchDb.Payload + "</pattern>");
                                    sbAIML.AppendLine("<template>");
                                    sbAIML.AppendLine("<srai>" + mdSearchDb.Payload + "</srai>");
                                    sbAIML.AppendLine("</template>");
                                    sbAIML.AppendLine("</category>");
                                }
                            }
                            if (itemMdFollowCards.MdVoucherID != null && itemMdFollowCards.MdVoucherID != 0)
                            {
                                var mdVoucherDb = _mdVoucherService.GetByID(itemMdFollowCards.MdVoucherID ?? default(int));
                                if (!String.IsNullOrEmpty(mdVoucherDb.Payload))
                                {
                                    //sw.WriteLine("<category>");
                                    //sw.WriteLine("<pattern>module_api_search" + mdSearchDb.Payload + "</pattern>");
                                    //sw.WriteLine("<template>");
                                    //sw.WriteLine("<srai>" + mdSearchDb.Payload + "</srai>");
                                    //sw.WriteLine("</template>");
                                    //sw.WriteLine("</category>");

                                    sbAIML.AppendLine("<category>");
                                    sbAIML.AppendLine("<pattern>module_voucher" + mdVoucherDb.Payload + "</pattern>");
                                    sbAIML.AppendLine("<template>");
                                    sbAIML.AppendLine("<srai>" + mdVoucherDb.Payload + "</srai>");
                                    sbAIML.AppendLine("</template>");
                                    sbAIML.AppendLine("</category>");
                                }
                            }
                        }
                    }
                    sbAIML.AppendLine("</aiml>");
                    //sw.WriteLine("</aiml>");
                    //sw.Close();

                    if (aimlDb == null)
                    {
                        AIMLFile aimlFileDb = new AIMLFile();
                        aimlFileDb.CardID = card.ID;
                        aimlFileDb.UserID = userId;
                        aimlFileDb.BotID = card.BotID;
                        aimlFileDb.Src = nameFolderAIML;
                        aimlFileDb.Extension = "aiml";
                        aimlFileDb.Name = card.Name;
                        aimlFileDb.Content = sbAIML.ToString();
                        aimlFileDb.Status = true;
                        _aimlService.Create(aimlFileDb);
                        _aimlService.Save();
                    }
                    else
                    {
                        aimlDb.Content = sbAIML.ToString();
                        _aimlService.Update(aimlDb);
                        _aimlService.Save();
                    }
                }
                catch (Exception e)
                {

                }
                finally
                {

                }

                //}
                response = request.CreateResponse(HttpStatusCode.OK, card);
                return response;
            });
        }


        //[Route("getAimlMultiCard")]
        //[HttpGet]
        //public HttpResponseMessage GetAimlMultiCard(HttpRequestMessage request)
        //{
        //    return CreateHttpResponse(request, () =>
        //    {
        //        HttpResponseMessage response = null;

        //        var lstAIML = _aimlService.GetByBotId(5036).Where(x =>x.FormQnAnswerID == null).ToList();
        //        int ttCount = lstAIML.Count();
        //        int i = 0;
        //        foreach(var item in lstAIML)
        //        {
        //            i++;
        //            int x = 100;
        //            AIMLFile aimlFileDb = new AIMLFile();
        //            aimlFileDb = _aimlService.GetByID(item.ID);
        //            aimlFileDb.Content = Regex.Replace(item.Content, "&", "và");
        //            _aimlService.Update(aimlFileDb);
        //            _aimlService.Save();
        //            if(i == x)
        //            {
        //                Thread.Sleep(3000);
        //                x = x + 100;
        //            }
        //        }
               
        //        response = request.CreateResponse(HttpStatusCode.OK, true);
        //        return response;
        //    });
        //}

        public class ResultCard
        {
            public bool IsActionDb { set; get; }
            public Card Card { set; get; }
        }
    }
}
