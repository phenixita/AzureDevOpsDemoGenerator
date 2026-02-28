using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using VstsDemoBuilder.Infrastructure;
using VstsDemoBuilder.Models;
using VstsDemoBuilder.ServiceInterfaces;
using VstsDemoBuilder.Services;

namespace VstsDemoBuilder.Controllers
{

    public class AccountController : CompatController
    {
        private readonly AccessDetails accessDetails = new AccessDetails();
        private TemplateSelection.Templates templates = new TemplateSelection.Templates();
        private ILog logger = LogManager.GetLogger("ErrorLog");
        private IProjectService projectService;
        private ITemplateService templateService;

        public AccountController(IProjectService _projectService, ITemplateService _templateService)
        {
            projectService = _projectService;
            templateService = _templateService;
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Unsupported_browser()
        {
            return View();
        }

        /// <summary>
        /// Verify View
        /// </summary>
        /// <param name="model"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Verify(LoginModel model, string id, string message = "")
        {
            model = model ?? new LoginModel();
            Session.Clear();
            if (!string.IsNullOrWhiteSpace(message))
            {
                ViewBag.resMessage = message;
            }
            // check to enable extractor
            if (string.IsNullOrEmpty(model.EnableExtractor) || model.EnableExtractor.ToLower() == "false")
            {
                model.EnableExtractor = System.Configuration.ConfigurationManager.AppSettings["EnableExtractor"];
            }
            if (!string.IsNullOrEmpty(model.EnableExtractor))
            {
                Session["EnableExtractor"] = model.EnableExtractor;
            }

            var userAgent = Request.Headers.UserAgent.ToString();
            if (!string.IsNullOrWhiteSpace(userAgent) &&
                (userAgent.Contains("MSIE", StringComparison.OrdinalIgnoreCase) ||
                 userAgent.Contains("Trident", StringComparison.OrdinalIgnoreCase)))
            {
                return RedirectToAction("Unsupported_browser", "Account");
            }
            try
            {
                if (!string.IsNullOrEmpty(model.name))
                {
                    if (System.IO.File.Exists(Server.MapPath("~") + @"\Templates\TemplateSetting.json"))
                    {
                        string privateTemplatesJson = System.IO.File.ReadAllText(Server.MapPath("~") + @"\Templates\TemplateSetting.json");
                        templates = Newtonsoft.Json.JsonConvert.DeserializeObject<TemplateSelection.Templates>(privateTemplatesJson);
                        if (templates != null)
                        {
                            bool flag = false;
                            foreach (var grpTemplate in templates.GroupwiseTemplates)
                            {
                                foreach (var template in grpTemplate.Template)
                                {
                                    if (template.ShortName != null && template.ShortName.ToLower() == model.name.ToLower())
                                    {
                                        flag = true;
                                        Session["templateName"] = template.Name;
                                    }
                                }
                            }
                            if (flag == false)
                            {
                                Session["templateName"] = null;
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(model.Event))
                {
                    string eventsTemplate = Server.MapPath("~") + @"\Templates\Events.json";
                    if (System.IO.File.Exists(eventsTemplate))
                    {
                        string eventContent = System.IO.File.ReadAllText(eventsTemplate);
                        var jItems = JObject.Parse(eventContent);
                        if (jItems[model.Event] != null)
                        {
                            model.Event = jItems[model.Event].ToString();
                        }
                        else
                        {
                            model.Event = string.Empty;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(model.TemplateURL))
                {
                    if (model.TemplateURL.EndsWith(".zip"))
                    {
                        PrivateTemplate _privateTemplate = UploadPrivateTempalteFromHome(model.TemplateURL);
                        if (_privateTemplate.IsTemplateValid)
                        {
                            Session["PrivateTemplateURL"] = _privateTemplate.privateTemplatePath;
                            Session["PrivateTemplateName"] = _privateTemplate.privateTemplateName;
                            Session["PrivateTemplateOriginalName"] = _privateTemplate.privateTemplateOriginalName;
                        }
                        else
                        {
                            ViewBag.resMessage = _privateTemplate.responseMessage;
                            return View(new LoginModel());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Debug(JsonConvert.SerializeObject(ex, Formatting.Indented) + Environment.NewLine);
            }
            //return RedirectToAction("../account/verify");
            return View(model);
        }

        /// <summary>
        /// Get Account at the end of project provision
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public string GetAccountName()
        {
            if (Session["AccountName"] != null)
            {
                string accountName = Session["AccountName"].ToString();
                return accountName;
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Index view which calls VSTS OAuth
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Index()
        {
            try
            {
                Session["visited"] = "1";
                string tenantId = System.Configuration.ConfigurationManager.AppSettings["TenantId"] ?? "common";
                string clientId = System.Configuration.ConfigurationManager.AppSettings["ClientId"];
                string redirectUrl = System.Configuration.ConfigurationManager.AppSettings["RedirectUri"];
                string appScope = System.Configuration.ConfigurationManager.AppSettings["appScope"];

                // Microsoft Entra ID OAuth 2.0 authorization code flow
                string url = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize" +
                    $"?client_id={Uri.EscapeDataString(clientId)}" +
                    "&response_type=code" +
                    $"&redirect_uri={Uri.EscapeDataString(redirectUrl)}" +
                    $"&scope={Uri.EscapeDataString(appScope)}" +
                    "&state=User1";
                return Redirect(url);
            }
            catch (Exception ex)
            {
                logger.Debug(JsonConvert.SerializeObject(ex, Formatting.Indented) + Environment.NewLine);
            }
            return RedirectToAction("../shared/error");
        }

        /// <summary>
        /// Sign out
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public new ActionResult SignOut()
        {
            Session.Clear();
            string tenantId = System.Configuration.ConfigurationManager.AppSettings["TenantId"] ?? "common";
            return Redirect($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/logout");
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult SessionOutReturn()
        {
            return View();
        }

        public PrivateTemplate UploadPrivateTempalteFromHome(string TemplateURL)
        {
            PrivateTemplate privateTemplate = new PrivateTemplate();
            string templatePath = string.Empty;
            try
            {
                privateTemplate.IsTemplateValid = false;
                string templateName = "";
                string fileName = Path.GetFileName(TemplateURL);
                string extension = Path.GetExtension(TemplateURL);
                privateTemplate.privateTemplateOriginalName = fileName.ToLower().Replace(".zip", "").Trim();
                templateName = fileName.ToLower().Replace(".zip", "").Trim() + "-" + Guid.NewGuid().ToString().Substring(0, 6) + extension.ToLower();
                privateTemplate.privateTemplateName = templateName.ToLower().Replace(".zip", "").Trim();
                privateTemplate.privateTemplatePath = templateService.GetTemplateFromPath(TemplateURL, templateName, "", "", "");

                if (privateTemplate.privateTemplatePath != "")
                {
                    privateTemplate.responseMessage = templateService.checkSelectedTemplateIsPrivate(privateTemplate.privateTemplatePath);
                    if (privateTemplate.responseMessage != "SUCCESS")
                    {
                        var templatepath = AppPath.MapPath("~") + @"\PrivateTemplates\" + templateName.ToLower().Replace(".zip", "").Trim();
                        if (Directory.Exists(templatepath))
                            Directory.Delete(templatepath, true);
                    }
                    if (privateTemplate.responseMessage == "SUCCESS")
                    {
                        privateTemplate.IsTemplateValid = true;
                    }
                }
                else
                {
                    privateTemplate.responseMessage = "Unable to download file, please check the provided URL";
                    privateTemplate.IsTemplateValid = false;
                }

            }
            catch (Exception ex)
            {
                ProjectService.logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + "\t" + ex.Message + "\t" + "\n" + ex.StackTrace + "\n");
            }
            return privateTemplate;
        }
    }
}
