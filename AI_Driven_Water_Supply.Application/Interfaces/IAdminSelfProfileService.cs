using System.Threading;
using System.Threading.Tasks;
using AI_Driven_Water_Supply.Domain.Entities;

namespace AI_Driven_Water_Supply.Application.Interfaces
{
    public interface IAdminSelfProfileService
    {
        Task<Profile?> GetMyProfileAsync(CancellationToken cancellationToken = default);

        Task<bool> UpdateMyDisplayAsync(string username, string? phone, CancellationToken cancellationToken = default);
    }
}
