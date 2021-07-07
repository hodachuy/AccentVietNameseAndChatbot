using Microsoft.AspNet.SignalR;
using System;
using System.Threading.Tasks;
using BotProject.Service.Livechat;
using Microsoft.AspNet.Identity;
using BotProject.Model.Models.LiveChat;
using BotProject.Web.Infrastructure.Core;
using BotProject.Web.Infrastructure.Log4Net;
using System.Linq;
using BotProject.Common;
using System.Data.SqlClient;
using BotProject.Data.Repositories;
using Microsoft.AspNet.SignalR.Hubs;
using Autofac;

namespace BotProject.Web.SignalRChat
{
    public class ChatHub : Hub
    {
        static IHubContext _context = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();

        private const string USER_CUSTOMER = "customer";
        private const string USER_AGENT = "agent";
        private const string USER_BOT = "bot";

        private string _connectionID = "";

        private readonly string _sqlConnection = Helper.ReadString("SqlConnection");

        private readonly ILifetimeScope _hubLifetimeScope;

        private ICustomerService _customerService;
        private IChatCommonSerivce _chatCommonService;
        private ApplicationUserManager _userManager;

       
        public ChatHub(ILifetimeScope lifetimeScope)
        {
            _hubLifetimeScope = lifetimeScope.BeginLifetimeScope("AutofacWebRequest");
            _customerService = _hubLifetimeScope.Resolve<ICustomerService>();
            _chatCommonService = _hubLifetimeScope.Resolve<IChatCommonSerivce>();
            _userManager = ServiceFactory.GetService<ApplicationUserManager>();
        }


        /// <summary>
        /// Khách hàng kết nối chat tới tổng đài viên
        /// agentId gửi kèm theo trong static/app.js
        /// </summary>
        /// <param name="customerId"></param>
        /// <param name="threadId"></param>
        /// <param name="threadId"></param>
        public void ConnectCustomerToChannelChat(string customerId, long channelGroupId)
        {
            string connectionId = Context.ConnectionId;
            try
            {
                // check customer exist
                var customerDb = _customerService.GetById(customerId);
                customerDb.ConnectionID = connectionId;
                customerDb.StatusChatValue = CommonConstants.USER_ONLINE;
                customerDb.ConnectedDate = DateTime.Now;
                _customerService.Update(customerDb);
                _customerService.Save();

                // Kiểm tra customer tồn tại trong thread chat chưa
                var threadParticipantExits = _chatCommonService.CheckCustomerInThreadParticipant(customerId);
                if (threadParticipantExits != null)
                {
                    // Kết nối lại customer vào thread theo connectionId mới 
                    _context.Groups.Add(connectionId, threadParticipantExits.ThreadID.ToString());
                    OnchangeStatusCustomerOnline(channelGroupId, customerDb.ID, connectionId);
                    // Gửi lại thread ra customer
                    Clients.Caller.receiveThreadChat(threadParticipantExits.ThreadID.ToString(), customerDb.ID);
                    return;
                }

                // create thread chat
                var threadDb = _chatCommonService.AddThreadMessage();
                _chatCommonService.Save();

                // add to thread participant
                ThreadParticipant threadParticipant = new ThreadParticipant();
                threadParticipant.ChannelGroupID = channelGroupId;
                threadParticipant.CreatedDate = DateTime.Now;
                threadParticipant.ThreadID = threadDb.ID;
                threadParticipant.CustomerID = customerId;
                threadParticipant.CreatedDate = DateTime.Now;

                _chatCommonService.AddThreadParticipant(threadParticipant);
                _chatCommonService.Save();

                // add hub group
                _context.Groups.Add(connectionId, threadDb.ID.ToString());

                // Gửi lại thread ra customer
                Clients.Caller.receiveThreadChat(threadDb.ID, customerDb.ID);

                // Gửi thread thông báo customer mới tới agent
                Clients.Others.receiveNewCustomerToAgent(channelGroupId, threadDb.ID, customerDb);

            }
            catch (Exception ex)
            {
                BotLog.Error(ex.StackTrace + " " + ex.Source + " " + ex.Message);
                Clients.Caller.showErrorMessage("Error");
            }
        }


        public void SendTyping(long channelGroupId, string threadId, string agentId,string agentName,string customerId, bool isTyping,string typeUser)
        {
            string[] arrExcludeUserConnectionId = new string[] { Context.ConnectionId };
            _context.Clients.Group(threadId, arrExcludeUserConnectionId).receiveTyping(channelGroupId.ToString(), threadId, agentId, agentName, customerId, isTyping, typeUser);
        }

