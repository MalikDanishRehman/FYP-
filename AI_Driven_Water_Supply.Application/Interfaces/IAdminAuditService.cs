using System.Threading;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Application.Interfaces
{
    public interface IAdminAuditService
    {
        Task LogAsync(string action, string? entityType, string? entityId, string? payloadJson, CancellationToken cancellationToken = default);
    }
}
