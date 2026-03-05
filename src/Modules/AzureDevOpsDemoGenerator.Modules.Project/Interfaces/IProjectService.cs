using Newtonsoft.Json.Linq;
using System.Net.Http;
using AzureDevOpsDemoGenerator.Modules.Core;

namespace AzureDevOpsDemoGenerator.Modules.Project
{
    using Project = global::AzureDevOpsDemoGenerator.Modules.Core.Project;

    public interface IProjectService
    {
        void RemoveKey(string id);

        void AddMessage(string id, string message);

        JObject GetStatusMessage(string id);

        HttpResponseMessage GetprojectList(string accname, string pat);

        string GetJsonFilePath(bool IsPrivate, string TemplateFolder, string TemplateName, string FileName = "");

        string[] CreateProjectEnvironment(Project model);

        bool CheckForInstalledExtensions(string extensionJsonFile, string token, string account);

        bool InstallExtensions(Project model, string accountName, string PAT);

        bool WhereDoseTemplateBelongTo(string templatName);

    }
}
