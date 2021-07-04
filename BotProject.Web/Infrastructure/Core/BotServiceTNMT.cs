using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AIMLbot;
using System.Web.Hosting;
using System.Threading.Tasks;
using BotProject.Common;
using BotProject.Web.Models;
using BotProject.Service;
using System.Xml;
using BotProject.Web.Infrastructure.Log4Net;
using AutoMapper;
using BotProject.Common.ViewModels;
using BotProject.Model.Models;

namespace BotProject.Web.Infrastructure.Core
{
    sealed class BotServiceTNMT
    {
        AIMLbot.Bot _bot;
        User _user;
        private IAIMLFileService _aimlFileService;
        private static BotServiceTNMT botInstance = null;
        private static readonly object lockObject = new object();
        private string pathSetting = PathServer.PathAIML + "config";
        private BotServiceTNMT()
        {
            _bot = new AIMLbot.Bot();
            _bot.isAcceptingUserInput = true;
            _bot.loadSettings(pathSetting);
            _aimlFileService = ServiceFactory.Get<IAIMLFileService>();
            var lstAIML = _aimlFileService.GetActiveByBotId(5036);
            var lstAIMLVm = Mapper.Map<IEnumerable<AIMLFileViewModel>, IEnumerable<AIMLFile>>(lstAIML);

            loadAIMLFromDatabase(lstAIMLVm);

            _user = loadUserBot(Guid.NewGuid().ToString());
        }
        public static BotServiceTNMT BotInstance
        {
            get
            {
                if (botInstance == null)
                {
                    lock (lockObject)
                    {
                        if (botInstance == null)
                        {
                            botInstance = new BotServiceTNMT();
                        }

                    }
                }
                return botInstance;
            }
        }


        public AIMLbot.Result Chat(string text)//, AIMLbot.Utils.SettingsDictionary Predicates
        {
            AIMLbot.Request r = new Request(text, _user, _bot);
            AIMLbot.Result result = _bot.Chat(r);
            return result;
        }

        public AIMLbot.User loadUserBot(string userId)
        {
            _user = new User(userId, _bot);
            return _user;
        }

        public void loadAIMLFromDatabase(IEnumerable<BotProject.Model.Models.AIMLFile> lstAIML)
        {
            if (lstAIML.Count() != 0)
            {
                foreach (var item in lstAIML)
                {
                    try
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(item.Content);
                        _bot.loadAIMLFromXML(doc, item.Src);

                    }
                    catch (Exception ex)
                    {
                        string msg = item.Content + ex.Message;
                        BotLog.Info(msg);
                    }
                }
            }
        }
    }
}