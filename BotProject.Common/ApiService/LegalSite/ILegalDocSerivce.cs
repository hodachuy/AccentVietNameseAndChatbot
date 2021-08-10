using BotProject.Common.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotProject.Common.ApiService.LegalSite
{
    public interface ILegalDocSerivce
    {
        List<LegalApiModel> GetLegalDocs(string content);
    }
}
