using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using AzureDevOpsDemoGenerator.Web.Infrastructure;
using AzureDevOpsDemoGenerator.Modules.Core;
using AzureDevOpsDemoGenerator.Modules.Account;
using AzureDevOpsDemoGenerator.Modules.Project;
using AzureDevOpsDemoGenerator.Modules.Template;
using AzureDevOpsDemoGenerator.Modules.Extractor;
using VstsRestAPI.Viewmodel.ProjectAndTeams;
using VstsRestAPI.WorkItemAndTracking;

namespace AzureDevOpsDemoGenerator.Web.Controllers.Apis
{
    [ApiController]
    [Route("api/environment")]
    public class ProjectController : ControllerBase
    {
        private readonly ITemplateService templateService;
        private readonly IProjectService projectService;
        public delegate string[] ProcessEnvironment(Project model);
        public int usercount = 0;

        public ProjectController(ITemplateService templateService, IProjectService projectService)
        {
            this.templateService = templateService;
            this.projectService = projectService;
        }

        [HttpPost("create")]
        public IActionResult create([FromBody] MultiProjects model)
        {
            ProjectService.TrackFeature("api/environment/create");

            ProjectResponse returnObj = new ProjectResponse();
            returnObj.templatePath = model.templatePath;
            returnObj.templateName = model.templateName;
            string PrivateTemplatePath = string.Empty;
            string extractedTemplate = string.Empty;
            List<RequestedProject> returnProjects = new List<RequestedProject>();
            try
            {
                string readErrorMessages = System.IO.File.ReadAllText(Path.Combine(AppPath.MapPath("~/JSON"), "ErrorMessages.json"));
                var messages = JsonConvert.DeserializeObject<Messages>(readErrorMessages);
                var errormessages = messages.ErrorMessages;
                List<string> listOfExistedProjects = new List<string>();

                if (string.IsNullOrEmpty(model.organizationName))
                {
                    return StatusCode((int)HttpStatusCode.BadRequest, errormessages.AccountMessages.InvalidAccountName);
                }

                if (string.IsNullOrEmpty(model.accessToken))
                {
                    return StatusCode((int)HttpStatusCode.Unauthorized, errormessages.AccountMessages.InvalidAccessToken);
                }

                HttpResponseMessage response = projectService.GetprojectList(model.organizationName, model.accessToken);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return StatusCode((int)HttpStatusCode.BadRequest, errormessages.AccountMessages.CheckaccountDetails);
                }

                var projectResult = response.Content.ReadAsAsync<ProjectsResponse.ProjectResult>().Result;
                foreach (var project in projectResult.value)
                {
                    listOfExistedProjects.Add(project.name);
                }

                if (model.users != null && model.users.Count > 0)
                {
                    List<string> listOfRequestedProjectNames = new List<string>();
                    foreach (var project in model.users)
                    {
                        if (!string.IsNullOrEmpty(project.email) && !string.IsNullOrEmpty(project.projectName))
                        {
                            string pattern = @"^(?!_)(?![.])[a-zA-Z0-9!^\-`)(]*[a-zA-Z0-9_!^\.)( ]*[^.\/\\~@#$*%+=[\]{\}'"",:;?<>|](?:[a-zA-Z!)(][a-zA-Z0-9!^\-` )(]+)?$";
                            bool isProjectNameValid = Regex.IsMatch(project.projectName, pattern);
                            List<string> restrictedNames = new List<string>() { "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "COM10", "PRN", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LTP", "LTP8", "LTP9", "NUL", "CON", "AUX", "SERVER", "SignalR", "DefaultCollection", "Web", "App_code", "App_Browesers", "App_Data", "App_GlobalResources", "App_LocalResources", "App_Themes", "App_WebResources", "bin", "web.config" };

                            if (!isProjectNameValid)
                            {
                                project.status = errormessages.ProjectMessages.InvalidProjectName;
                                return StatusCode((int)HttpStatusCode.BadRequest, project);
                            }

                            if (restrictedNames.ConvertAll(d => d.ToLower()).Contains(project.projectName.Trim().ToLower()))
                            {
                                project.status = errormessages.ProjectMessages.ProjectNameWithReservedKeyword;
                                return StatusCode((int)HttpStatusCode.BadRequest, project);
                            }

                            listOfRequestedProjectNames.Add(project.projectName.ToLower());
                        }
                        else
                        {
                            project.status = errormessages.ProjectMessages.ProjectNameOrEmailID;
                            return StatusCode((int)HttpStatusCode.BadRequest, project);
                        }
                    }

                    bool anyDuplicateProjects = listOfRequestedProjectNames.GroupBy(n => n).Any(c => c.Count() > 1);
                    if (anyDuplicateProjects)
                    {
                        return StatusCode((int)HttpStatusCode.BadRequest, errormessages.ProjectMessages.DuplicateProject);
                    }

                    string templateName = string.Empty;
                    bool isPrivate = false;
                    if (string.IsNullOrEmpty(model.templateName) && string.IsNullOrEmpty(model.templatePath))
                    {
                        return StatusCode((int)HttpStatusCode.BadRequest, errormessages.TemplateMessages.TemplateNameOrTemplatePath);
                    }

                    if (!string.IsNullOrEmpty(model.templatePath))
                    {
                        string fileName = Path.GetFileName(model.templatePath);
                        string extension = Path.GetExtension(model.templatePath);

                        if (extension.ToLower() == ".zip")
                        {
                            extractedTemplate = fileName.ToLower().Replace(".zip", "").Trim() + "-" + Guid.NewGuid().ToString().Substring(0, 6) + extension.ToLower();
                            templateName = extractedTemplate;
                            model.templateName = extractedTemplate.ToLower().Replace(".zip", "").Trim();
                            PrivateTemplatePath = templateService.GetTemplateFromPath(model.templatePath, extractedTemplate, model.gitHubToken, model.userId, model.password);

                            if (string.IsNullOrEmpty(PrivateTemplatePath))
                            {
                                return StatusCode((int)HttpStatusCode.BadRequest, errormessages.TemplateMessages.FailedTemplate);
                            }

                            string privateErrorMessage = templateService.checkSelectedTemplateIsPrivate(PrivateTemplatePath);
                            if (privateErrorMessage != "SUCCESS")
                            {
                                var templatepath = Path.Combine(AppPath.MapPath("~/PrivateTemplates"), model.templateName);
                                if (Directory.Exists(templatepath))
                                {
                                    Directory.Delete(templatepath, true);
                                }
                                return StatusCode((int)HttpStatusCode.BadRequest, privateErrorMessage);
                            }

                            isPrivate = true;
                        }
                        else
                        {
                            return StatusCode((int)HttpStatusCode.BadRequest, errormessages.TemplateMessages.PrivateTemplateFileExtension);
                        }
                    }
                    else
                    {
                        string templateResponse = templateService.GetTemplate(model.templateName);
                        if (templateResponse == "Template Not Found!")
                        {
                            return StatusCode((int)HttpStatusCode.BadRequest, errormessages.TemplateMessages.TemplateNotFound);
                        }
                        templateName = model.templateName;
                    }

                    string extensionJsonFile = projectService.GetJsonFilePath(isPrivate, PrivateTemplatePath, templateName, "Extensions.json");
                    if (System.IO.File.Exists(extensionJsonFile) && projectService.CheckForInstalledExtensions(extensionJsonFile, model.accessToken, model.organizationName))
                    {
                        if (model.installExtensions)
                        {
                            Project pmodel = new Project();
                            pmodel.SelectedTemplate = model.templateName;
                            pmodel.accessToken = model.accessToken;
                            pmodel.accountName = model.organizationName;

                            bool isextensionInstalled = projectService.InstallExtensions(pmodel, model.organizationName, model.accessToken);
                        }
                        else
                        {
                            return StatusCode((int)HttpStatusCode.BadRequest, errormessages.ProjectMessages.ExtensionNotInstalled);
                        }
                    }

                    foreach (var project in model.users)
                    {
                        var exists = listOfExistedProjects.ConvertAll(d => d.ToLower()).Contains(project.projectName.ToLower());
                        if (exists)
                        {
                            project.status = project.projectName + " is already exist";
                        }
                        else
                        {
                            usercount++;
                            project.trackId = Guid.NewGuid().ToString().Split('-')[0];
                            project.status = "Project creation is initiated..";

                            Project pmodel = new Project();
                            pmodel.SelectedTemplate = model.templateName;
                            pmodel.accessToken = model.accessToken;
                            pmodel.accountName = model.organizationName;
                            pmodel.ProjectName = project.projectName;
                            pmodel.Email = project.email;
                            pmodel.id = project.trackId;
                            pmodel.IsApi = true;
                            if (!string.IsNullOrEmpty(model.templatePath))
                            {
                                pmodel.PrivateTemplatePath = PrivateTemplatePath;
                                pmodel.PrivateTemplateName = model.templateName;
                                pmodel.IsPrivatePath = true;
                            }

                            ProcessEnvironment processTask = new ProcessEnvironment(projectService.CreateProjectEnvironment);
                            processTask.BeginInvoke(pmodel, new AsyncCallback(EndEnvironmentSetupProcess), processTask);
                        }
                        returnProjects.Add(project);
                    }

                    if (!string.IsNullOrEmpty(model.templatePath) && usercount == 0 && string.IsNullOrEmpty(extractedTemplate))
                    {
                        templateService.deletePrivateTemplate(extractedTemplate);
                    }
                }
            }
            catch (Exception ex)
            {
                ProjectService.logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t BulkProject \t" + ex.Message + "\t" + "\n" + ex.StackTrace + "\n");
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }

