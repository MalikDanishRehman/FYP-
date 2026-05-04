using AI_Driven_Water_Supply.Application.DTOs;
using AI_Driven_Water_Supply.Application.Interfaces;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class Admin_Setting
    {
        [Inject] private IAdminSelfProfileService SelfProfile { get; set; } = default!;
        [Inject] private IAdminPreferencesService Preferences { get; set; } = default!;
        [Inject] private IAuthService AuthService { get; set; } = default!;
        [Inject] private IToastService Toast { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;

        private string adminName = "";
        private string adminEmail = "";
        private string adminPhone = "";
        private string newPassword = "";
        private string confirmPassword = "";

        private bool emailAlerts = true;
        private bool desktopNotifications = true;
        private bool monthlyReports;
        private bool soundEffects = true;

        private bool isLoading = true;

        private string AvatarLetter()
        {
            var n = (adminName ?? "").Trim();
            return string.IsNullOrEmpty(n) ? "A" : n.Substring(0, 1).ToUpperInvariant();
        }

        protected override async Task OnInitializedAsync()
        {
            var user = AuthService.CurrentUser;
            if (user == null)
            {
                await AuthService.TryRefreshSession();
                user = AuthService.CurrentUser;
            }

            if (user == null)
            {
                Nav.NavigateTo("/login");
                return;
            }

            adminEmail = user.Email ?? "";

            var profile = await SelfProfile.GetMyProfileAsync();
            if (profile != null)
            {
                adminName = profile.Username ?? "";
                adminPhone = profile.Phone ?? "";
            }

            var row = await Preferences.GetAsync();
            if (row != null && !string.IsNullOrWhiteSpace(row.Preferences))
            {
                try
                {
                    var dto = JsonSerializer.Deserialize<AdminNotificationPreferencesDto>(
                        row.Preferences,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (dto != null)
                    {
                        emailAlerts = dto.EmailAlerts;
                        desktopNotifications = dto.DesktopNotifications;
                        monthlyReports = dto.MonthlyReports;
                        soundEffects = dto.SoundEffects;
                    }
                }
                catch { /* ignore */ }
            }

            isLoading = false;
        }

        private async Task SaveProfileAsync()
        {
            var ok = await SelfProfile.UpdateMyDisplayAsync(adminName, adminPhone, default);
            Toast.ShowToast(
                "Profile",
                ok ? "Profile updated." : "Could not update profile.",
                ok ? "success" : "error");
        }

        private async Task SavePreferencesAsync()
        {
            var dto = new AdminNotificationPreferencesDto
            {
                EmailAlerts = emailAlerts,
                DesktopNotifications = desktopNotifications,
                MonthlyReports = monthlyReports,
                SoundEffects = soundEffects
            };

            var ok = await Preferences.UpsertNotificationsAsync(dto, default);
            Toast.ShowToast(
                "Preferences",
                ok ? "Notification preferences saved." : "Could not save preferences.",
                ok ? "success" : "error");
        }

        private async Task ChangePasswordAsync()
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                Toast.ShowToast("Security", "New password must be at least 6 characters.", "error");
                return;
            }

            if (newPassword != confirmPassword)
            {
                Toast.ShowToast("Security", "New password and confirmation do not match.", "error");
                return;
            }

            var ok = await AuthService.UpdatePasswordAsync(newPassword, default);
            if (ok)
            {
                newPassword = "";
                confirmPassword = "";
                Toast.ShowToast("Security", "Password updated.", "success");
            }
            else
            {
                Toast.ShowToast("Security", "Password update failed.", "error");
            }
        }
    }
}
