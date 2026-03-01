using System.Threading;
using System.Threading.Tasks;
using VstsDemoBuilder.Models;

namespace VstsDemoBuilder.ServiceInterfaces
{
    public interface IAccountService
    {
        string GenerateRequestPostData(string appSecret, string authCode, string callbackUrl);
        Task<AccessDetails> GetAccessTokenAsync(string body, CancellationToken cancellationToken = default);

        Task<ProfileDetails> GetProfileAsync(AccessDetails accessDetails, CancellationToken cancellationToken = default);
        Task<AccessDetails> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
        Task<AccountsResponse.AccountList> GetAccountsAsync(string memberID, AccessDetails details, CancellationToken cancellationToken = default);
    }
}
