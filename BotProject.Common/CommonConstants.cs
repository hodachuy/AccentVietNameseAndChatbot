using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace BotProject.Common
{
    public class CommonConstants
    {
        public const string SessionUser = "User";
        // session livechat
        public const string SessionLcCustomer = "Customer";
        public const string Administrator = "Administrator";
        public const string Visitor = "Visitor";
        public const string PathImage = "Card";

        public const string PostBackCard = "postback_card_";

        public const string CreateQnA = "Create";
        public const string UpdateQnA = "Update";

        public const string SessionUserBot = "UserBot";
        public const string SessionResultBot = "ResultBot";

        public const string ModuleAdminContact = "postback_module_admin_contact";
		public const string ModuleEngineerName = "postback_module_engineer_name";
		public const string ModulePhone = "postback_module_phone";
        public const string ModuleVoucher = "postback_module_voucher";
        public const string ModuleAge = "postback_module_age";
        public const string ModuleEmail = "postback_module_email";
        public const string ModuleName = "postback_module_name";
        public const string ModuleSearchAPI = "postback_module_api_search";

        public const string MethodeHTTP_POST = "POST";
        public const string MethodeHTTP_GET = "GET";

        public const string UserSay_IsStartDefault = "StartDefault";
        public const string UserSay_IsStartFirst = "StartFirst";
        public const string UserSay_IsStartLast = "StartLast";
        public const string UserSay_IsStartDouble = "StartDouble";

        public const string MdSearch_Luat = "luat";
        public const string MdSearch_Dell = "dell";
        public const string MdSearch_Digipro = "digipro";

        //DỊCH VỤ NGOÀI SỬA CHỮA
        public const string MdSearch_Digipro_OOW = "digipro-oow";

        public const string MdSearch_Yte = "y-te";

        public const string POSTBACK_MODULE = "postback_module";

        public const string TYPE_FACEBOOK = "facebook";
        public const string TYPE_ZALO = "zalo";
        public const string TYPE_KIOSK = "kiosk";

        public const string BOT_REQUEST_TEXT = "text";
        public const string BOT_REQUEST_PAYLOAD_POSTBACK = "payload_postback";


        #region Nhóm tài khoản livechat
        public const int GROUP_OWNER = 1;
        public const int GROUP_ADMIN = 2;
        public const int GROUP_SUPPORT = 3;
        #endregion

        /// <summary>
        /// Nhóm tài khoản quản lý toàn bộ hệ thống,
        /// xem tất cả danh sách bot tài khoản clone khác, viết dashboard
        /// </summary>
        public const int GROUP_SUPER_ADMIN = 4;



        public const int USER_ONLINE = 200;
        public const int USER_OFFLINE = 201;
    }
    public class ZaloConstans
    {
        public const string EVENT_USER_SEND_TEXT = "user_send_text";
    }
    public class FacebookConstans
    {
        public const string BOT_SETTING = "BotSetting";
    }


    public class PathServer
    {
        /// <summary>
        /// HttpContext.Current.Server.MapPath("~/File/Images/");
        /// System.Web.HttpContext.Current.Server.MapPath
        /// </summary>
        public static string PathImage = HttpContext.Current.Server.MapPath("~/File/Images/");
        /// <summary>
        /// HttpContext.Current.Server.MapPath("~/File/AIML/");
        /// </summary>
        public static string PathAIML = HttpContext.Current.Server.MapPath("~/File/AIML/");

        /// <summary>
        /// HttpContext.Current.Server.MapPath("~/File/Datasets_Training_Accent/")
        /// </summary>
        public static string PathAccent = HttpContext.Current.Server.MapPath("~/File/Datasets_Training_Accent/");

        public static string PathLogoSetting = HttpContext.Current.Server.MapPath("~/assets/images/logo/");

        public static string PathNLR = HttpContext.Current.Server.MapPath("~/File/NLP/");

        /// <summary>
        /// HttpContext.Current.Server.MapPath("~/File/AIML/AIML2Graphmaster/");
        /// </summary>
        public static string PathAIML2Graphmaster = HttpContext.Current.Server.MapPath("~/File/AIML2Graphmaster/");

        /// <summary>
        /// ~/File/MedicalSymptoms/
        /// </summary>
        public static string PathMedicalSymptoms = HttpContext.Current.Server.MapPath("~/File/MedicalSymptoms/");
    }

    public class PathConfig
    {
        /// <summary>
        /// HttpContext.Current.Server.MapPath("~/Web.config")
        /// </summary>
        public static string PathWebConfig = HttpContext.Current.Server.MapPath("~/Web.config");

        /// <summary>
        /// HttpContext.Current.Server.MapPath("~/AppSettings.config")
        /// </summary>
        public static string PathAppConfig = HttpContext.Current.Server.MapPath("~/AppSettings.config");
    }

    public class MessageBot
    {
        public const string BOT_HISTORY_HANDLE_001 = "Nhấn nút";
        public const string BOT_HISTORY_HANDLE_002 = "Không tìm thấy trong kịch bản";
        public const string BOT_HISTORY_HANDLE_003 = "Bot hiểu trả lời theo kịch bản";
        public const string BOT_HISTORY_HANDLE_004 = "Điền thông tin";
        public const string BOT_HISTORY_HANDLE_005 = "Gọi NLP API từ bên ngoài";
        public const string BOT_HISTORY_HANDLE_006 = "Gọi Search NLP trong cấu hình";
		public const string BOT_HISTORY_HANDLE_007 = "Nhấn Voucher";
        public const string BOT_HISTORY_HANDLE_008 = "Bot không hiểu và Search NLP không tìm thấy";
        public const string BOT_HISTORY_HANDLE_009 = "Nhấn Chat ADMIN";
    }

    public class ApiDomain
    {
        public static string LegalSite = ConfigHelper.ReadString("LegalSiteDomain");
        public static string MedicalSite = ConfigHelper.ReadString("MedicalSiteDomain");
    }
}
