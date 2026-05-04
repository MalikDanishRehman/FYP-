namespace AI_Driven_Water_Supply.Application.DTOs
{
    public sealed class AdminNotificationPreferencesDto
    {
        public bool EmailAlerts { get; set; } = true;
        public bool DesktopNotifications { get; set; } = true;
        public bool MonthlyReports { get; set; }
        public bool SoundEffects { get; set; } = true;
    }
}
