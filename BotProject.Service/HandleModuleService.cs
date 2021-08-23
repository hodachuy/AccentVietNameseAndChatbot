using BotProject.Common;
using BotProject.Common.AppThird3PartyTemplate;
using BotProject.Common.DigiproService.Digipro;
using BotProject.Common.SendSmsMsgService;
using BotProject.Common.ViewModels;
using BotProject.Data.Infrastructure;
using BotProject.Model.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Xml;

namespace BotProject.Service
{
    public interface IHandleModuleServiceService
    {
        HandleResultBotViewModel HandleIsPhoneNumber(string number, int botID);
        HandleResultBotViewModel HandledIsEmail(string email, int botID);
        HandleResultBotViewModel HandledIsAge(string age, int botID);
        HandleResultBotViewModel HandleIsName(string name, int botID);
		HandleResultBotViewModel HandleIsEngineerName(string name, int botID);
        HandleResultBotViewModel HandleIsModuleKnowledgeInfoPatient(string mdName, int botID, string notFound);
        HandleResultBotViewModel HandleIsSearchAPI(string mdName, string mdSearchID, string notFound);
        HandleResultBotViewModel HandleIsVoucher(string phoneNumber, string mdVoucherID, string engineerName, string branchOTP, string Type = "");
        HandleResultBotViewModel HandleIsCheckOTP(string OTP, string phoneNumber, string mdVoucherID, string msgTimeoutOTP = "");
		HandleResultBotViewModel HandleIsAdminContact(string text, int botID, bool isFinishMessage = false);

        string SendSms(string strPhoneNumber, string strMsg); 
		void Save();
    }
    public class HandleModuleService : IHandleModuleServiceService
    {
        private string _cardTitleResendSmsOTP = "Gửi lại OTP";
        private string _cardPayloadID = ConfigHelper.ReadString("CARD_RESEND_SMS");
        private string _msgSMS = ConfigHelper.ReadString("MSG_SMS");

        private const string CharacterPattern = @"^[A-Za-z]+";
        private const string NumberPattern = @"^\d+$";
        private const string PhonePattern = @"^(\+[0-9]{9})$";
        private const string EmailPattern =
        @"^\s*[\w\-\+_']+(\.[\w\-\+_']+)*\@[A-Za-z0-9]([\w\.-]*[A-Za-z0-9])?\.[A-Za-z][A-Za-z\.]*[A-Za-z]$";

        private IMdPhoneService _mdPhoneService;
        private IMdEmailService _mdEmailService;
        private IMdAgeService _mdAgeService;
        private IModuleKnowledegeService _mdKnowledegeService;
        private IMdSearchService _mdSearchService;
        private IMdSearchCategoryService _mdSearchCategoryService;
		private IMdEngineerNameService _mdEngineerNameService;
		private IMdVoucherService _mdVoucherService;
        private IUserTelephoneService _userTelephoneService;
		private IMdAdminContactService _mdAdminContactService;
		private IUnitOfWork _unitOfWork;
        private IErrorService _errorService;

        // SEND SMS
        private const string APIKey = "CEB8AEF76FCB2A7E7D1C35BCD35A11";//Login to eSMS.vn to get this";//Dang ky tai khoan tai esms.vn de lay key//Register account at esms.vn to get key
        private const string SecretKey = "CD5E0FC78C4234F07FAADEC41B3B2E";//Login to eSMS.vn to get this";


        public HandleModuleService(IMdPhoneService mdPhoneService,
                                    IMdEmailService mdEmailService,
                                    IMdAgeService mdAgeService,
                                    IModuleKnowledegeService mdKnowledegeService,
                                    IMdSearchService mdSearchService,
                                    IMdSearchCategoryService mdSearchCategoryService,
                                    IMdVoucherService mdVoucherService,
                                    IUserTelephoneService userTelephoneService,
                                    IErrorService errorService,
									IMdEngineerNameService mdEngineerNameService,
									IMdAdminContactService mdAdminContactService,
									IUnitOfWork unitOfWork)
        {
            _mdPhoneService = mdPhoneService;
            _mdEmailService = mdEmailService;
            _mdAgeService = mdAgeService;
            _mdKnowledegeService = mdKnowledegeService;
            _mdSearchService = mdSearchService;
            _mdSearchCategoryService = mdSearchCategoryService;
            _mdVoucherService = mdVoucherService;
            _userTelephoneService = userTelephoneService;
			_mdEngineerNameService = mdEngineerNameService;
			_mdAdminContactService = mdAdminContactService;
			_unitOfWork = unitOfWork;
            _errorService = errorService;
        }
        public HandleResultBotViewModel HandleIsPhoneNumber(string number, int botID)
        {
            HandleResultBotViewModel rsHandle = new HandleResultBotViewModel();
            var mdPhoneDb = _mdPhoneService.GetByBotID(botID);
            string rsMessage = "";
            rsHandle.Postback = mdPhoneDb.Payload;
            if (number.Contains(Common.CommonConstants.ModulePhone))
            {
                rsHandle.Status = false;
                rsHandle.Message = tempText(mdPhoneDb.MessageStart);// sau này phát triển thêm random nhiều message, tạo aiml random li(thẻ error phone)
                rsMessage = mdPhoneDb.MessageStart;
                rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateText(rsMessage, "{{senderId}}").ToString();
				rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateText(rsMessage, "{{senderId}}").ToString();

				return rsHandle;
            }
            bool isNumber = ValidatePhoneNumber(number, true);
            if (!isNumber)
            {
                rsHandle.Status = false;
                rsHandle.Message = tempText(mdPhoneDb.MessageError);
                rsMessage = mdPhoneDb.MessageError;
                rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateText(rsMessage, "{{senderId}}").ToString();
				rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateText(rsMessage, "{{senderId}}").ToString();

				return rsHandle;
            }
            rsHandle.Status = true;
            rsHandle.Message = tempText(mdPhoneDb.MessageEnd);
            rsMessage = mdPhoneDb.MessageEnd;
            rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateText(rsMessage, "{{senderId}}").ToString();
			rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateText(rsMessage, "{{senderId}}").ToString();

			return rsHandle;
        }

