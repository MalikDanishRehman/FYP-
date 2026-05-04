using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Domain.Entities;
using Supabase;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Infrastructure.Services
{
    public sealed class AdminAccessService : IAdminAccessService
    {
        private readonly Client _supabase;

        public AdminAccessService(Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<bool> IsCurrentUserAdminAsync(CancellationToken cancellationToken = default)
        {
            var user = _supabase.Auth.CurrentUser;
            if (user == null) return false;

            try
            {
                var response = await _supabase.From<Profile>()
                    .Where(x => x.Id == user.Id)
                    .Get();

                var profile = response.Models.FirstOrDefault();
                return profile != null
                    && string.Equals(profile.Role, "admin", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
