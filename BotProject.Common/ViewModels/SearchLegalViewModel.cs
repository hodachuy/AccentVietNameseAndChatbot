using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotProject.Common.ViewModels
{
    public class SearchLegalViewModel
    {
        public bool status { get; set; }
        public string message { get; set; }
        public List<LegalVm> data { get; set; }
    }


    public class LegalApiModel
    {
        public string Title { get; set; }
        public string SubTitle { get; set; }
        public string Url { get; set; }
    }
}
