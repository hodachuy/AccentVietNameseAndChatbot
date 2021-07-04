using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BotProject.Web.Models.Livechat
{
    public class MessageViewModel
    {
        public long ID { set; get; }
        public long ConversationID { set; get; }
        public DateTime Timestamp { set; get; }
        public string Body { set; get; }
        public string AgentID { set; get; }
        public string CustomerID { set; get; }
        public bool IsBotChat { set; get; }
    }
}