		public HandleResultBotViewModel HandleIsAdminContact(string text, int botID, bool isFinishMessage = false)
		{
			HandleResultBotViewModel rsHandle = new HandleResultBotViewModel();
			var mdAdminContactDb = _mdAdminContactService.GetByBotID(botID);
			rsHandle.Postback = mdAdminContactDb.Payload;
			string rsMessage = "";
            if (text.Contains(Common.CommonConstants.ModuleAdminContact))
			{
				rsHandle.Status = false;
				rsHandle.Message = tempText(mdAdminContactDb.MessageStart1);
				if (String.IsNullOrEmpty(mdAdminContactDb.MessageStart1))
				{
					rsMessage = mdAdminContactDb.MessageStart2;
					rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdAdminContactDb.Payload, mdAdminContactDb.TitlePayload).ToString() + "split";
					rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdAdminContactDb.Payload, mdAdminContactDb.TitlePayload).ToString() + "split";
				}
				else
				{
					rsMessage = mdAdminContactDb.MessageStart1;
					rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateImage(rsMessage, "{{senderId}}").ToString() + "split" + FacebookTemplate.GetMessageTemplateTextAndQuickReply(mdAdminContactDb.MessageStart2, "{{senderId}}", mdAdminContactDb.Payload, mdAdminContactDb.TitlePayload).ToString();
					rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateImage(rsMessage, "{{senderId}}").ToString() + "split" + ZaloTemplate.GetMessageTemplateTextAndQuickReply(mdAdminContactDb.MessageStart2, "{{senderId}}", mdAdminContactDb.Payload, mdAdminContactDb.TitlePayload).ToString();
				}

				return rsHandle;
			}
			rsHandle.Status = true;
			//rsHandle.Message = tempText(mdAdminContactDb.MessageStart3);// nếu call tới follow thẻ khác trả về postback id card
			//rsMessage = mdAdminContactDb.MessageStart3;
			//rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdAdminContactDb.Payload, mdAdminContactDb.TitlePayload).ToString() + "split";
			//rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdAdminContactDb.Payload, mdAdminContactDb.TitlePayload).ToString() + "split";

