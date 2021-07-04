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
using Newtonsoft.Json.Linq;

namespace BotProject.Web.API
{
    [RoutePrefix("api/groupcard")]
    public class GroupCardController : ApiControllerBase
    {
        private IGroupCardService _groupCardService;
        private ICardService _cardService;
        private ICommonCardService _commonCardService;
        public GroupCardController(IErrorService errorService,
            IGroupCardService groupCardService,
            ICardService cardService,
            ICommonCardService commonCardService) : base(errorService)
        {
            _groupCardService = groupCardService;
            _cardService = cardService;
            _commonCardService = commonCardService;
        }

        [Route("getbybot")]
        [HttpGet]
        public HttpResponseMessage GetAllByBotID(HttpRequestMessage request, int botId)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                var lstGroupCard = _groupCardService.GetListGroupCardByBotID(botId);
                var lstGroupCardVm = Mapper.Map<IEnumerable<GroupCard>, IEnumerable<GroupCardViewModel>>(lstGroupCard);
                if (lstGroupCardVm != null && lstGroupCardVm.Any())
                {
                    foreach(var item in lstGroupCardVm)
                    {
                        var lstCard = _cardService.GetListCardByGroupCardID(item.ID);
                        item.Cards = Mapper.Map<IEnumerable<Card>, IEnumerable<CardViewModel>>(lstCard);
                    }
                }
                response = request.CreateResponse(HttpStatusCode.OK, lstGroupCardVm);
                return response;
            });
        }

        [Route("create")]
        [HttpPost]
        public HttpResponseMessage Create(HttpRequestMessage request, GroupCardViewModel grCardVm)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;

                GroupCard grCardDb = new GroupCard();
                grCardDb.Name = grCardVm.Name;
                grCardDb.BotID = grCardVm.BotID;
                grCardDb.Index = grCardVm.Index;
                _groupCardService.Create(grCardDb);
                _groupCardService.Save();
                response = request.CreateResponse(HttpStatusCode.OK, grCardDb);
                return response;
            });
        }

        [Route("update")]
        [HttpPost]
        public HttpResponseMessage Update(HttpRequestMessage request, GroupCardViewModel grCardVm)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;

                GroupCard grCardDb = new GroupCard();
                grCardDb.Name = grCardVm.Name;
                grCardDb.BotID = grCardVm.BotID;
                grCardDb.ID = grCardVm.ID;
                grCardDb.Index = grCardVm.Index;
                _groupCardService.Update(grCardDb);
                _groupCardService.Save();
                response = request.CreateResponse(HttpStatusCode.OK, grCardDb);
                return response;
            });
        }
        [Route("updateIndex")]
        [HttpPost]
        public HttpResponseMessage UpdateIndex(HttpRequestMessage request, GroupCardViewModel grCardVm)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;

                GroupCard grCardDb = new GroupCard();
                grCardDb = _groupCardService.GetById(grCardVm.ID);
                grCardDb.Index = grCardVm.Index;
                _groupCardService.Update(grCardDb);
                _groupCardService.Save();
                response = request.CreateResponse(HttpStatusCode.OK, grCardDb);
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
                int groupCardId = json.groupCardId;
                if (groupCardId == 0)
                {
                    return request.CreateResponse(HttpStatusCode.NoContent);
                }

                var groupCardDb = _groupCardService.GetById(groupCardId);
                groupCardDb.IsDelete = true;
                _groupCardService.Update(groupCardDb);
                _groupCardService.Save();

                //var lstCard = _cardService.GetListCardByGroupCardID(groupCardId);
                //if(lstCard != null && lstCard.Count() != 0)
                //{
                //    foreach(var item in lstCard)
                //    {
                //        _commonCardService.DeleteCard(item.ID);
                //    }
                //}
                //_groupCardService.Delete(groupCardId);
                //_groupCardService.Save();

                response = request.CreateResponse(HttpStatusCode.OK, true);
                return response;
            });
        }
    }
}
