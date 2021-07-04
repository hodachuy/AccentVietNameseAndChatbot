using BotProject.Common.ViewModels;
using BotProject.Data.Infrastructure;
using BotProject.Model.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotProject.Data.Repositories
{
    public interface IAIMLFileRepository : IRepository<AIMLFile>
    {
        IEnumerable<AIMLFileViewModel> GetListAIMLFileActive(int botId);

    }

    public class AIMLFileRepository : RepositoryBase<AIMLFile>, IAIMLFileRepository
    {
        public AIMLFileRepository(IDbFactory dbFactory) : base(dbFactory)
        {
        }

        public IEnumerable<AIMLFileViewModel> GetListAIMLFileActive(int botId)
        {
            var query = from a in DbContext.AIMLFiles
                        join fq in DbContext.FormQuestionAnswers
                        on a.FormQnAnswerID equals fq.ID into joined
                        from fq in joined.DefaultIfEmpty()
                        where a.BotID == botId
                        && a.Status == true
                        select new AIMLFileViewModel
                        {
                            ID = a.ID,
                            Content = a.Content,
                            FormQnAStatus = fq.Status,
                            Status = a.Status,
                            BotID = a.BotID
                        };
            query = query.Where(x => x.FormQnAStatus != false);
            return query;
        }
    }
}
