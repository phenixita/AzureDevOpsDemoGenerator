using System.Threading.Tasks;
using VstsDemoBuilder.Blazor.Models;

namespace VstsDemoBuilder.Blazor.Services;

public interface IProvisioningService
{
    Task<string> StartProjectProvisioningAsync(ProjectCreateFormModel formModel, string userId, string email, string accessToken);
}
