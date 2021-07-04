using AutoMapper;
using BotProject.Common.ViewModels;
using BotProject.Model.Models;
using BotProject.Service;
using BotProject.Web.Infrastructure.Core;
using BotProject.Web.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;
using System.Web.Mvc;
namespace BotProject.Web.Controllers
{
    public class DashboardController : BaseController
    {
        private IBotService _botService;
		private IQnAService _qnaService;

        public DashboardController(IErrorService errorService,
									IBotService botService,
									IQnAService qnaService) : base(errorService)
        {
            _botService = botService;
			_qnaService = qnaService;

		}

        public ActionResult Index()
        {

            if (UserInfo == null || (UserInfo != null && UserInfo.Groups == null))
            {
                return RedirectToAction("Login", "Account");
            }

            var lstBot = _botService.GetListBotDashboard(UserInfo.Id);
            var lstBotViewModel = Mapper.Map<IEnumerable<StoreProcBotViewModel>, IEnumerable<BotViewModel>>(lstBot);
            if(lstBotViewModel.Count() != 0)
            {
                foreach(var item in lstBotViewModel)
                {
                    if (item.Name.Split(' ').Length > 1)
                    {
                        item.NameAcronym = (item.Name.Split(' ')[0].Substring(0, 1) + item.Name.Split(' ')[1].Substring(0, 1)).ToUpper();
                    }else
                    {
                        item.NameAcronym = item.Name.Split(' ')[0].Substring(0, 1).ToUpper();
                    }
                }
            }
            return View(lstBotViewModel);
        }

        [ChildActionOnly]
        public ActionResult Header()
        {
            return PartialView(UserInfo);
        }

        [ChildActionOnly]
        //[OutputCache(Duration = 3600, Location = System.Web.UI.OutputCacheLocation.Client)]
        public ActionResult BotCategory()
        {
            var lstBot = _botService.GetListBotByUserID(UserInfo.Id);			
			if(lstBot != null && lstBot.Count() != 0)
			{
				foreach(var item in lstBot)
				{
					item.FormQuestionAnswers = _qnaService.GetListFormByBotID(item.ID);
				}
			}
            var lstBotViewModel = Mapper.Map<IEnumerable<Bot>, IEnumerable<BotViewModel>>(lstBot);
            return PartialView(lstBotViewModel);
        }


        [ChildActionOnly]
        public ActionResult Footer()
        {
            return PartialView();
        }
    }
}