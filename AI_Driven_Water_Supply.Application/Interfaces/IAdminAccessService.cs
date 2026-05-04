using System.Threading;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Application.Interfaces
{
    public interface IAdminAccessService
    {
        Task<bool> IsCurrentUserAdminAsync(CancellationToken cancellationToken = default);
    }
}
