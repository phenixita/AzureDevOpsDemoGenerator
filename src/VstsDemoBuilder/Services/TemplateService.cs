using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using VstsDemoBuilder.Extensions;
using VstsDemoBuilder.Infrastructure;
using VstsDemoBuilder.Models;
using VstsDemoBuilder.ServiceInterfaces;
using VstsRestAPI.Viewmodel.Extractor;
using static VstsDemoBuilder.Models.TemplateSelection;

namespace VstsDemoBuilder.Services
{
    public class TemplateService : ITemplateService
    {

        public List<TemplateDetails> GetAllTemplates()
        {
            var templates = new TemplateSelection.Templates();
            var TemplateDetails = new List<TemplateDetails>();
            try
            {
                Project model = new Project();
                string[] dirTemplates = Directory.GetDirectories(AppPath.MapPath("~/Templates"));
                List<string> TemplateNames = new List<string>();
                //Taking all the template folder and adding to list
                foreach (string template in dirTemplates)
                {
                    TemplateNames.Add(Path.GetFileName(template));
                    // Reading Template setting file to check for private templates                   
                }

                if (System.IO.File.Exists(AppPath.MapPath("~/Templates/TemplateSetting.json")))
                {
                    string templateSetting = model.ReadJsonFile(AppPath.MapPath("~/Templates/TemplateSetting.json"));
                    templates = JsonConvert.DeserializeObject<TemplateSelection.Templates>(templateSetting);

                    foreach (var templateList in templates.GroupwiseTemplates)
                    {
                        foreach (var template in templateList.Template)
                        {
                            TemplateDetails tmp = new TemplateDetails();

                            tmp.Name = template.Name;
                            tmp.ShortName = template.ShortName;
                            tmp.Tags = template.Tags;
                            tmp.Description = template.Description;
                            //tmp.TemplateFolder = template.TemplateFolder;
                            TemplateDetails.Add(tmp);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                ProjectService.logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t BulkProject \t" + ex.Message + "\t" + "\n" + ex.StackTrace + "\n");
            }
            return TemplateDetails;
        }

        public List<TemplateDetails> GetTemplatesByTags(string Tags)
        {
            var templates = new TemplateSelection.Templates();
            var Selectedtemplates = new List<TemplateDetails>();
            char delimiter = ',';
            string[] strComponents = Tags.Split(delimiter);
            try
            {
                Project model = new Project();
                string[] dirTemplates = Directory.GetDirectories(AppPath.MapPath("~/Templates"));
                List<string> TemplateNames = new List<string>();
                //Taking all the template folder and adding to list
                foreach (string template in dirTemplates)
                {
                    TemplateNames.Add(Path.GetFileName(template));
                    // Reading Template setting file to check for private templates                   
                }

                if (System.IO.File.Exists(AppPath.MapPath("~/Templates/TemplateSetting.json")))
                {
                    string templateSetting = model.ReadJsonFile(AppPath.MapPath("~/Templates/TemplateSetting.json"));
                    templates = JsonConvert.DeserializeObject<TemplateSelection.Templates>(templateSetting);

                    foreach (var groupwiseTemplates in templates.GroupwiseTemplates)
                    {
                        foreach (var tmp in groupwiseTemplates.Template)
                        {
                            if (tmp.Tags != null)
                            {
                                foreach (string str in strComponents)
                                {
                                    if (tmp.Tags.Contains(str))
                                    {
                                        TemplateDetails template = new TemplateDetails();

                                        template.Name = tmp.Name;
                                        template.ShortName = tmp.ShortName;
                                        template.Tags = tmp.Tags;
                                        template.Description = tmp.Description;
                                        //template.TemplateFolder = tmp.TemplateFolder;
                                        Selectedtemplates.Add(template);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ProjectService.logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t BulkProject \t" + ex.Message + "\t" + "\n" + ex.StackTrace + "\n");
            }
            return Selectedtemplates;
        }

        public string GetTemplate(string TemplateName)
        {
            string template = string.Empty;
            try
            {
                string templatesPath = AppPath.MapPath("~/Templates");

                string projectTemplatePath = Path.Combine(templatesPath, Path.GetFileName(TemplateName), "ProjectTemplate.json");
                if (System.IO.File.Exists(projectTemplatePath))
                {
                    Project objP = new Project();
                    template = objP.ReadJsonFile(projectTemplatePath);
                }
                else
                {
                    template = "Template Not Found!";
                }
            }
            catch (Exception ex)
            {
                ProjectService.logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + "\t" + ex.Message + "\t" + "\n" + ex.StackTrace + "\n");
            }
            return template;
        }

        /// <summary>
        /// Get extracted template path from the given templatepath(url) in request body
        /// </summary>
        /// <param name="TemplateUrl"></param>
        /// <param name="ExtractedTemplate"></param>
        public string GetTemplateFromPath(string TemplateUrl, string ExtractedTemplate, string GithubToken, string UserID = "", string Password = "")
        {
            string templatePath = string.Empty;
            try
            {
                Uri uri = new Uri(TemplateUrl);
                string fileName = Path.GetFileName(TemplateUrl);
                string extension = Path.GetExtension(fileName);
                string templateName = ExtractedTemplate.ToLower().Replace(".zip", "").Trim();
                string extractedZipDir = AppPath.MapPath("~/ExtractedZipFile");
                if (!Directory.Exists(extractedZipDir))
                {
                    Directory.CreateDirectory(extractedZipDir);
                }
                var path = Path.Combine(extractedZipDir, ExtractedTemplate);
                if (uri.Host == "github.com")
                {
                    string gUri = uri.ToString();
                    gUri = gUri.Replace("github.com", "raw.githubusercontent.com").Replace("/blob/", "/");
                    uri = new Uri(gUri);
                    TemplateUrl = uri.ToString();
                }
                //Downloading template from source of type github
                if (uri.Host == "raw.githubusercontent.com")
                {
                    var githubToken = GithubToken;
                    //var url = TemplateUrl.Replace("github.com/", "raw.githubusercontent.com/").Replace("/blob/master/", "/master/");

                    using (var client = new System.Net.Http.HttpClient())
                    {
                        var credentials = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:", githubToken);
                        credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(credentials));
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
                        var contents = client.GetByteArrayAsync(TemplateUrl).Result;
                        System.IO.File.WriteAllBytes(path, contents);
                    }
                }
                //Downloading file from other source type (ftp or https)
                else
                {
                    using (var httpClient = new System.Net.Http.HttpClient())
                    {
                        if (UserID != null && Password != null)
                        {
                            var byteArray = System.Text.Encoding.ASCII.GetBytes($"{UserID}:{Password}");
                            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                        }
                        var data = httpClient.GetByteArrayAsync(TemplateUrl).Result;
                        System.IO.File.WriteAllBytes(path, data);
                    }
                }
                templatePath = ExtractZipFile(path, templateName);

            }
            catch (Exception ex)
            {
                ProjectService.logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + ex.Message + "\t" + "\n" + ex.StackTrace + "\n");
            }
            finally
            {
                var zippath = Path.Combine(AppPath.MapPath("~/ExtractedZipFile"), ExtractedTemplate);
                if (File.Exists(zippath))
                    File.Delete(zippath);
            }
            return templatePath;
        }

        public string ExtractZipFile(string path, string templateName)
        {
            string templatePath = string.Empty;
            bool isExtracted = false;
            try
            {
                if (File.Exists(path))
                {
                    string privateTemplatesDir = AppPath.MapPath("~/PrivateTemplates");
                    if (!Directory.Exists(privateTemplatesDir))
                    {
                        Directory.CreateDirectory(privateTemplatesDir);
                    }
                    var Extractedpath = Path.Combine(privateTemplatesDir, templateName);
                    System.IO.Compression.ZipFile.ExtractToDirectory(path, Extractedpath);

                    isExtracted = checkTemplateDirectory(Extractedpath);
                    if (isExtracted)
                        templatePath = FindPrivateTemplatePath(Extractedpath);
                    else
                        Directory.Delete(Extractedpath, true);
                }
            }
            catch (Exception ex)
            {
                ProjectService.logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + ex.Message + "\t" + "\n" + ex.StackTrace + "\n");
            }
            return templatePath;

        }

        /// <summary>
        /// Check the valid files from extracted files from zip file in PrivateTemplate Folder 
        /// </summary>
        /// <param name="dir"></param>
        public bool checkTemplateDirectory(string dir)
        {
            try
            {
                string[] filepaths = Directory.GetFiles(dir);
                foreach (var file in filepaths)
                {
                    if (Path.GetExtension(Path.GetFileName(file)) != ".json")
                    {
                        return false;
                    }
                }
                string[] subdirectoryEntries = Directory.GetDirectories(dir);
                foreach (string subdirectory in subdirectoryEntries)
                {
                    checkTemplateDirectory(subdirectory);
                }

            }
            catch (Exception ex)
            {
                ProjectService.logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + "\t" + ex.Message + "\t" + "\n" + ex.StackTrace + "\n");
            }
            return true;
        }

        /// <summary>
        /// Get the private template path from the private template folder 
        /// </summary>
        /// <param name="privateTemplatePath"></param>
        public string FindPrivateTemplatePath(string privateTemplatePath)
        {
            string templatePath = "";
            try
            {
                DirectoryInfo di = new DirectoryInfo(privateTemplatePath);
                FileInfo[] TXTFiles = di.GetFiles("*.json");
                if (TXTFiles.Length > 0)
                {
                    templatePath = privateTemplatePath;
                }
                else
                {
                    string[] subdirs = Directory.GetDirectories(privateTemplatePath);
                    templatePath = FindPrivateTemplatePath(subdirs[0] + Path.DirectorySeparatorChar);
                }

            }
            catch (Exception ex)
            {
                ProjectService.logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + "\t" + ex.Message + "\t" + "\n" + ex.StackTrace + "\n");
            }
            return templatePath;
        }

        public string checkSelectedTemplateIsPrivate(string extractPath)
        {
            string response = string.Empty;
            try
            {
                bool isExtracted = checkTemplateDirectory(extractPath);
                if (!isExtracted)
                {
                    response = "File or the folder contains unwanted entries, so discarding the files, please try again";
                }
                else
                {
                    bool settingFile = System.IO.File.Exists(Path.Combine(extractPath, "ProjectSettings.json"));
                    bool projectFile = System.IO.File.Exists(Path.Combine(extractPath, "ProjectTemplate.json"));

                    if (settingFile && projectFile)
                    {
                        string projectFileData = System.IO.File.ReadAllText(Path.Combine(extractPath, "ProjectTemplate.json"));
                        ProjectSetting settings = JsonConvert.DeserializeObject<ProjectSetting>(projectFileData);
                        response = "SUCCESS";
                    }
                    else if (!settingFile && !projectFile)
                    {
                        string[] folderName = System.IO.Directory.GetDirectories(extractPath);
                        string subDir = "";
                        if (folderName.Length > 0)
                        {
                            subDir = folderName[0];
                        }
                        else
                        {
                            response = "Could not find required preoject setting and project template file.";
                        }
                        if (subDir != "")
                        {
                            response = checkSelectedTemplateIsPrivate(subDir);
                        }
                        if (response != "SUCCESS")
                        {
                            Directory.Delete(extractPath, true);
                            response = "Project setting and project template files not found! plase include the files in zip and try again";
                        }
                    }
                    else
                    {
                        if (!settingFile)
                        {
                            Directory.Delete(extractPath, true);
                            response = "Project setting file not found! plase include the files in zip and try again";
                            //return Json("SETTINGNOTFOUND");
                        }
                        if (!projectFile)
                        {
                            Directory.Delete(extractPath, true);
                            response = "Project template file not found! plase include the files in zip and try again";
                            //return Json("PROJECTFILENOTFOUND");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ProjectService.logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + "\t" + ex.Message + "\t" + "\n" + ex.StackTrace + "\n");
            }
            return response;
        }

        public void deletePrivateTemplate(string Template)
        {
            try
            {
                if (!string.IsNullOrEmpty(Template))
                {
                    string privateTemplatesDir = AppPath.MapPath("~/PrivateTemplates");
                    var templatepath = Path.Combine(privateTemplatesDir, Template);
                    if (Directory.Exists(templatepath))
                    {
                        Directory.Delete(templatepath, true);
                    }
                    string[] subdirs = Directory.GetDirectories(privateTemplatesDir)
                            .Select(Path.GetFileName)
                            .ToArray();
                    foreach (string folderName in subdirs)
                    {
                        string folderPath = Path.Combine(privateTemplatesDir, folderName);
                        DirectoryInfo d = new DirectoryInfo(folderPath);
                        if (d.CreationTime < DateTime.Now.AddHours(-1))
                            Directory.Delete(folderPath, true);
                    }
                }
            }
            catch (Exception ex)
            {
                ProjectService.logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + "\t" + ex.Message + "\t" + "\n" + ex.StackTrace + "\n");
            }
        }
    }
}

