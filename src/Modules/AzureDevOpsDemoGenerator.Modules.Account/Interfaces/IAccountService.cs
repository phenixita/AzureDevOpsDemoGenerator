using System;
using System.Collections.Generic;
using System.Linq;
using AzureDevOpsDemoGenerator.Modules.Core;

namespace AzureDevOpsDemoGenerator.Modules.Account
{
    public interface IAccountService
    {
        string GenerateRequestPostData(string appSecret, string authCode, string callbackUrl);
        AccessDetails GetAccessToken(string body);

        ProfileDetails GetProfile(AccessDetails accessDetails);
        AccessDetails Refresh_AccessToken(string refreshToken);
        AccountsResponse.AccountList GetAccounts(string memberID, AccessDetails details);
    }
}
