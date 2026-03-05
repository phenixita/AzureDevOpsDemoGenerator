using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureDevOpsDemoGenerator.Modules.Core;
using static AzureDevOpsDemoGenerator.Modules.Core.TemplateSelection;

namespace AzureDevOpsDemoGenerator.Modules.Template
{
    public interface ITemplateService
    {
        List<TemplateDetails> GetAllTemplates();

        List<TemplateDetails> GetTemplatesByTags(string Tags);

        string GetTemplate(string TemplateName);

        string GetTemplateFromPath(string TemplateUrl, string ExtractedTemplate, string GithubToken, string UserID = "", string Password = "");

        bool checkTemplateDirectory(string dir);

        string FindPrivateTemplatePath(string privateTemplatePath);

        string checkSelectedTemplateIsPrivate(string templatePath);

        void deletePrivateTemplate(string Template);
    }
}
