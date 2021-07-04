using AutoMapper;
using BotProject.Common;
using BotProject.Model.Models;
using BotProject.Service;
using BotProject.Web.Infrastructure.Core;
using BotProject.Web.Infrastructure.Extensions;
using BotProject.Web.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace BotProject.Web.API
{
	[RoutePrefix("api/formqna")]
    public class FormQnAController : ApiControllerBase
    {
		private IQnAService _qnaService;
		public FormQnAController(IErrorService errorService,
                                IQnAService qnaService
                                ) : base(errorService)
        {
			_qnaService = qnaService;
        }

		[Route("create")]
		[HttpPost]
		public HttpResponseMessage Create(HttpRequestMessage request, FormQuestionAnswerViewModel formQnAVm)
		{
			return CreateHttpResponse(request, () =>
			{
				HttpResponseMessage response = null;
				FormQuestionAnswer formQnADb = new FormQuestionAnswer();
				formQnADb.UpdateFormQnA(formQnAVm);
				var formQnAReturn = _qnaService.AddFormQnAnswer(ref formQnADb);
				try
				{
					// create file form botQna in bot aiml
					//string pathFolderAIML = ConfigurationManager.AppSettings["AIMLPath"] + "\\" + "User_" + formQnAVm.UserID + "_BotID_" + formQnAVm.BotID;
                    string pathFolderAIML = PathServer.PathAIML + "User_" + formQnAVm.UserID + "_BotID_" + formQnAVm.BotID;
                    string nameFolderAIML = "formQnA_ID_" + formQnADb.ID + "_" + formQnADb.Alias + ".aiml";
					string pathString = System.IO.Path.Combine(pathFolderAIML, nameFolderAIML);

                    if (!System.IO.File.Exists(pathString))
					{
                        try
                        {
                            StreamWriter sw = new StreamWriter(pathString, true, Encoding.UTF8);
                            sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                            sw.WriteLine("<aiml>");
                            sw.WriteLine("<category>");
                            sw.WriteLine("<pattern>*</pattern>");
                            sw.WriteLine("<template>");
                            sw.WriteLine("<random>");
                            sw.WriteLine("<li> NOT_MATCH_01 </li>");
                            sw.WriteLine("<li> NOT_MATCH_02 </li>");
                            sw.WriteLine("<li> NOT_MATCH_03 </li>");
                            sw.WriteLine("<li> NOT_MATCH_04 </li>");
                            sw.WriteLine("<li> NOT_MATCH_05 </li>");
                            sw.WriteLine("<li> NOT_MATCH_06 </li>");
                            sw.WriteLine("</random>");
                            sw.WriteLine("</template>");
                            sw.WriteLine("</category>");
                            sw.WriteLine("</aiml>");
                            sw.Close();
                        }
                        catch { }
						//using (System.IO.FileStream fs = System.IO.File.Create(pathString))
						//{
                            
						//}
					}
				}
				catch (Exception ex)
				{

				}
				var reponseData = Mapper.Map<FormQuestionAnswer, FormQuestionAnswerViewModel>(formQnAReturn);

				response = request.CreateResponse(HttpStatusCode.OK, reponseData);

				return response;
			});
		}


        [Route("update")]
        [HttpPost]
        public HttpResponseMessage Update(HttpRequestMessage request, FormQuestionAnswerViewModel formQnAVm)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;

                var formQnaDb = _qnaService.GetFormQnAnswerById(formQnAVm.ID);
                formQnaDb.Name = formQnAVm.Name;
                _qnaService.UpdateFormQuestionAnswer(formQnaDb);
                _qnaService.Save();
                response = request.CreateResponse(HttpStatusCode.OK);
                return response;
            });
        }

        [Route("delete")]
        [HttpPost]
        public HttpResponseMessage Delete(HttpRequestMessage request, FormQuestionAnswerViewModel formQnAVm)
        {
            return CreateHttpResponse(request, () =>
            {
                HttpResponseMessage response = null;

                var formQnaDb = _qnaService.GetFormQnAnswerById(formQnAVm.ID);
                formQnaDb.IsDelete = true;
                formQnaDb.Status = false;
                _qnaService.UpdateFormQuestionAnswer(formQnaDb);
                _qnaService.Save();
                response = request.CreateResponse(HttpStatusCode.OK, true);
                return response;
            });
        }
    }
}