			return rsHandle;
		}

		public HandleResultBotViewModel HandledIsEmail(string email, int botID)
        {
            HandleResultBotViewModel rsHandle = new HandleResultBotViewModel();
            var mdEmailDb = _mdEmailService.GetByBotID(botID);
            rsHandle.Postback = mdEmailDb.Payload;
            string rsMessage = "";
            rsHandle.Status = true;
            if (email.Contains(Common.CommonConstants.ModuleEmail))
            {
                rsHandle.Status = false;
                rsHandle.Message = tempText(mdEmailDb.MessageStart);
                rsMessage = mdEmailDb.MessageStart;
                rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateText(rsMessage, "{{senderId}}").ToString();
				rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateText(rsMessage, "{{senderId}}").ToString();

				return rsHandle;
            }
            bool isEmail = Regex.Match(email, EmailPattern).Success;
            if (!isEmail)
            {
                rsHandle.Status = false;
                rsHandle.Message = tempText(mdEmailDb.MessageError);
                rsMessage = mdEmailDb.MessageError;
                rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateText(rsMessage, "{{senderId}}").ToString();
				rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateText(rsMessage, "{{senderId}}").ToString();

				return rsHandle;
            }
            rsHandle.Status = true;
            rsHandle.Message = tempText(mdEmailDb.MessageEnd);// nếu call tới follow thẻ khác trả về postback id card
            rsMessage = mdEmailDb.MessageEnd;
            rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateText(rsMessage, "{{senderId}}").ToString();
			rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateText(rsMessage, "{{senderId}}").ToString();

			return rsHandle;
        }

        public HandleResultBotViewModel HandledIsAge(string age, int botID)
        {
            HandleResultBotViewModel rsHandle = new HandleResultBotViewModel();
            var mdAgeDb = _mdAgeService.GetByBotID(botID);
            string rsMessage = "";
            rsHandle.Postback = mdAgeDb.Payload;
            rsHandle.Status = true;
            if (age.Contains(Common.CommonConstants.ModuleAge))
            {
                rsHandle.Status = false;
                rsHandle.Message = tempText(mdAgeDb.MessageStart);
                rsMessage = mdAgeDb.MessageStart;
                rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateText(rsMessage, "{{senderId}}").ToString();
				rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateText(rsMessage, "{{senderId}}").ToString();

				return rsHandle;
            }
            bool isAge = Regex.Match(age, NumberPattern).Success;
            if (!isAge)
            {
                rsHandle.Status = false;
                rsHandle.Message = tempText(mdAgeDb.MessageError);
                rsMessage = mdAgeDb.MessageError;
                rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateText(rsMessage, "{{senderId}}").ToString();
				rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateText(rsMessage, "{{senderId}}").ToString();
				return rsHandle;
            }
            else
            {
                if (Int32.Parse(age) < 5)
                {
                    rsHandle.Status = false;
                    rsHandle.Message = tempText("Bạn còn quá nhỏ để tôi đưa ra tư vấn.");
                    rsMessage = "Bạn còn quá nhỏ để tôi đưa ra tư vấn.";
                    rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateText(rsMessage, "{{senderId}}").ToString();
					rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateText(rsMessage, "{{senderId}}").ToString();

					return rsHandle;
                }
                if (Int32.Parse(age) > 110)
                {
                    rsHandle.Status = false;
                    rsHandle.Message = tempText("Xin lỗi tôi không thể đưa ra tư vấn hợp lý với độ tuổi này.");
                    rsMessage = "Xin lỗi tôi không thể đưa ra tư vấn hợp lý với độ tuổi này.";
                    rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateText(rsMessage, "{{senderId}}").ToString();
					rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateText(rsMessage, "{{senderId}}").ToString();
					return rsHandle;
                }
            }
            rsHandle.Status = true;
            rsHandle.Message = tempText(mdAgeDb.MessageEnd);// nếu call tới follow thẻ khác trả về postback id card
            rsMessage = mdAgeDb.MessageEnd;
            rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateText(rsMessage, "{{senderId}}").ToString();
			rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateText(rsMessage, "{{senderId}}").ToString();

			return rsHandle;
        }

		public HandleResultBotViewModel HandleIsEngineerName(string name, int botID)
		{
			HandleResultBotViewModel rsHandle = new HandleResultBotViewModel();
			var mdEngineerNameDb = _mdEngineerNameService.GetByBotID(botID);
			rsHandle.Postback = mdEngineerNameDb.Payload;
			string rsMessage = "";
			if (name.Contains(Common.CommonConstants.ModuleEngineerName))
			{
				rsHandle.Status = false;
				rsHandle.Message = tempText(mdEngineerNameDb.MessageStart);
				rsMessage = mdEngineerNameDb.MessageStart;
				rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdEngineerNameDb.Payload, mdEngineerNameDb.TitlePayload).ToString();
                rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdEngineerNameDb.Payload, mdEngineerNameDb.TitlePayload).ToString();

                return rsHandle;
			}
			rsHandle.Status = true;
			rsHandle.Message = tempText(mdEngineerNameDb.MessageEnd);// nếu call tới follow thẻ khác trả về postback id card
			rsMessage = mdEngineerNameDb.MessageEnd;
			rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdEngineerNameDb.Payload, mdEngineerNameDb.TitlePayload).ToString();
			rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdEngineerNameDb.Payload, mdEngineerNameDb.TitlePayload).ToString();

			return rsHandle;
		}

		public HandleResultBotViewModel HandleIsName(string name, int botID)
        {
            HandleResultBotViewModel rsHandle = new HandleResultBotViewModel();
            //rsHandle.Postback = postbackCard;
            if (name.Contains(Common.CommonConstants.ModuleName))
            {
                rsHandle.Status = false;
                rsHandle.Message = tempText("Bạn tên là gì?");// sau này phát triển thêm random nhiều message, tạo aiml random li(thẻ error phone)
                return rsHandle;
            }
            bool isName = Regex.Match(name, CharacterPattern).Success;
            if (!isName)
            {
                rsHandle.Status = false;
                rsHandle.Message = tempText("Hình như không giống tên cho lắm?");
                return rsHandle;
            }
            rsHandle.Status = true;
            rsHandle.Message = tempText("Cảm ơn bạn đã cho biết tên!");
            return rsHandle;
        }

        public HandleResultBotViewModel HandleIsVoucher(string phoneNumber, string mdVoucherID,  string engineerName, string branchOTP, string Type = "")
        {
            HandleResultBotViewModel rsHandle = new HandleResultBotViewModel();
            string rsMessage = "";
            try
            {
                var mdVoucherDb = _mdVoucherService.GetByID(Int32.Parse(mdVoucherID));
                UserTelePhone usTelephone = new UserTelePhone();
                if (mdVoucherDb != null)
                {
                    if (!String.IsNullOrEmpty(mdVoucherDb.TitlePayload))
                    {
                        rsHandle.Postback = tempNodeBtnModule(mdVoucherDb.Payload, mdVoucherDb.TitlePayload);
                    }
                    if (phoneNumber.Contains(Common.CommonConstants.ModuleVoucher))
                    {
                        rsHandle.Status = false;
                        rsHandle.Message = tempText(mdVoucherDb.MessageStart);
                        rsMessage = mdVoucherDb.MessageStart;
                        rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdVoucherDb.Payload, mdVoucherDb.TitlePayload).ToString();
						rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdVoucherDb.Payload, mdVoucherDb.TitlePayload).ToString();

						return rsHandle;
                    }
                    else
                    {
                        //Tạo mã code đồng nhất cho 2 voucher nếu có
                        string telePhoneNumber = phoneNumber;
                        string serialNumber = engineerName;//""
                        string[] strArrSpecial = new string[] {"-", " ",",",":"};

                        //check phonenumber có kèm theo serialnumber không
                        foreach(var item in strArrSpecial)
                        {
                            if (phoneNumber.Contains(item))
                            {
                                var arrStrPhone = Regex.Split(phoneNumber, item);
								telePhoneNumber = arrStrPhone[0];
                                serialNumber = arrStrPhone[1];
                                break;
                            }
                        }

                        bool isNumber = ValidatePhoneNumber(telePhoneNumber, true);
                        if (isNumber == false)
                        {
                            rsHandle.Status = false;
                            rsHandle.Message = tempText(mdVoucherDb.MessageError);
                            rsMessage = mdVoucherDb.MessageError;
                            rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdVoucherDb.Payload, mdVoucherDb.TitlePayload).ToString();
							rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdVoucherDb.Payload, mdVoucherDb.TitlePayload).ToString();

							return rsHandle;
                        }

                        if (!String.IsNullOrEmpty(serialNumber))
                        {
                            bool isSerialNumber = true;// check serial number gọi api
                            if(isSerialNumber == false)
                            {
                                rsHandle.Status = false;
                                rsHandle.Message = tempText("Số SerialNumber không đúng");
                                rsMessage = "Số SerialNumber không đúng";
                                rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdVoucherDb.Payload, mdVoucherDb.TitlePayload).ToString();
                                rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdVoucherDb.Payload, mdVoucherDb.TitlePayload).ToString();

                                return rsHandle;
                            }

                        }

						string codeVoucher = mdVoucherDb.Code;
						string filter2 = "BotID = " + mdVoucherDb.BotID + " and m.Code Like N'" + codeVoucher + "' and u.TelephoneNumber = "+ telePhoneNumber + "";
						StoreProcUserTelephoneByVoucherViewModel strProcUserTelephoneDb = new StoreProcUserTelephoneByVoucherViewModel();
						strProcUserTelephoneDb = _userTelephoneService.GetUserTelephoneByVoucher(filter2, "", 1, 1, null).SingleOrDefault();

						//var usTelephoneDb = _userTelephoneService.GetByPhoneAndMdVoucherId(telePhoneNumber, Int32.Parse(mdVoucherID));

                        if (strProcUserTelephoneDb != null)
                        {
                            if (strProcUserTelephoneDb.IsReceived)
                            {
                                rsHandle.Status = false;
                                rsHandle.Message = tempText("Số điện thoại của bạn đã được nhận voucher trước đó.");
                                rsMessage = "Số điện thoại của bạn đã được nhận voucher trước đó.";
                                rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdVoucherDb.Payload, mdVoucherDb.TitlePayload).ToString();
								rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdVoucherDb.Payload, mdVoucherDb.TitlePayload).ToString();

								return rsHandle;
                            }
                        }

                        string codeOTP = RandomStr(5);

                        //rsHandle.Status = true;
                        //rsHandle.Message = tempText("Vui lòng nhập mã OTP được gửi tới số điện thoại");
                        //rsMessage = "Vui lòng nhập mã OTP " + codeOTP;//mdVoucherDb.MessageError;// "Vui lòng nhập mã OTP được gửi tới số điện thoại";
                        //rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdVoucherDb.Payload, mdVoucherDb.TitlePayload).ToString();
                        //rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdVoucherDb.Payload, mdVoucherDb.TitlePayload).ToString();

                        //if (strProcUserTelephoneDb == null)
                        //{
                        //    usTelephone.Code = codeOTP;
                        //    usTelephone.IsReceive = false;
                        //    usTelephone.IsDelete = false;
                        //    usTelephone.TelephoneNumber = telePhoneNumber;
                        //    usTelephone.TypeService = "Voucher";
                        //    usTelephone.MdVoucherID = Int32.Parse(mdVoucherID);
                        //    usTelephone.NumberReceive = 1;
                        //    usTelephone.Type = Type;
                        //    if (!String.IsNullOrEmpty(branchOTP))
                        //    {
                        //        usTelephone.BranchOTP = branchOTP;
                        //    }

                        //    usTelephone.CreatedDate = DateTime.Now;
                        //    usTelephone.SerialNumber = serialNumber;

                        //    if (!String.IsNullOrEmpty(Type))
                        //    {

                        //        int numberOrder = 10000;
                        //        string filter = "m.BotID = " + mdVoucherDb.BotID + " and u.Type Like N'" + Type + "'";
                        //        StoreProcUserTelephoneByVoucherViewModel userTelePhoneDb = new StoreProcUserTelephoneByVoucherViewModel();
                        //        userTelePhoneDb = _userTelephoneService.GetUserTelephoneByVoucher(filter, "", 1, 1, null).SingleOrDefault();
                        //        if (userTelePhoneDb == null)
                        //        {
                        //            numberOrder = 10001;
                        //        }
                        //        else
                        //        {
                        //            numberOrder = 10000 + userTelePhoneDb.Total + 1;
                        //        }
                        //        if (Type == "facebook")
                        //        {
                        //            usTelephone.NumberOrder = "F" + numberOrder.ToString();
                        //        }
                        //        if (Type == "zalo")
                        //        {
                        //            usTelephone.NumberOrder = "Z" + numberOrder.ToString();
                        //        }
                        //    }

                        //    _userTelephoneService.Create(usTelephone);
                        //    _unitOfWork.Commit();

                        //}
                        //else
                        //{
                        //    var usT = _userTelephoneService.GetById(strProcUserTelephoneDb.ID);
                        //    usT.IsReceive = false;
                        //    usT.IsDelete = false;
                        //    usT.Code = codeOTP;
                        //    usT.SerialNumber = serialNumber;
                        //    if (!String.IsNullOrEmpty(branchOTP))
                        //    {
                        //        usT.BranchOTP = branchOTP;
                        //    }
                        //    _userTelephoneService.Update(usT);
                        //    _unitOfWork.Commit();
                        //}
                        //return rsHandle;


                        //SendSmsService sm = new SendSmsService();
                        //string rsSmsMsg = sm.SendSmsMsg(telePhoneNumber, codeOTP + " la ma OTP cua ban tu DIGIPRO");
                        string rsSmsMsg = SendSms(telePhoneNumber, codeOTP + " la ma OTP cua ban tu DIGIPRO");
                        if (rsSmsMsg == "100")//thành công
                        {
                            rsHandle.Status = true;
                            rsHandle.Message = tempText("Vui lòng nhập mã OTP được gửi tới số điện thoại");
                            rsMessage = mdVoucherDb.MessageError;// "Vui lòng nhập mã OTP được gửi tới số điện thoại";//"Vui lòng nhập mã OTP " + codeOTP;//
                            rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdVoucherDb.Payload, mdVoucherDb.TitlePayload).ToString();
                            rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdVoucherDb.Payload, mdVoucherDb.TitlePayload).ToString();

                            if (strProcUserTelephoneDb == null)
                            {
                                usTelephone.Code = codeOTP;
                                usTelephone.IsReceive = false;
                                usTelephone.IsDelete = false;
                                usTelephone.TelephoneNumber = telePhoneNumber;
                                usTelephone.TypeService = "Voucher";
                                usTelephone.MdVoucherID = Int32.Parse(mdVoucherID);
                                usTelephone.NumberReceive = 1;
                                usTelephone.Type = Type;
                                if (!String.IsNullOrEmpty(branchOTP))
                                {
                                    usTelephone.BranchOTP = branchOTP;
                                }

                                usTelephone.CreatedDate = DateTime.Now;
                                usTelephone.SerialNumber = serialNumber;

                                if (!String.IsNullOrEmpty(Type))
                                {

                                    int numberOrder = 10000;
                                    string filter = "m.BotID = " + mdVoucherDb.BotID + " and u.Type Like N'" + Type + "'";
                                    StoreProcUserTelephoneByVoucherViewModel userTelePhoneDb = new StoreProcUserTelephoneByVoucherViewModel();
                                    userTelePhoneDb = _userTelephoneService.GetUserTelephoneByVoucher(filter, "", 1, 1, null).SingleOrDefault();
                                    if (userTelePhoneDb == null)
                                    {
                                        numberOrder = 10001;
                                    }
                                    else
                                    {
                                        numberOrder = 10000 + userTelePhoneDb.Total + 1;
                                    }
                                    if (Type == "facebook")
                                    {
                                        usTelephone.NumberOrder = "F" + numberOrder.ToString();
                                    }
                                    if (Type == "zalo")
                                    {
                                        usTelephone.NumberOrder = "Z" + numberOrder.ToString();
                                    }
                                }

                                _userTelephoneService.Create(usTelephone);
                                _unitOfWork.Commit();

                            }
                            else
                            {
                                var usT = _userTelephoneService.GetById(strProcUserTelephoneDb.ID);
                                usT.IsReceive = false;
                                usT.IsDelete = false;
                                usT.Code = codeOTP;
                                usT.SerialNumber = serialNumber;
                                if (!String.IsNullOrEmpty(branchOTP))
                                {
                                    usT.BranchOTP = branchOTP;
                                }
                                _userTelephoneService.Update(usT);
                                _unitOfWork.Commit();
                            }
                            return rsHandle;
                        }
                        else
                        {
                            rsHandle.Status = false;
                            rsHandle.Message = tempText("Tin nhắn OTP không gửi được");
                            rsMessage = "Tin nhắn OTP không gửi được";
                            rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdVoucherDb.Payload, mdVoucherDb.TitlePayload).ToString();
                            rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdVoucherDb.Payload, mdVoucherDb.TitlePayload).ToString();

                            return rsHandle;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                rsHandle.Message = tempText("Not found");
                rsMessage = "Not found";
                rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", "", "").ToString();
                rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", "", "").ToString();
            }
            rsMessage = "Not found";
            rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", "", "").ToString();
            rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", "", "").ToString();
            return rsHandle;
        }
        public HandleResultBotViewModel HandleIsCheckOTP(string OTP, string phoneNumber, string mdVoucherID, string msgTimeOutOTP = "")
        {
            HandleResultBotViewModel rsHandle = new HandleResultBotViewModel();
            try
            {
                string rsMessage = "";

                var mdVoucherDb = _mdVoucherService.GetByID(Int32.Parse(mdVoucherID));

                var usTelephoneDb = _userTelephoneService.GetByPhoneAndMdVoucherId(phoneNumber, Int32.Parse(mdVoucherID));
                if(usTelephoneDb != null)
                {
                    if(usTelephoneDb.Code == "timeout")
                    {
                        rsHandle.Status = false;
                        rsHandle.Message = tempText(msgTimeOutOTP);
                        rsMessage = msgTimeOutOTP;
                        rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateTextAndQuickReplyMulti(rsMessage, "{{senderId}}", mdVoucherDb.Payload, mdVoucherDb.TitlePayload,_cardPayloadID,_cardTitleResendSmsOTP).ToString();
                        rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateTextAndQuickReplyMulti(rsMessage, "{{senderId}}", mdVoucherDb.Payload, mdVoucherDb.TitlePayload, _cardPayloadID, _cardTitleResendSmsOTP).ToString();
                        return rsHandle;
                    }
                }

                bool isContainOTP = _userTelephoneService.CheckContainOTP(OTP, phoneNumber, Int32.Parse(mdVoucherID));
                if (isContainOTP)
                {
                    rsHandle.Status = true;
                    rsHandle.Message = tempImage(mdVoucherDb.Image);
                    rsMessage = mdVoucherDb.Image;
                    //var usTelephoneDb = _userTelephoneService.GetByPhoneAndMdVoucherId(phoneNumber, Int32.Parse(mdVoucherID));
                    if (usTelephoneDb != null)
                    {
                        var usT = _userTelephoneService.GetById(usTelephoneDb.ID);
                        usT.IsReceive = true;
                        usT.IsDelete = false;
                        _userTelephoneService.Update(usT);
                        _unitOfWork.Commit();
                    }
                    if (String.IsNullOrEmpty(mdVoucherDb.MessageEnd))
                    {
                        rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateImage(rsMessage, "{{senderId}}").ToString() + "split";
                        rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateImage(rsMessage, "{{senderId}}").ToString() + "split"; ;
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(usTelephoneDb.NumberOrder))
                        {
                            int num = 1000 + usTelephoneDb.ID;
                            usTelephoneDb.NumberOrder = num.ToString();
                        }
                        string messageEnd = Regex.Replace(mdVoucherDb.MessageEnd, "{{stt}}", usTelephoneDb.NumberOrder);

                        rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateImage(rsMessage, "{{senderId}}").ToString() + "split" + FacebookTemplate.GetMessageTemplateText(messageEnd, "{{senderId}}").ToString();
                        rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateImage(rsMessage, "{{senderId}}").ToString() + "split" + ZaloTemplate.GetMessageTemplateText(messageEnd, "{{senderId}}").ToString();
                        //SendSmsService sm = new SendSmsService();

                        string msgSMS = Regex.Replace(_msgSMS, "{{stt}}", usTelephoneDb.NumberOrder);
                        //string rsSmsMsg = sm.SendSmsMsg(phoneNumber, msgSMS);
                        string rsSmsMsg = SendSms(phoneNumber, msgSMS);
                    }
                }
                else
                {
                    if (!String.IsNullOrEmpty(mdVoucherDb.TitlePayload))
                    {
                        rsHandle.Postback = tempNodeBtnModule(mdVoucherDb.Payload, mdVoucherDb.TitlePayload);
                    }
                    rsHandle.Status = false;
                    rsHandle.Message = tempText("Mã OTP không đúng");
                    rsMessage = "Mã OTP không đúng";
                    rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdVoucherDb.Payload, mdVoucherDb.TitlePayload).ToString();
                    rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdVoucherDb.Payload, mdVoucherDb.TitlePayload).ToString();

                }
                return rsHandle;
            }
            catch(Exception ex)
            {
                LogError(ex.Message);
                rsHandle.Message = tempText("Not found");
                string rsMessage = "Not found";
                rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", "", "").ToString();
                rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", "", "").ToString();
            }
            string msg = "Not found";
            rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateTextAndQuickReply(msg, "{{senderId}}", "", "").ToString();
            rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateTextAndQuickReply(msg, "{{senderId}}", "", "").ToString();
            return rsHandle;
        }

        public HandleResultBotViewModel HandleIsModuleKnowledgeInfoPatient(string mdName, int botID, string notFound)
        {
            HandleResultBotViewModel rsHandle = new HandleResultBotViewModel();
            string mdInfoPatientID = mdName.Replace(".", String.Empty).Replace("postback_module_med_get_info_patient_", "");
            var mdGetInfoPatientDb = _mdKnowledegeService.GetByMdMedInfoPatientID(Int32.Parse(mdInfoPatientID));
            if (mdGetInfoPatientDb != null)
            {
                if (!String.IsNullOrEmpty(mdGetInfoPatientDb.OptionText))
                {
                    var arrOpt = mdGetInfoPatientDb.OptionText.Split(new string[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                    rsHandle.Message = TemplateOptionBot(arrOpt, mdGetInfoPatientDb.Title, mdGetInfoPatientDb.Payload, mdInfoPatientID, notFound);
                }
            }
            return rsHandle;
        }

        public HandleResultBotViewModel HandleIsSearchAPI(string text, string mdSearchID, string notFound)
        {
            text = text.ToLower();
            HandleResultBotViewModel rsHandle = new HandleResultBotViewModel();
            var mdSearchDb = _mdSearchService.GetByID(Int32.Parse(mdSearchID));
            string rsMessage = "";
            if (!String.IsNullOrEmpty(mdSearchDb.TitlePayload))
            {
                rsHandle.Postback = tempNodeBtnModule(mdSearchDb.Payload, mdSearchDb.TitlePayload);
            }
            try
            {
                // Đối với module tìm kiếm , tắt mở check cho thẻ tiếp theo, khi có message error hoặc không.
                if (!mdSearchDb.Equals(null))
                {
                    if (text.Contains(Common.CommonConstants.ModuleSearchAPI))
                    {
                        rsHandle.Status = false;
                        rsHandle.Message = tempText(mdSearchDb.MessageStart);
                        rsMessage = mdSearchDb.MessageStart;
                    }
                    else
                    {
                        rsHandle.Status = false;
                        var mdSearchCategory = _mdSearchCategoryService.GetById(mdSearchDb.MdSearchCategoryID);
                        if (mdSearchCategory.Alias.ToLower() == Common.CommonConstants.MdSearch_Luat)
                        {
                            if (mdSearchDb.UrlAPI.Contains(apiSearchLegal))
                            {
                                rsHandle.ResultAPI = GetModuleApiSearchLegal(text, mdSearchDb.ParamAPI, mdSearchDb.UrlAPI, mdSearchDb.KeyAPI, mdSearchDb.MethodeAPI);
                                if (!String.IsNullOrEmpty(rsHandle.ResultAPI))
                                {                                  
                                    var dataListLegal = new JavaScriptSerializer
                                    {
                                        MaxJsonLength = Int32.MaxValue,
                                        RecursionLimit = 100
                                    }.Deserialize<List<LegalApiModel>>(rsHandle.ResultAPI);
                                    if (dataListLegal.Count() != 0)
                                    {
                                        dataListLegal = dataListLegal.Take(4).ToList();
                                        string resultTotal = "Tìm thấy " + dataListLegal.Count() + " kết quả liên quan văn bản luật";
                                        // kèm payload để thoát tra cứu
                                        rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateText(resultTotal, "{{senderId}}").ToString() + "split" + FacebookTemplate.GetMessageTemplateGenericByListLegal("{{senderId}}", dataListLegal,"", mdSearchDb.Payload, mdSearchDb.TitlePayload).ToString();
                                        rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateText(resultTotal,"{{senderId}}").ToString()
                                            + "split" 
                                            + ZaloTemplate.GetMessageTemplateGenericByListLegal("{{senderId}}", dataListLegal).ToString()
                                            + "split"
                                            + ZaloTemplate.GetMessageTemplateTextAndQuickReply("Anh/chị chọn nút bên dưới nếu muốn thoát tra cứu", "{{senderId}}", mdSearchDb.Payload, mdSearchDb.TitlePayload).ToString();
                                        return rsHandle;
                                    }                                    
                                }

                                rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateTextAndQuickReply(mdSearchDb.MessageError, "{{senderId}}", mdSearchDb.Payload, mdSearchDb.TitlePayload).ToString();
                                rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateTextAndQuickReply(mdSearchDb.MessageError, "{{senderId}}", mdSearchDb.Payload, mdSearchDb.TitlePayload).ToString();

                                rsHandle.Message = tempText(mdSearchDb.MessageError);
                                rsMessage = mdSearchDb.MessageError;
                                return rsHandle;
                            }
                            else if(mdSearchDb.UrlAPI.Contains(apiSearchArticle))
                            {
                                rsHandle.ResultAPI = GetModuleApiSearchArticle(text, mdSearchDb.ParamAPI, mdSearchDb.UrlAPI, mdSearchDb.KeyAPI, mdSearchDb.MethodeAPI);
                                if (!String.IsNullOrEmpty(rsHandle.ResultAPI))
                                {
                                    var resultArticles = new JavaScriptSerializer
                                    {
                                        MaxJsonLength = Int32.MaxValue,
                                        RecursionLimit = 100
                                    }.Deserialize<Dictionary<string, string>>(rsHandle.ResultAPI);

                                    string totalArticle = resultArticles["total"];
                                    if (totalArticle != "0")
                                    {
                                        string resultTotal = "Tìm thấy " + totalArticle + " kết quả liên quan điều luật";
                                        string url = "https://trogiupluat.vn/dieu-luat-lien-quan.html?content=" + HttpUtility.UrlEncode(text);
                                        // kèm payload để thoát tra cứu
                                        rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateTextAndButtonLink(resultTotal, "{{senderId}}", url, "Xem chi tiết", mdSearchDb.Payload, mdSearchDb.TitlePayload).ToString();
                                        rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateTextAndButtonLink(resultTotal, "{{senderId}}", url, "Xem chi tiết").ToString()
                                            +"split"
                                            + ZaloTemplate.GetMessageTemplateTextAndQuickReply("Anh/chị chọn nút bên dưới nếu muốn thoát tra cứu", "{{senderId}}", mdSearchDb.Payload, mdSearchDb.TitlePayload).ToString();
                                        return rsHandle;
                                    }
                                }
                                rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateTextAndQuickReply(mdSearchDb.MessageError, "{{senderId}}", mdSearchDb.Payload, mdSearchDb.TitlePayload).ToString();
                                rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateTextAndQuickReply(mdSearchDb.MessageError, "{{senderId}}", mdSearchDb.Payload, mdSearchDb.TitlePayload).ToString();

                                rsHandle.Message = tempText(mdSearchDb.MessageError);
                                rsMessage = mdSearchDb.MessageError;
                                return rsHandle;
                            }
                            else
                            {
                                rsHandle.ResultAPI = GetModuleApiSearchQuestionAnwser(text, mdSearchDb.ParamAPI, mdSearchDb.UrlAPI, mdSearchDb.KeyAPI, mdSearchDb.MethodeAPI);
                                if (String.IsNullOrEmpty(rsHandle.ResultAPI))
                                {
                                    rsHandle.Message = tempText(mdSearchDb.MessageError);
                                    rsMessage = mdSearchDb.MessageError;
                                }
                            }

                            if (String.IsNullOrEmpty(rsHandle.ResultAPI))
                            {
                                rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdSearchDb.Payload, mdSearchDb.TitlePayload).ToString();
                                rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdSearchDb.Payload, mdSearchDb.TitlePayload).ToString();
                            }

                            return rsHandle;
                        }
                        if (mdSearchCategory.Alias.ToLower() == Common.CommonConstants.MdSearch_Dell)
                        {
                            var resultDell = DellServices.GetAssetHeader(text);
                            if (resultDell != null)
                            {
                                rsHandle.Message = tempText(resultDell.TextWarranty);
                                rsMessage = resultDell.TextWarranty;
                            }
                            else
                            {
                                rsHandle.Message = tempText(mdSearchDb.MessageError);
                                rsMessage = mdSearchDb.MessageError;
                            }
                        }
                        if (mdSearchCategory.Alias.ToLower() == Common.CommonConstants.MdSearch_Digipro)
                        {
                            bool IsOOW = false;// lấy idsla != 4
                            var resultDigipro = DigiproService.GetDetailServiceDigiproByRofOrSvtag(mdSearchDb.UrlAPI.Trim(), text, IsOOW);
                            if (resultDigipro != null)
                            {
                                rsHandle.Message = tempText(resultDigipro);
                                rsMessage = resultDigipro;
                            }
                            else
                            {
                                rsHandle.Message = tempText(mdSearchDb.MessageError);
                                rsMessage = mdSearchDb.MessageError;
                            }
                        }
                        if (mdSearchCategory.Alias.ToLower() == Common.CommonConstants.MdSearch_Digipro_OOW)
                        {
                            bool IsOOW = true;// lấy idsla = 4
                            var resultDigipro = DigiproService.GetDetailServiceDigiproByRofOrSvtag(mdSearchDb.UrlAPI.Trim(), text, IsOOW);
                            if (resultDigipro != null)
                            {
                                rsHandle.Message = tempText(resultDigipro);
                                rsMessage = resultDigipro;
                            }
                            else
                            {
                                rsHandle.Message = tempText(mdSearchDb.MessageError);
                                rsMessage = mdSearchDb.MessageError;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                rsHandle.Message = tempText(mdSearchDb.MessageError);
                rsMessage = mdSearchDb.MessageError;
                //throw new Exception(ex.ToString());
            }
            rsHandle.TemplateJsonFacebook = FacebookTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdSearchDb.Payload, mdSearchDb.TitlePayload).ToString();
			rsHandle.TemplateJsonZalo = ZaloTemplate.GetMessageTemplateTextAndQuickReply(rsMessage, "{{senderId}}", mdSearchDb.Payload, mdSearchDb.TitlePayload).ToString();

			return rsHandle;
        }

        private static string TemplateOptionBot(string[] arrOpt, string title, string postback, string mdInfoPatientID, string notFound)
        {
            if (!String.IsNullOrEmpty(notFound))
            {
                title = notFound;
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<div class=\"_4xkn clearfix\">");
            sb.AppendLine("<div class=\"profilePictureColumn\" style=\"bottom: 0px;\">");
            sb.AppendLine("<div class=\"_4cqr\">");
            sb.AppendLine("<img class=\"profilePicture img\" src=\"{{image_logo}}\"/>");
            sb.AppendLine("<div class=\"clearfix\"></div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class=\"messages\">");
            sb.AppendLine("<div class=\"_21c3\">");
            sb.AppendLine("<div class=\"clearfix _2a0-\">");
            sb.AppendLine("<div class=\"_4xko _4xkr _tmpB\" tabindex=\"0\" role=\"button\" style=\"background-color: rgb(241, 240, 240);font-family: Segoe UI Light;\">");
            sb.AppendLine("<span>");
            sb.AppendLine("<span>" + title + "</span>");
            sb.AppendLine("</span>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class=\"_4xko _4xkr _tmpB\" tabindex=\"0\" role=\"button\" style=\"background-color: rgb(241, 240, 240);font-family: Segoe UI Light; width:100%\">");
            sb.AppendLine("<ul>");
            foreach (var item in arrOpt)
            {
                sb.AppendLine("<li><input type=\"checkbox\" value=\"" + item + "\" class=\"chk-opt-module-" + mdInfoPatientID + "\"/>" + item + "</li>");
            }
            sb.AppendLine("</ul>");
            sb.AppendLine("</div>");
            if (!String.IsNullOrEmpty(postback))
            {
                sb.AppendLine("<div class=\"_4xko _2k7w _4xkr\">");
                sb.AppendLine("<div class=\"_2k7x\">");
                sb.AppendLine("<div class=\"_6b7s\">");
                sb.AppendLine("<div class=\"_6ir5\">");
                sb.AppendLine("<div class=\"_4bqf _6ir3\">");
                sb.AppendLine("<a class=\"_6ir4 _6ir4_module\" data-id=\"" + mdInfoPatientID + "\" data-postback =\"module_patient_" + postback + "\" href=\"#\" style=\"color: {{color}}\">Tiếp tục</a>");
                sb.AppendLine("</div>");
                sb.AppendLine("</div>");
                sb.AppendLine("</div>");
                sb.AppendLine("</div>");
                sb.AppendLine("</div>");
            }
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");

            return sb.ToString();
        }

        /// <summary>
        /// Template text UI BOT
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string tempText(string text)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<div class=\"_4xkn clearfix\">");
            sb.AppendLine("     <div class=\"profilePictureColumn\" style=\"bottom: 0px;\">");
            sb.AppendLine("         <div class=\"_4cqr\">");
            sb.AppendLine("             <img class=\"profilePicture img\" src=\"{{image_logo}}\" alt=\"\">");
            sb.AppendLine("             <div class=\"clearfix\"></div>");
            sb.AppendLine("         </div>");
            sb.AppendLine("     </div>");
            sb.AppendLine("     <div class=\"messages\">");
            sb.AppendLine("         <div class=\"_21c3\">");
            sb.AppendLine("             <div class=\"clearfix _2a0-\">");

            sb.AppendLine("<div class=\"_4xko _4xkr _tmpB\" tabindex=\"0\" role=\"button\" style=\"background-color:rgb(241, 240, 240); font-family: Segoe UI Light\">");
            sb.AppendLine("     <span>");
            sb.AppendLine("         <span>" + text + "</span>");
            sb.AppendLine("     </span>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            return sb.ToString();
        }

        private static string tempImage(string urlImage)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<div class=\"_4xkn clearfix\">");
            sb.AppendLine("     <div class=\"profilePictureColumn\" style=\"bottom: 0px;\">");
            sb.AppendLine("         <div class=\"_4cqr\">");
            sb.AppendLine("             <img class=\"profilePicture img\" src=\"{{image_logo}}\" alt=\"\">");
            sb.AppendLine("             <div class=\"clearfix\"></div>");
            sb.AppendLine("         </div>");
            sb.AppendLine("     </div>");
            sb.AppendLine("     <div class=\"messages\">");
            sb.AppendLine("         <div class=\"_21c3\">");
            sb.AppendLine(" <div class=\"clearfix _2a0-\">");
            sb.AppendLine("     <div class=\"_6j0s\" style=\"background-image:url(&quot;" + ConfigHelper.ReadString("Domain") + urlImage + "&quot;); background-position: center center; height: 250px; width: 100%;background-repeat: no-repeat;background-size: contain;\"></div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            return sb.ToString();
        }

        private static string tempNodeBtnModule(string payload, string titlePayload)
        {
            StringBuilder sbPostback = new StringBuilder();
            sbPostback.AppendLine("<div class=\"_6biu\">");
            sbPostback.AppendLine("                                                                    <div  class=\"_23n- form_carousel\">");
            sbPostback.AppendLine("                                                                        <div class=\"_4u-c\">");
            sbPostback.AppendLine("                                                                            <div index=\"0\" class=\"_a28 lst_btn_carousel\">");
            sbPostback.AppendLine("                                                                                <div class=\"_a2e\">");
            sbPostback.AppendLine(" <div class=\"_2zgz _2zgz_postback\">");
            sbPostback.AppendLine("      <div class=\"_4bqf _6biq _6bir\" tabindex=\"0\" role=\"button\" data-postback =\"" + payload + "\" style=\"border-color: {{color}} color: {{color}}\">" + titlePayload + "</div>");
            sbPostback.AppendLine(" </div>");
            sbPostback.AppendLine("                                                                                </div>");
            sbPostback.AppendLine("                                                                            </div>");
            sbPostback.AppendLine("                                                                            <div class=\"_4u-f\">");
            sbPostback.AppendLine("                                                                                <iframe aria-hidden=\"true\" class=\"_1_xb\" tabindex=\"-1\"></iframe>");
            sbPostback.AppendLine("                                                                            </div>");
            sbPostback.AppendLine("                                                                        </div>");
            return sbPostback.ToString();
        }

        private static bool ValidatePhoneNumber(string phone, bool IsRequired)
        {
            if (string.IsNullOrEmpty(phone) & !IsRequired)
                return true;

            if (string.IsNullOrEmpty(phone) & IsRequired)
                return false;

            var cleaned = RemoveNonNumeric(phone);
            if (IsRequired)
            {
                if (cleaned.Length == 10)
                    return true;
                else
                    return false;
            }
            else
            {
                if (cleaned.Length == 0)
                    return true;
                else if (cleaned.Length > 0 & cleaned.Length < 10)
                    return false;
                else if (cleaned.Length == 10)
                    return true;
                else
                    return false; // should never get here
            }
        }

        private static string RemoveNonNumeric(string phone)
        {
            return Regex.Replace(phone, @"[^0-9]+", "");
        }

        private static string RandomStr(int iStrLen)
        {
            Random m_rand = new Random();
            int iRandom = m_rand.Next(0, 999999999);
            string strRandom = iRandom.ToString().PadLeft(iStrLen, '0');
            strRandom = ((strRandom.Length == iStrLen) ? strRandom : (strRandom.Substring(strRandom.Length - iStrLen, iStrLen)));
            return strRandom;
        }

        #region --DATA SOURCE API--
        private string apiRelateQA = "/api/get_related_pairs";
        private string apiSearchLegal = "/api/legal/SearchLegalDoc";
        private string apiSearchArticle = "/api/article/search-relate";
        private string urlAPISearchLegal = "https://trogiupluat.vn";
        private string GetModuleApiSearchQuestionAnwser(string contentText, string param, string urlAPI, string keyAPI, string methodeHttp)
        {
            string responseString;
            if (String.IsNullOrEmpty(param))
            {
                param = "question=" + contentText + "&type=leg&number=10";
            }
            else
            {
                param = "question=" + contentText + "&type=leg&number=10&field=" + param;
            }
            responseString = ExcuteModuleSearchAPI(apiRelateQA, param, urlAPI, keyAPI, methodeHttp);
            if (responseString != null)
            {
                var lstQues = new JavaScriptSerializer
                {
                    MaxJsonLength = Int32.MaxValue,
                    RecursionLimit = 100
                }
                .Deserialize<List<dynamic>>(responseString);
            }
            return responseString;
        }

        private string GetModuleApiSearchLegal(string contentText, string param, string urlAPI, string keyAPI, string methodeHttp)
        {
            param = "keyword=" + contentText;
            return ExcuteModuleSearchAPI(apiSearchLegal, param, urlAPISearchLegal, keyAPI, methodeHttp);
        }

        private string GetModuleApiSearchArticle(string contentText, string param, string urlAPI, string keyAPI, string methodeHttp)
        {
            param = "keyword=" + contentText;
            return ExcuteModuleSearchAPI(apiSearchArticle, param, urlAPISearchLegal, keyAPI, methodeHttp);
        }

        private string ExcuteModuleSearchAPI(string NameFuncAPI, string param, string UrlAPI, string KeySecrectAPI, string Type = "Post")
        {
            string result = null;
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(UrlAPI);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!String.IsNullOrEmpty(KeySecrectAPI))
                {
                    string[] key = KeySecrectAPI.Split(':');
                    client.DefaultRequestHeaders.Add(key[0], key[1]);
                }
                HttpResponseMessage response = new HttpResponseMessage();
                param = Uri.UnescapeDataString(param);
                var dict = HttpUtility.ParseQueryString(param);
                string json = JsonConvert.SerializeObject(dict.Cast<string>().ToDictionary(k => k, v => dict[v]));

                StringContent httpContent = new StringContent(json, UnicodeEncoding.UTF8, "application/json");
                try
                {
                    if (Type.ToUpper().Equals(Common.CommonConstants.MethodeHTTP_POST))
                    {
                        response = client.PostAsync(NameFuncAPI, httpContent).Result;
                    }
                    else if (Type.ToUpper().Equals(Common.CommonConstants.MethodeHTTP_GET))
                    {
                        string requestUri = NameFuncAPI + "?" + param;
                        response = client.GetAsync(requestUri).Result;
                    }
                }
                catch (Exception ex)
                {
                    return String.Empty;
                }
                if (response.IsSuccessStatusCode)
                {
                    result = response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    result = String.Empty;
                }
            }
            return result;
        }


        public void Save()
        {
            throw new NotImplementedException();
        }

        private void LogError(string message)
        {
            try
            {
                Error error = new Error();
                error.CreatedDate = DateTime.Now;
                error.Message = message;
                _errorService.Create(error);
                _unitOfWork.Commit();
            }
            catch
            {
            }
        }

        #endregion



        #region SEND SMS
        private string SendGetRequest(string RequestUrl)
        {
            Uri address = new Uri(RequestUrl);
            HttpWebRequest request;
            HttpWebResponse response = null;
            StreamReader reader;
            if (address == null) { throw new ArgumentNullException("address"); }
            try
            {
                request = WebRequest.Create(address) as HttpWebRequest;
                request.UserAgent = ".NET Sample";
                request.KeepAlive = false;
                request.Timeout = 15 * 1000;
                response = request.GetResponse() as HttpWebResponse;
                if (request.HaveResponse == true && response != null)
                {
                    reader = new StreamReader(response.GetResponseStream());
                    string result = reader.ReadToEnd();
                    result = result.Replace("</string>", "");
                    return result;
                }
            }
            catch (WebException wex)
            {
                if (wex.Response != null)
                {
                    using (HttpWebResponse errorResponse = (HttpWebResponse)wex.Response)
                    {
                        Console.WriteLine(
                            "The server returned '{0}' with the status code {1} ({2:d}).",
                            errorResponse.StatusDescription, errorResponse.StatusCode,
                            errorResponse.StatusCode);
                    }
                }
            }
            finally
            {
                if (response != null) { response.Close(); }
            }
            return null;
        }

        public string SendSms(string strPhoneNumber, string strMsg)
        {
            string URL = "http://rest.esms.vn/MainService.svc/json/SendMultipleMessage_V4_get?Phone=" + strPhoneNumber + "&Content=" + strMsg + "&ApiKey=" + APIKey + "&SecretKey=" + SecretKey + "&IsUnicode=0&Brandname=&SmsType=8";
            //De dang ky brandname rieng vui long lien he hotline 0902435340 hoac nhan vien kinh Doanh cua ban
            //-----------------------------------

            //-----------------------------------
            string result = SendGetRequest(URL);
            JObject ojb = JObject.Parse(result);
            int CodeResult = (int)ojb["CodeResult"];//100 is successfull

            string SMSID = (string)ojb["SMSID"];//id of SMS
            return CodeResult.ToString();
        }

        #endregion

    }
}
