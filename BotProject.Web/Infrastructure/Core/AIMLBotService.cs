using AIMLbot;
using BotProject.Common;
using BotProject.Model.Models;
using BotProject.Service;
using BotProject.Web.Infrastructure.Log4Net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Xml;

namespace BotProject.Web.Infrastructure.Core
{
    public sealed class AIMLBotService
    {
        private IBotService _botDbService;
        List<Tuple<AIMLbot.Bot, string>> _lstAIMLBot = new List<Tuple<AIMLbot.Bot, string>>();
        private static readonly Lazy<AIMLBotService> singleInstance = new Lazy<AIMLBotService>(() => new AIMLBotService());
        private AIMLBotService()
        {
            _botDbService = ServiceFactory.Get<IBotService>();
            var lstBotDb = _botDbService.GetListBotByAllUser();
            foreach(var bot in lstBotDb)
            {
                var aimlBot = new AIMLbot.Bot();
                aimlBot.loadSettings(PathServer.PathAIML + "config");
                aimlBot.isAcceptingUserInput = true;
                _lstAIMLBot.Add(new Tuple<AIMLbot.Bot, string>(aimlBot, bot.ID.ToString()));
            }
        }
        public static AIMLBotService AIMLBotInstance
        {
            get
            {
                return singleInstance.Value;
            }
        }

        public AIMLbot.Result Chat(string text, User userBot, AIMLbot.Bot bot)
        {
            AIMLbot.Request r = new Request(text, userBot, bot);
            AIMLbot.Result result = bot.Chat(r);
            return result;
        }

        public AIMLbot.User loadUserBot(string userId, AIMLbot.Bot bot)
        {
            return new User(userId, bot);
        }

        public AIMLbot.Bot GetServerBot(string botId)
        {
            bool hasAIMLBotService = _lstAIMLBot.Any(x => x.Item2.Equals(botId));
            if (hasAIMLBotService == false) {
                var aimlBot = new AIMLbot.Bot();
                aimlBot.loadSettings(PathServer.PathAIML + "config");
                aimlBot.isAcceptingUserInput = true;
                _lstAIMLBot.Add(new Tuple<AIMLbot.Bot, string>(aimlBot, botId));
                return aimlBot;
            }
            return _lstAIMLBot.Where(x => x.Item2 == botId).FirstOrDefault().Item1;
        }

        public void LoadBotSetting(AIMLbot.Bot bot)
        {
            bot.isAcceptingUserInput = true;
            bot.loadSettings(PathServer.PathAIML + "config");
        }

        public void LoadGraphmasterFromAIMLBinaryFile(string pathFolderAIML2Graphmaster, AIMLbot.Bot bot)
        {
            bot.loadFromBinaryFile(pathFolderAIML2Graphmaster);
        }

        public void LoadAIMLFile2Graphmaster(IEnumerable<AIMLFile> lstAIML, string botId, AIMLbot.Bot bot)
        {
            if (lstAIML.Count() != 0)
            {
                foreach (var item in lstAIML)
                {
                    try
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(item.Content);
                        bot.loadAIMLFromXML(doc, item.Src);

                    }
                    catch (Exception ex)
                    {
                        string msg = item.Content + ex.Message;
                        BotLog.Info(msg);
                    }
                }
            }
        }

        public void SaveGraphmaster2AIMLBinaryFile(string pathFolderAIML2Graphmaster, AIMLbot.Bot bot)
        {
            bot.saveToBinaryFile(pathFolderAIML2Graphmaster);
        }
    }
}