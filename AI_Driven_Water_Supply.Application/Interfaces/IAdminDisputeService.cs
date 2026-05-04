using System;
using System.Threading;
using System.Threading.Tasks;
using AI_Driven_Water_Supply.Application.Common;
using AI_Driven_Water_Supply.Application.DTOs;

namespace AI_Driven_Water_Supply.Application.Interfaces
{
    public interface IAdminDisputeService
    {
        Task<PagedResult<AdminDisputeRowDto>> ListAsync(
            string? statusTab,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<bool> ResolveAsync(Guid disputeId, string resolutionNotes, CancellationToken cancellationToken = default);
    }
}