            returnObj.users = returnProjects;
            return StatusCode((int)HttpStatusCode.Accepted, returnObj);
        }

        [HttpGet("GetCurrentProgress")]
        public IActionResult GetCurrentProgress(string trackId)
        {
            ProjectService.TrackFeature("api/environment/GetCurrentProgress");
            var currentProgress = projectService.GetStatusMessage(trackId);
            return Ok(currentProgress["status"]);
        }

        public void EndEnvironmentSetupProcess(IAsyncResult result)
        {
            string templateUsed = string.Empty;
            string id = string.Empty;
            try
            {
                ProcessEnvironment processTask = (ProcessEnvironment)result.AsyncState;
                string[] strResult = processTask.EndInvoke(result);
                if (strResult != null && strResult.Length > 0)
                {
                    id = strResult[0];
                    templateUsed = strResult[2];
                    projectService.RemoveKey(id);

                    if (ProjectService.StatusMessages.Keys.Count(x => x == id + "_Errors") == 1)
                    {
                        string errorMessages = ProjectService.statusMessages[id + "_Errors"];
                        if (!string.IsNullOrEmpty(errorMessages))
                        {
                            string logPath = AppPath.MapPath("~/Log");
                            string fileName = string.Format("{0}_{1}.txt", templateUsed, DateTime.Now.ToString("ddMMMyyyy_HHmmss"));

                            if (!Directory.Exists(logPath))
                            {
                                Directory.CreateDirectory(logPath);
                            }

                            System.IO.File.AppendAllText(Path.Combine(logPath, fileName), errorMessages);

                            string patBase64 = System.Configuration.ConfigurationManager.AppSettings["PATBase64"];
                            string url = System.Configuration.ConfigurationManager.AppSettings["URL"];
                            string projectId = System.Configuration.ConfigurationManager.AppSettings["PROJECTID"];
                            string issueName = string.Format("{0}_{1}", templateUsed, DateTime.Now.ToString("ddMMMyyyy_HHmmss"));
                            IssueWI objIssue = new IssueWI();

                            errorMessages = errorMessages + Environment.NewLine + "TemplateUsed: " + templateUsed;
                            errorMessages = errorMessages + Environment.NewLine + "ProjectCreated : " + ProjectService.projectName;
                            ProjectService.logger.Error(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t  Error: " + errorMessages);

                            string logWIT = System.Configuration.ConfigurationManager.AppSettings["LogWIT"];
                            if (logWIT == "true")
                            {
                                objIssue.CreateIssueWI(patBase64, "4.1", url, issueName, errorMessages, projectId, "Demo Generator");
                            }
                        }
                    }
                }
                usercount--;
            }
            catch (Exception ex)
            {
                ProjectService.logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + ex.Message + "\t" + "\n" + ex.StackTrace + "\n");
            }
            finally
            {
                if (usercount == 0 && !string.IsNullOrEmpty(templateUsed))
                {
                    templateService.deletePrivateTemplate(templateUsed);
                }
            }
        }
    }
}
