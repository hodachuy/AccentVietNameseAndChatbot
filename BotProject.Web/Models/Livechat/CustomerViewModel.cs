using BotProject.Model.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BotProject.Web.Models.Livechat
{
    public class CustomerViewModel
    {
        public string ID { get; set; }
        public string ConnectionID { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Avatar { get; set; }
        public ApplicationChannel ApplicationChannels { set; get; }
        public long ChannelGroupID { set; get; }
        public int StatusChatValue { set; get; }
        public DateTime CreatedDate { set; get; }
        public DateTime? LogoutDate { set; get; }
        public int? BotID { set; get; }
        public virtual IEnumerable<DeviceViewModel> Devices { set; get; }
    }
    public class DeviceViewModel
    {
        public long ID { get; set; }
        public string IPAddress { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string Country { get; set; }
        public string Latitude { get; set; }
        public string Longtitude { get; set; }
        public string Timezone { get; set; }
        public string FullUserAgent { get; set; }
        public string OS { get; set; }
        public string Browser { get; set; }
        public bool IsMobile { get; set; }
        public string CustomerID { get; set; }
    }
}