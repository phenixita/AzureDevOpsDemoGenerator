using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VstsDemoBuilder.Infrastructure;
using VstsDemoBuilder.Models;
using VstsDemoBuilder.ServiceInterfaces;
using VstsDemoBuilder.Services;

namespace VstsDemoBuilder.Controllers
{

    public class AccountController : LegacyController
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
        public ActionResult Verify(LoginModel model, string id)
        {
            Session.Clear();
            // check to enable extractor
            if (string.IsNullOrEmpty(model.EnableExtractor) || model.EnableExtractor.ToLower() == "false")
            {
                model.EnableExtractor = VstsDemoBuilder.Infrastructure.AppSettings.Get("EnableExtractor");
            }
            if (!string.IsNullOrEmpty(model.EnableExtractor))
            {
                Session["EnableExtractor"] = model.EnableExtractor;
            }

            var userAgent = Request.Headers["User-Agent"].ToString();
            if (userAgent.Contains("MSIE", StringComparison.OrdinalIgnoreCase) ||
                userAgent.Contains("Trident", StringComparison.OrdinalIgnoreCase))
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
        /// Index view which calls Entra ID (Azure AD) OAuth
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Index()
        {
            try
            {
                Session["visited"] = "1";
                string authorityUri = VstsDemoBuilder.Infrastructure.AppSettings.Get("AuthorityUri");
                string redirectUrl = VstsDemoBuilder.Infrastructure.AppSettings.Get("RedirectUri");
                string clientId = VstsDemoBuilder.Infrastructure.AppSettings.Get("ClientId");
                string appScope = VstsDemoBuilder.Infrastructure.AppSettings.Get("appScope");

                logger.Info($"OAuth Index - AuthorityUri: {authorityUri}, ClientId: {clientId}, RedirectUri: {redirectUrl}, Scope: {appScope}");

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUrl))
                {
                    var errorMsg = $"OAuth configuration missing - ClientId: {(string.IsNullOrEmpty(clientId) ? "MISSING" : "SET")}, RedirectUri: {(string.IsNullOrEmpty(redirectUrl) ? "MISSING" : "SET")}";
                    logger.Error(errorMsg);
                    throw new InvalidOperationException(errorMsg);
                }

                var uriBuilder = new System.UriBuilder(authorityUri)
                {
                    Query = $"client_id={Uri.EscapeDataString(clientId)}" +
                            $"&response_type=code" +
                            $"&scope={Uri.EscapeDataString(appScope)}" +
                            $"&redirect_uri={Uri.EscapeDataString(redirectUrl)}"
                };

                logger.Info($"OAuth URL: {uriBuilder.ToString()}");
                return Redirect(uriBuilder.ToString());
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error in OAuth redirect: {ex.Message}";
                logger.Error(errorMsg, ex);
                ViewBag.Error = errorMsg;
                return View("Error");
            }
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
            return Redirect("https://app.vssps.visualstudio.com/_signout");
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult SessionOutReturn()
        {
            return View();
        }

        /// <summary>
        /// Get OAuth URL for authentication flows
        /// </summary>
        /// <param name="type">Type of OAuth flow (GetStarted)</param>
        /// <returns>OAuth URL from configuration</returns>
        [HttpGet]
        [AllowAnonymous]
        [Route("api/account/oauth-url")]
        public IActionResult GetOAuthUrl(string type = "GetStarted")
        {
            try
            {
                string authorityUri = VstsDemoBuilder.Infrastructure.AppSettings.Get("AuthorityUri");
                string clientId = VstsDemoBuilder.Infrastructure.AppSettings.Get("AzureEntraClientId");
                string responseType = VstsDemoBuilder.Infrastructure.AppSettings.Get("AzureEntraResponseType");
                string responseMode = VstsDemoBuilder.Infrastructure.AppSettings.Get("AzureEntraResponseMode");
                string redirectUri = VstsDemoBuilder.Infrastructure.AppSettings.Get("AzureVsspsRedirectUri");
                string resourceUri = VstsDemoBuilder.Infrastructure.AppSettings.Get("AzureResourceUri");

                // Generate nonce for security
                string nonce = Guid.NewGuid().ToString();

                var oauthUrl = new System.UriBuilder(authorityUri)
                {
                    Query = $"client_id={Uri.EscapeDataString(clientId)}" +
                            $"&response_type={Uri.EscapeDataString(responseType)}" +
                            $"&response_mode={responseMode}" +
                            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                            $"&resource={Uri.EscapeDataString(resourceUri)}" +
                            $"&nonce={nonce}"
                };

                return Ok(new { url = oauthUrl.ToString() });
            }
            catch (Exception ex)
            {
                logger.Error($"Error generating OAuth URL: {ex.Message}", ex);
                return BadRequest(new { error = "Failed to generate OAuth URL" });
            }
        }

        /// <summary>
        /// Debug endpoint to check OAuth configuration (Development only)
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        [Route("api/account/debug-config")]
        public IActionResult DebugOAuthConfig()
        {
            #if DEBUG
            return Ok(new
            {
                AuthorityUri = VstsDemoBuilder.Infrastructure.AppSettings.Get("AuthorityUri"),
                ClientId = VstsDemoBuilder.Infrastructure.AppSettings.Get("ClientId"),
                RedirectUri = VstsDemoBuilder.Infrastructure.AppSettings.Get("RedirectUri"),
                AppScope = VstsDemoBuilder.Infrastructure.AppSettings.Get("appScope"),
                Status = "OAuth configuration loaded successfully"
            });
            #else
            return BadRequest("Debug endpoint not available in Release mode");
            #endif
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
                        var templatepath = Server.MapPath("~") + @"\PrivateTemplates\" + templateName.ToLower().Replace(".zip", "").Trim();
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

