using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Domain.Entities;
using Supabase;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Infrastructure.Services
{
    public sealed class AdminSelfProfileService : IAdminSelfProfileService
    {
        private readonly Client _supabase;
        private readonly IAdminAccessService _adminAccess;

        public AdminSelfProfileService(Client supabase, IAdminAccessService adminAccess)
        {
            _supabase = supabase;
            _adminAccess = adminAccess;
        }

        public async Task<Profile?> GetMyProfileAsync(CancellationToken cancellationToken = default)
        {
            if (!await _adminAccess.IsCurrentUserAdminAsync(cancellationToken).ConfigureAwait(false))
                return null;

            var user = _supabase.Auth.CurrentUser;
            if (user == null) return null;

            try
            {
                var response = await _supabase.From<Profile>().Where(x => x.Id == user.Id).Get().ConfigureAwait(false);
                return response.Models.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> UpdateMyDisplayAsync(string username, string? phone, CancellationToken cancellationToken = default)
        {
            if (!await _adminAccess.IsCurrentUserAdminAsync(cancellationToken).ConfigureAwait(false))
                return false;

            var user = _supabase.Auth.CurrentUser;
            if (user == null) return false;

            try
            {
                await _supabase.From<Profile>()
                    .Where(x => x.Id == user.Id)
                    .Set(x => x.Username, username)
                    .Set(x => x.Phone, phone ?? "")
                    .Update()
                    .ConfigureAwait(false);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
