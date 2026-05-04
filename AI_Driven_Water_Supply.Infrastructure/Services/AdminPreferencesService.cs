using AI_Driven_Water_Supply.Application.DTOs;
using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Domain.Entities;
using Supabase;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Infrastructure.Services
{
    public sealed class AdminPreferencesService : IAdminPreferencesService
    {
        private readonly Client _supabase;
        private readonly IAdminAccessService _adminAccess;

        public AdminPreferencesService(Client supabase, IAdminAccessService adminAccess)
        {
            _supabase = supabase;
            _adminAccess = adminAccess;
        }

        public async Task<AdminPreferences?> GetAsync(CancellationToken cancellationToken = default)
        {
            if (!await _adminAccess.IsCurrentUserAdminAsync(cancellationToken).ConfigureAwait(false))
                return null;

            var user = _supabase.Auth.CurrentUser;
            if (user == null) return null;

            try
            {
                var response = await _supabase.From<AdminPreferences>()
                    .Where(x => x.AdminId == user.Id)
                    .Get()
                    .ConfigureAwait(false);

                return response.Models.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> UpsertNotificationsAsync(AdminNotificationPreferencesDto dto, CancellationToken cancellationToken = default)
        {
            if (!await _adminAccess.IsCurrentUserAdminAsync(cancellationToken).ConfigureAwait(false))
                return false;

            var user = _supabase.Auth.CurrentUser;
            if (user == null) return false;

            try
            {
                var json = JsonSerializer.Serialize(new
                {
                    emailAlerts = dto.EmailAlerts,
                    desktopNotifications = dto.DesktopNotifications,
                    monthlyReports = dto.MonthlyReports,
                    soundEffects = dto.SoundEffects
                });

                var row = new AdminPreferences
                {
                    AdminId = user.Id,
                    Preferences = json,
                    UpdatedAt = DateTime.UtcNow
                };

                await _supabase.From<AdminPreferences>().Upsert(row).ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
