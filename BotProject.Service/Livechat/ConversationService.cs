using BotProject.Common.ViewModels.LiveChat;
using BotProject.Data.Infrastructure;
using BotProject.Data.Repositories;
using BotProject.Data.Repositories.LiveChat;
using BotProject.Model.Models.LiveChat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotProject.Service.Livechat
{
    public interface IConversationService
    {
        Conversation AddConversation(Conversation conversation);
        Message AddMessage(Message msg);
        IEnumerable<Conversation> GetListConversation(long threadId);
        IEnumerable<SP_ConversationMessage> GetListConversationMessage(string filter, string sort, int pageNumber, int pageSize, long? selectedID);
        void Save();
    }
    public class ConversationService : IConversationService
    {
        private IConversationRepository _conversationRepository;
        private IMessageRepository _messageRepository;
        private IUnitOfWork _unitOfWork;
        public ConversationService(IUnitOfWork unitOfWork,
                                   IConversationRepository conversationRepository,
                                   IMessageRepository messageRepository)
        {
            _unitOfWork = unitOfWork;
            _conversationRepository = conversationRepository;
            _messageRepository = messageRepository;
        }
        public Conversation AddConversation(Conversation conversation)
        {
            return _conversationRepository.Add(conversation);
        }

        public Message AddMessage(Message msg)
        {
            return _messageRepository.Add(msg);
        }

        public IEnumerable<Conversation> GetListConversation(long threadId)
        {
            return _conversationRepository.GetMulti(x => x.ThreadID == threadId);
        }

        public IEnumerable<SP_ConversationMessage> GetListConversationMessage(string filter, string sort, int pageNumber, int pageSize, long? selectedID)
        {
            return _conversationRepository.GetListConversationMessage(filter, sort, pageNumber, pageSize, selectedID);
        }

        public void Save()
        {
            _unitOfWork.Commit();
        }
    }
}
