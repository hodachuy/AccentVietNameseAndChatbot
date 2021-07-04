using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotProject.Common.ViewModels
{
    public class AIMLFileViewModel
    {
        public int ID { set; get; }

        public int? FormQnAnswerID { set; get; }

        public int? CardID { set; get; }

        public int BotID { set; get; }

        public string Name { set; get; }

        public string Src { set; get; }

        public string Extension { set; get; }

        public string Content { set; get; }

        public string UserID { set; get; }
        public bool? Status { set; get; }
        public bool? FormQnAStatus { set; get; }
    }
}