        public void SendMessage(string channelGroupId, string threadId, string message, string agentId,string customerId, string userName, string typeUser)
        {
            string[] arrExcludeUserConnectionId = new string[] { Context.ConnectionId };
            _context.Clients.Group(threadId, arrExcludeUserConnectionId).receiveMessages(channelGroupId, threadId, message, agentId, customerId, userName, typeUser);
        }

        public void SendActionChat(string channelGroupId, string threadId, string contentAction, string agentId, string customerId)
        {
            string[] arrExcludeUserConnectionId = new string[] { Context.ConnectionId };
            _context.Clients.Group(threadId, arrExcludeUserConnectionId).receiveActionChat(channelGroupId, threadId, contentAction, agentId, customerId);        
        }

        public void CheckCustomerFocusTabChat(string channelGroupId, string threadId, string customerId, bool isFocusTab)
        {
            string[] arrExcludeUserConnectionId = new string[] { Context.ConnectionId };
            _context.Clients.Group(threadId, arrExcludeUserConnectionId).receiveSignalCustomerFocusTabChat(channelGroupId, threadId, customerId, isFocusTab);
        }

        public void CheckAgentFocusTabChat(string channelGroupId, string threadId, string customerId, bool isFocusTab)
        {
            string[] arrExcludeUserConnectionId = new string[] { Context.ConnectionId };
            _context.Clients.Group(threadId, arrExcludeUserConnectionId).receiveSignalAgentFocusTabChat(channelGroupId, threadId, customerId, isFocusTab);
        }

        public void TransferCustomerToBot(string channelGroupId, string threadId, string customerId, int botId, bool isTransfer)
        {
            string[] arrExcludeUserConnectionId = new string[] { Context.ConnectionId };
            _context.Clients.Group(threadId, arrExcludeUserConnectionId).receiveSingalChatWithBot(channelGroupId, threadId, customerId, botId, isTransfer);
        }

        //public void ConnectAgentToListCustomer(string agentId, long channelGroupId)
        //{
        //    _connectionID = Context.ConnectionId;

        //    var agentDb = _userManager.FindById(agentId);
        //    agentDb.ConnectionID = _connectionID;
        //    agentDb.StatusChatValue = CommonConstants.USER_ONLINE;
        //    _userManager.Update(agentDb);

        //    // kết nối tới channelGroup
        //    // agent tham gia kênh
        //    _context.Groups.Add(_connectionID, channelGroupId.ToString());

        //    try
        //    {
        //        string filter = "tp.ChannelGroupID = " + channelGroupId + " and c.StatusChatValue = 200";
        //        var lstCustomerOnline = _chatCommonService.GetCustomerJoinChatByChannelGroupID(filter, "", 1, Int32.MaxValue, null).ToList();
        //        if (lstCustomerOnline != null && lstCustomerOnline.Count() != 0)
        //        {
        //            foreach (var customer in lstCustomerOnline)
        //            {
        //                // kết nối tới từng customer
        //                _context.Groups.Add(_connectionID, customer.ThreadID.ToString());
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        BotLog.Error(ex.StackTrace + ex.Message);
        //    }
        //}

        public void ConnectAgentToChannelChat(string agentId, long channelGroupId)
        {
            _connectionID = Context.ConnectionId;
            //var agentDb = _userManager.FindById(agentId);
            //agentDb.ConnectionID = _connectionID;
            //agentDb.StatusChatValue = CommonConstants.USER_ONLINE;
            //_userManager.Update(agentDb);
            OnchangeStatusAgentOnline(agentId, _connectionID);
            // agent tham gia kênh
            _context.Groups.Add(_connectionID, channelGroupId.ToString());
        }


        public void AgentJoinChatCustomer(string threadId)
        {
            _connectionID = Context.ConnectionId;
            _context.Groups.Add(_connectionID, threadId);
        }


        //public void OnchangeStatusAgent(long channelGroupId, string agentId, int status)
        //{
        //    var agentDb = _userManager.FindById(agentId);
        //    agentDb.StatusChatValue = status;
        //    _userManager.Update(agentDb);
        //    //send to client
        //    _context.Clients.All.getStatusAgent(channelGroupId, agentId, status);
        //}

