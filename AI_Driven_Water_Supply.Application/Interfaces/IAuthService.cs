using Supabase.Gotrue;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Application.Interfaces
{
    public interface IAuthService
    {
        Task<bool> SignIn(string email, string password);
        Task<bool> SignUp(string email, string password, string username);
        Task SignOut();
        Task<bool> UpdateUserRole(string role);
        User? CurrentUser { get; }

        // 👇 YEH LINE ADD KARO (Notification System)
        event Action OnChange;

        Task TryRefreshSession();

        Task<bool> UpdatePasswordAsync(string newPassword, CancellationToken cancellationToken = default);
    }
}