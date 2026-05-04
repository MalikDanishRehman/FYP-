using System.Threading;
using System.Threading.Tasks;
using AI_Driven_Water_Supply.Application.Common;
using AI_Driven_Water_Supply.Application.DTOs;
using AI_Driven_Water_Supply.Domain.Entities;

namespace AI_Driven_Water_Supply.Application.Interfaces
{
    public interface IAdminProfileModerationService
    {
        Task<AdminProfileCountsDto?> GetProfileCountsAsync(CancellationToken cancellationToken = default);

        Task<PagedResult<Profile>> ListConsumersAsync(
            int page,
            int pageSize,
            string? searchUsername,
            CancellationToken cancellationToken = default);

        Task<PagedResult<Profile>> ListProvidersAsync(
            int page,
            int pageSize,
            string? searchUsername,
            CancellationToken cancellationToken = default);

        Task<bool> SetAccountStatusAsync(string profileId, string accountStatus, CancellationToken cancellationToken = default);
    }
}
