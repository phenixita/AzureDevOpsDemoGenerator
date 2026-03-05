using System.Collections.Generic;
using AzureDevOpsDemoGenerator.Modules.Core;
using VstsRestAPI;

namespace AzureDevOpsDemoGenerator.Modules.Extractor
{
    using Project = global::AzureDevOpsDemoGenerator.Modules.Core.Project;

    public interface IExtractorService
    {
        ProjectConfigurations ProjectConfiguration(Project model);
        int GetTeamsCount(ProjectConfigurations appConfig);
        int GetIterationsCount(ProjectConfigurations appConfig);
        Dictionary<string, int> GetWorkItemsCount(ProjectConfigurations appConfig);
        int GetBuildDefinitionCount(ProjectConfigurations appConfig);
        int GetReleaseDefinitionCount(ProjectConfigurations appConfig);
        string[] GenerateTemplateArifacts(Project model);
    }
}
