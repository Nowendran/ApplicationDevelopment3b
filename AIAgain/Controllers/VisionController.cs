using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AIAgain.Models;

namespace AIAgain.Controllers
{
    public abstract class VisionController : ImageController
    {

        protected VisionServiceClient VisionServiceClient { get; private set; }
        public VisionController()
        {
            var apiKey = ConfigurationManager.AppSettings["CognitiveServicesVisionApiKey"];
            var apiRoot = ConfigurationManager.AppSettings["CognitiveServicesVisionApiUrl"];
            VisionServiceClient = new VisionServiceClient(apiKey, apiRoot);
        }

       

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            ViewBag.LeftMenu = "_ComputerVisionMenu";
        }

        protected override void OnException(ExceptionContext filterContext)
        {
            base.OnException(filterContext);

            if (filterContext.ExceptionHandled)
            {
                return;
            }

            var message = filterContext.Exception.Message;
            var code = "";

            if (filterContext.Exception is ClientException)
            {
                var faex = filterContext.Exception as ClientException;
                message = faex.Error.Message;
                code = faex.Error.Code;
            }

            filterContext.Result = new ViewResult
            {
                ViewName = "Error",
                ViewData = new ViewDataDictionary(filterContext.Controller.ViewData)
                {
                    Model = new Errors { Code = code, Message = message }
                }
            };

            filterContext.ExceptionHandled = true;
        }
    }
}