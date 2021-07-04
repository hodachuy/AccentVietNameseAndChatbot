using BotProject.Web.Infrastructure.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using BotProject.Service;
using BotProject.Service.Livechat;
using System.Web.Script.Serialization;
using BotProject.Web.Models.Livechat;
using BotProject.Model.Models.LiveChat;
using BotProject.Web.Infrastructure.Extensions;
using BotProject.Common;
using AutoMapper;
using BotProject.Model.Models;

namespace BotProject.Web.API_Livechat
{
    [RoutePrefix("api/lc_customer")]
    public class CustomerController : ApiControllerBase
    {
        private ICustomerService _customerService;
        private IAttributeSystemService _attributeService;

        public CustomerController(IErrorService errorService, ICustomerService customerService, IAttributeSystemService attributeService) : base(errorService)
        {
            _customerService = customerService;
            _attributeService = attributeService;
        }

        [Route("create")]
        [HttpPost]
        public HttpResponseMessage Create(HttpRequestMessage request)
        {
            return CreateHttpResponse(request, () => {
                HttpResponseMessage response = null;
                try
                {
                    var customer = System.Web.HttpContext.Current.Request.Unvalidated.Form["customer"];
                    if (customer == null)
                    {
                        response = request.CreateResponse(HttpStatusCode.NoContent, false);
                        return response;
                    }
                    var customerVm = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue, RecursionLimit = 100 }.Deserialize<CustomerViewModel>(customer);
                    var device = System.Web.HttpContext.Current.Request.Unvalidated.Form["device"];
                    if (device == null)
                    {
                        response = request.CreateResponse(HttpStatusCode.NoContent, false);
                        return response;
                    }
                    var deviceVm = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue, RecursionLimit = 100 }.Deserialize<DeviceViewModel>(device);

                    var customerDb = new Customer();
                    customerDb.UpdateCustomer(customerVm);
                    customerDb.StatusChatValue = CommonConstants.USER_ONLINE;
                    _customerService.Create(customerDb);

                    if(customerVm.BotID != null)
                    {
                        // Set Name
                        AttributePlatformUser attPlatformUser = new AttributePlatformUser();
                        attPlatformUser.UserID = customerVm.ID;
                        attPlatformUser.BotID = customerVm.BotID ?? default(int);
                        attPlatformUser.AttributeKey = "sender_name";
                        attPlatformUser.AttributeValue = customerVm.Name;
                        attPlatformUser.TypeDevice = "Web";
                        _attributeService.CreateUpdateAttributePlatform(attPlatformUser);
                        _attributeService.Save();
                    }


                    if (deviceVm != null)
                    {
                        Device deviceDb = new Device();
                        deviceDb.UpdateDevice(deviceVm);
                        deviceDb.CustomerID = customerVm.ID;
                        _customerService.CreateDevice(deviceDb);

                        if (String.IsNullOrEmpty(customerVm.Name))
                        {
                            customerDb.Name = deviceVm.IPAddress;
                        }
                    }

                    _customerService.Save();

                    response = request.CreateResponse(HttpStatusCode.OK, true);
                }
                catch (Exception ex)
                {
                    return request.CreateResponse(HttpStatusCode.OK, false);
                }

                return response;
            });
        }

        [Route("getAll")]
        [HttpGet]
        public HttpResponseMessage GetListCustomer(HttpRequestMessage request, int channelGroupId)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                var lstCustomer = _customerService.GetListCustomerByChannelGroupId(channelGroupId);           
                response = request.CreateResponse(HttpStatusCode.OK, lstCustomer);
                return response;
            });
        }

        [Route("getById")]
        [HttpGet]
        public HttpResponseMessage GetCustomerByID(HttpRequestMessage request, string customerId)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                var customerDb = _customerService.GetById(customerId);
                var customerVm = Mapper.Map<Customer, CustomerViewModel>(customerDb);
                var deviceDb = _customerService.GetDeviceByCustomerId(customerId).ToList();
                customerVm.Devices = Mapper.Map<IEnumerable<Device>,IEnumerable<DeviceViewModel>>(deviceDb);
                response = request.CreateResponse(HttpStatusCode.OK, customerVm);
                return response;
            });
        }

        [Route("getDeviceByCustomerId")]
        [HttpGet]
        public HttpResponseMessage GetDeviceByCustomerID(HttpRequestMessage request, string customerId)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;
                var deviceDb = _customerService.GetDeviceByCustomerId(customerId);
                response = request.CreateResponse(HttpStatusCode.OK, deviceDb);
                return response;
            });
        }
    }
}
