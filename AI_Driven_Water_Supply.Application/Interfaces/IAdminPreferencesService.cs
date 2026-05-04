using System.Threading;
using System.Threading.Tasks;
using AI_Driven_Water_Supply.Application.DTOs;
using AI_Driven_Water_Supply.Domain.Entities;

namespace AI_Driven_Water_Supply.Application.Interfaces
{
    public interface IAdminPreferencesService
    {
        Task<AdminPreferences?> GetAsync(CancellationToken cancellationToken = default);

        Task<bool> UpsertNotificationsAsync(AdminNotificationPreferencesDto dto, CancellationToken cancellationToken = default);
    }
}
