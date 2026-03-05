using System;
using System.Collections.Generic;
using System.Linq;

namespace AzureDevOpsDemoGenerator.Modules.Core
{
    public class GitHubAccessDetails
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public string scope { get; set; }
    }
    public class GitHubUserDetail
    {
        public string login { get; set; }
    }
}
