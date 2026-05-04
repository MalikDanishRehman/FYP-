using System.Threading;
using System.Threading.Tasks;
using AI_Driven_Water_Supply.Application.Common;
using AI_Driven_Water_Supply.Application.DTOs;

namespace AI_Driven_Water_Supply.Application.Interfaces
{
    public interface IAdminDashboardService
    {
        Task<AdminDashboardMetricsDto?> GetMetricsAsync(CancellationToken cancellationToken = default);

        Task<PagedResult<AdminOrderRowDto>> GetRecentOrdersAsync(
            int page,
            int pageSize,
            string? statusFilter,
            string? searchOrderId,
            CancellationToken cancellationToken = default);
    }
}