        public void OnchangeStatusCustomerOffline(string connectionId)
        {
            bool isHasCustomer = false;
            string customerId = "";
            long channelGroupId = 0;

            var sqlConnection = new SqlConnection(_sqlConnection);
            sqlConnection.Open();
            SqlCommand command = new SqlCommand("Select top 1 * from [Customers] where ConnectionID = @connectionId and StatusChatValue = 200", sqlConnection);
            command.Parameters.AddWithValue("@connectionId", connectionId);
            using (SqlDataReader reader = command.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    isHasCustomer = true;
                    if (reader.Read())
                    {
                        customerId = (string)reader["ID"];
                        channelGroupId = (long)reader["ChannelGroupID"];
                    }
                }
            }
            command.ExecuteNonQuery();
            if(isHasCustomer == true)
            {
                SqlCommand command2 = new SqlCommand("UPDATE Customers SET StatusChatValue = @statusCode,ConnectionID = @connectionId,LogoutDate = @logoutDate Where ID = @customerId", sqlConnection);
                command2.Parameters.AddWithValue("@statusCode", 201);
                command2.Parameters.AddWithValue("@customerId", customerId);
                command2.Parameters.AddWithValue("@connectionId", String.Empty);
                command2.Parameters.AddWithValue("@logoutDate", DateTime.Now);
                command2.ExecuteNonQuery();

                _context.Clients.Group(channelGroupId.ToString()).getStatusCustomerOffline(channelGroupId, customerId);
            }
            sqlConnection.Close();
        }
        public void OnchangeStatusCustomerOnline(long channelGroupId, string customerId, string connectionId)
        {
            _context.Clients.Group(channelGroupId.ToString(), connectionId).getStatusCustomerOnline(channelGroupId, customerId);
        }

        public void OnchangeStatusAgentOffline(string connectionId)
        {
            bool isHasAgent = false;
            string agentId = "";
            var sqlConnection = new SqlConnection(_sqlConnection);
            sqlConnection.Open();
            SqlCommand command = new SqlCommand("Select top 1 * from [ApplicationUsers] where ConnectionID = @connectionId and StatusChatValue = 200", sqlConnection);
            command.Parameters.AddWithValue("@connectionId", connectionId);
            using (SqlDataReader reader = command.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    isHasAgent = true;
                    if (reader.Read())
                    {
                        agentId = (string)reader["Id"];
                    }
                }
            }
            command.ExecuteNonQuery();

            if (isHasAgent == true)
            {
                SqlCommand command2 = new SqlCommand("UPDATE ApplicationUsers SET StatusChatValue = @statusCode,ConnectionID = @connectionId Where Id = @agentId", sqlConnection);
                command2.Parameters.AddWithValue("@statusCode", 201);
                command2.Parameters.AddWithValue("@agentId", agentId);
                command2.Parameters.AddWithValue("@connectionId", String.Empty);
                command2.ExecuteNonQuery();
            }
            sqlConnection.Close();
        }

        public void OnchangeStatusAgentOnline(string agentId, string connectionId)
        {
            var sqlConnection = new SqlConnection(_sqlConnection);
            sqlConnection.Open();
            
            SqlCommand command2 = new SqlCommand("UPDATE ApplicationUsers SET StatusChatValue = @statusCode,ConnectionID = @connectionId Where Id = @agentId", sqlConnection);
            command2.Parameters.AddWithValue("@statusCode", 200);
            command2.Parameters.AddWithValue("@agentId", agentId);
            command2.Parameters.AddWithValue("@connectionId", connectionId);
            command2.ExecuteNonQuery();
            sqlConnection.Close();
        }

        public override Task OnConnected()
        {
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            _connectionID = Context.ConnectionId;
            if (Context.QueryString["isCustomerConnected"] != null)
            {
                //bool isCustomerConnected = bool.Parse(Context.QueryString["isCustomerConnected"]);
                // customer
                OnchangeStatusCustomerOffline(_connectionID);
            }
            else if(Context.QueryString["isAgentConnected"] != null)
            {
                // agent
                OnchangeStatusAgentOffline(_connectionID);
            }

            return base.OnDisconnected(stopCalled);
        }

        public void OnDisconnectedCustomer(string agentId, long channelGroupId)
        {
            //var agentDb = _userManager.FindById(agentId);
            //agentDb.StatusChatValue = CommonConstants.USER_OFFLINE;
            //_userManager.Update(agentDb);
            //_context.Clients.Group(channelGroupId.ToString()).getStatusAgentOffline(agentId);
        }
    }
}