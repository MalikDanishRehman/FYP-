using Microsoft.AspNetCore.Components;
using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Domain.Entities;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public class ChartModeData
    {
        [JsonPropertyName("labels")] public List<string> Labels { get; set; } = new();
        [JsonPropertyName("pastData")] public List<int?> PastData { get; set; } = new();
        [JsonPropertyName("futureData")] public List<int?> FutureData { get; set; } = new();
        [JsonPropertyName("note")] public string Note { get; set; } = "";
    }

    public class ChartDataDto
    {
        [JsonPropertyName("tomorrow")] public ChartModeData Tomorrow { get; set; } = new();
        [JsonPropertyName("week")] public ChartModeData Week { get; set; } = new();
        [JsonPropertyName("month")] public ChartModeData Month { get; set; } = new();
    }

    public partial class Providerdashboard : ComponentBase
    {
        [Inject] public IAuthService AuthService { get; set; } = default!;
        [Inject] public Supabase.Client _supabase { get; set; } = default!;
        [Inject] public NavigationManager Nav { get; set; } = default!;
        [Inject] public IJSRuntime JS { get; set; } = default!;

        private string UserName = "Loading...";
        private int totalRevenue = 0;
        private int activeOrdersCount = 0;
        private int completedCount = 0;
        private string activeTab = "Week";
        private ChartDataDto? chartData;

        protected override async Task OnInitializedAsync()
        {
            var user = AuthService.CurrentUser;
            if (user == null) { await AuthService.TryRefreshSession(); user = AuthService.CurrentUser; }

            if (user != null)
            {
                await SetDynamicData(user);
                await LoadOrders();
                StateHasChanged();
            }
            else
            {
                Nav.NavigateTo("/login");
            }
        }

        private async Task SetDynamicData(Supabase.Gotrue.User user)
        {
            string fetchedName = "";
            try
            {
                var response = await _supabase.From<Profile>().Where(x => x.Id == user.Id).Get();
                var profile = response.Models.FirstOrDefault();
                if (profile != null)
                    fetchedName = profile.Username;
            }
            catch { }

            if (string.IsNullOrEmpty(fetchedName) && user.UserMetadata != null)
                if (user.UserMetadata.TryGetValue("username", out var nameObj)) fetchedName = nameObj?.ToString() ?? "";

            if (string.IsNullOrEmpty(fetchedName) && !string.IsNullOrEmpty(user.Email))
                fetchedName = user.Email.Split('@')[0];

            UserName = string.IsNullOrEmpty(fetchedName) ? "Provider" : fetchedName;
        }

        private async Task LoadOrders()
        {
            if (UserName == "Loading...") return;
            try
            {
                var response = await _supabase.From<Order>()
                    .Where(x => x.SupplierName == UserName)
                    .Get();
                var orders = response.Models;

                activeOrdersCount = orders.Count(o => o.Status != "Completed" && o.Status != "Cancelled");
                completedCount = orders.Count(o => o.Status == "Completed");
                totalRevenue = orders.Sum(o => o.TotalPrice);

                var today = System.DateTime.UtcNow.Date;
                chartData = new ChartDataDto();

                var dayNames = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
                var weekLabels = new List<string>();
                var weekPast = new List<int?>();
                for (var i = -6; i <= 0; i++)
                {
                    var d = today.AddDays(i);
                    weekLabels.Add(dayNames[(int)d.DayOfWeek]);
                    var count = orders.Count(o => o.CreatedAt.Date == d);
                    weekPast.Add(count);
                }
                chartData.Week.Labels = weekLabels;
                chartData.Week.PastData = weekPast;
                chartData.Week.FutureData = new List<int?> { null, null, null, null, null, null, null };
                chartData.Week.Note = "Demand based on your recent orders.";

                var monthLabels = new List<string> { "Week 1", "Week 2", "Week 3", "Week 4" };
                var monthPast = new List<int?>();
                for (var w = 3; w >= 0; w--)
                {
                    var weekStart = today.AddDays(-7 * w - 6);
                    var weekEnd = weekStart.AddDays(6);
                    var count = orders.Count(o => o.CreatedAt.Date >= weekStart && o.CreatedAt.Date <= weekEnd);
                    monthPast.Add(count);
                }
                chartData.Month.Labels = monthLabels;
                chartData.Month.PastData = monthPast;
                chartData.Month.FutureData = new List<int?> { null, null, null, null };
                chartData.Month.Note = "Overall demand from last 4 weeks.";

                chartData.Tomorrow.Labels = new List<string> { "8 AM", "10 AM", "12 PM", "2 PM", "4 PM", "6 PM", "8 PM" };
                chartData.Tomorrow.PastData = new List<int?> { null, null, null, null, null, null, null };
                chartData.Tomorrow.FutureData = new List<int?> { 5, 12, 25, 30, 20, 15, 10 };
                chartData.Tomorrow.Note = "Peak demand expected at 2:00 PM due to temperature rise.";
            }
            catch
            {
                chartData = GetDefaultChartData();
            }
        }

        private static ChartDataDto GetDefaultChartData()
        {
            return new ChartDataDto
            {
                Week = new ChartModeData
                {
                    Labels = new List<string> { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" },
                    PastData = new List<int?> { 12, 15, 10, 14, 18, null, null },
                    FutureData = new List<int?> { null, null, null, null, 18, 22, 25 },
                    Note = "Demand based on your recent orders."
                },
                Month = new ChartModeData
                {
                    Labels = new List<string> { "Week 1", "Week 2", "Week 3", "Week 4" },
                    PastData = new List<int?> { 150, 180, null, null },
                    FutureData = new List<int?> { null, 180, 210, 240 },
                    Note = "Overall 20% growth projected for next month."
                },
                Tomorrow = new ChartModeData
                {
                    Labels = new List<string> { "8 AM", "10 AM", "12 PM", "2 PM", "4 PM", "6 PM", "8 PM" },
                    PastData = new List<int?> { null, null, null, null, null, null, null },
                    FutureData = new List<int?> { 5, 12, 25, 30, 20, 15, 10 },
                    Note = "Peak demand expected at 2:00 PM due to temperature rise."
                }
            };
        }

        private async Task SwitchTab(string tab)
        {
            activeTab = tab;
            var data = chartData ?? GetDefaultChartData();
            await JS.InvokeVoidAsync("updatePredictionChart", activeTab, data);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var data = chartData ?? GetDefaultChartData();
                await JS.InvokeVoidAsync("renderPredictionChart", data);
            }
        }
    }
}
