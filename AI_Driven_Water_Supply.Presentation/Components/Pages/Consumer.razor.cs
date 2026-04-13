using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Domain.Entities;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class Consumer : IDisposable
    {
        private readonly SemaphoreSlim _supplierSearchLock = new(1, 1);
        [Inject] private Supabase.Client _supabase { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;
        [Inject] private IAuthService AuthService { get; set; } = default!;

        private string MyName = "";

        private const int PageSize = 12;
        private const int TopRatedLimit = 20;

        private string _searchQuery = "";
        private string? _serviceFilterToken;
        private readonly List<SupplierSearchRow> _supplierResults = new();
        private bool _supplierSearchLoading;
        private bool _supplierLoadMoreLoading;
        private bool _hasMoreSuppliers;
        private int _searchDebounceVersion;
        private CancellationTokenSource? _debounceCts;

        private SupplierOverviewTab _overviewTab = SupplierOverviewTab.All;
        private string _sortColumn = "username";
        private bool _sortAsc = true;

        private readonly HashSet<string> _selectedUsernames = new(StringComparer.OrdinalIgnoreCase);

        public enum SupplierOverviewTab
        {
            All,
            Active,
            TopRated
        }

        public sealed class SupplierSearchRow
        {
            public string Username { get; init; } = "";
            public double Rating { get; init; }
            public string ImageUrl { get; init; } = "";
            public IReadOnlyList<string> ServiceLabels { get; init; } = Array.Empty<string>();
        }

        protected override async Task OnInitializedAsync()
        {
            var user = AuthService.CurrentUser;
            if (user == null)
            {
                await AuthService.TryRefreshSession();
                user = AuthService.CurrentUser;
            }

            if (user != null)
            {
                try
                {
                    var response = await _supabase.From<Profile>().Where(x => x.Id == user.Id).Get();
                    var profile = response.Models.FirstOrDefault();

                    if (profile != null)
                        MyName = profile.Username ?? "";

                    if (string.IsNullOrEmpty(MyName) && user.UserMetadata != null && user.UserMetadata.ContainsKey("username"))
                        MyName = user.UserMetadata["username"]?.ToString() ?? "";
                    if (string.IsNullOrEmpty(MyName) && !string.IsNullOrEmpty(user.Email))
                        MyName = user.Email.Split('@')[0];
                }
                catch (Exception ex) { Console.WriteLine("Profile Error: " + ex.Message); }
            }

            await ExecuteSupplierSearchAsync(resetList: true);
        }

        public void Dispose()
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _supplierSearchLock.Dispose();
        }

        private void GoToBotPage() => Nav.NavigateTo("/Helper_Agent");

        private static string SanitizeIlikeTerm(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var t = raw.Trim();
            return t.Replace("\\", "", StringComparison.Ordinal)
                    .Replace("%", "", StringComparison.Ordinal)
                    .Replace("_", "", StringComparison.Ordinal);
        }

        private static IReadOnlyList<string> ParseServiceLabels(string? servicesCsv)
        {
            if (string.IsNullOrWhiteSpace(servicesCsv)) return Array.Empty<string>();
            var parts = servicesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var labels = new List<string>();
            foreach (var p in parts)
            {
                if (p.Equals("Bottle", StringComparison.OrdinalIgnoreCase)) labels.Add("Bottle");
                else if (p.Equals("Plant", StringComparison.OrdinalIgnoreCase)) labels.Add("RO plant");
                else if (p.Equals("Tanker", StringComparison.OrdinalIgnoreCase)) labels.Add("Tanker");
                else labels.Add(p);
            }
            return labels;
        }

        private static string? MapTokenToOrderRouteType(string? token) => token switch
        {
            "Bottle" => "bottle",
            "Plant" => "plant",
            "Tanker" => "tanker",
            _ => null
        };

        private Task SetServiceFilter(string? token)
        {
            _debounceCts?.Cancel();
            _serviceFilterToken = string.IsNullOrEmpty(token) ? null : token;
            return ExecuteSupplierSearchAsync(resetList: true);
        }

        private Task OnServiceFilterChange(ChangeEventArgs e)
        {
            var v = e.Value?.ToString();
            return SetServiceFilter(string.IsNullOrEmpty(v) ? null : v);
        }

        private Task SetOverviewTab(SupplierOverviewTab tab)
        {
            if (_overviewTab == tab) return Task.CompletedTask;
            _overviewTab = tab;
            _debounceCts?.Cancel();
            return ExecuteSupplierSearchAsync(resetList: true);
        }

        private Task SetSort(string column)
        {
            if (_overviewTab == SupplierOverviewTab.TopRated)
                return Task.CompletedTask;

            if (_sortColumn == column)
                _sortAsc = !_sortAsc;
            else
            {
                _sortColumn = column;
                _sortAsc = column != "rating";
            }

            _debounceCts?.Cancel();
            return ExecuteSupplierSearchAsync(resetList: true);
        }

        private void ToggleRowSelected(string username, ChangeEventArgs e)
        {
            var on = e.Value switch
            {
                bool b => b,
                string s => bool.TryParse(s, out var x) && x,
                _ => false
            };
            if (on) _selectedUsernames.Add(username);
            else _selectedUsernames.Remove(username);
        }

        private bool IsRowSelected(string username) => _selectedUsernames.Contains(username);

        private async Task OnSearchQueryInput(ChangeEventArgs e)
        {
            _searchQuery = e.Value?.ToString() ?? "";
            await DebouncedSearchAsync();
        }

        private async Task DebouncedSearchAsync()
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = new CancellationTokenSource();
            var ct = _debounceCts.Token;
            var version = ++_searchDebounceVersion;
            try
            {
                await Task.Delay(380, ct);
            }
            catch (TaskCanceledException)
            {
                return;
            }
            if (version != _searchDebounceVersion || ct.IsCancellationRequested) return;
            await ExecuteSupplierSearchAsync(resetList: true);
        }

        private Task OnSearchSubmit()
        {
            _debounceCts?.Cancel();
            return ExecuteSupplierSearchAsync(resetList: true);
        }

        private Task OnSearchKeyDown(KeyboardEventArgs e)
        {
            if (e.Key != "Enter" && e.Key != "NumpadEnter") return Task.CompletedTask;
            return OnSearchSubmit();
        }

        private async Task ExecuteSupplierSearchAsync(bool resetList)
        {
            await _supplierSearchLock.WaitAsync();
            try
            {
                if (resetList)
                {
                    _supplierResults.Clear();
                    _hasMoreSuppliers = false;
                }

                var loadingFull = resetList || _supplierResults.Count == 0;
                if (loadingFull) _supplierSearchLoading = true;
                else _supplierLoadMoreLoading = true;
                await InvokeAsync(StateHasChanged);

                var nameTerm = SanitizeIlikeTerm(_searchQuery);
                var query = _supabase.From<Profile>()
                    .Where(x => x.Role == "Provider");

                if (!string.IsNullOrEmpty(nameTerm))
                    query = query.Filter("username", Supabase.Postgrest.Constants.Operator.ILike, $"%{nameTerm}%");

                if (!string.IsNullOrEmpty(_serviceFilterToken))
                    query = query.Filter("services", Supabase.Postgrest.Constants.Operator.ILike, $"%{_serviceFilterToken}%");

                var isTopRated = _overviewTab == SupplierOverviewTab.TopRated;
                List<Profile> models;

                if (isTopRated)
                {
                    var response = await query
                        .Order("rating", Supabase.Postgrest.Constants.Ordering.Descending)
                        .Range(0, TopRatedLimit - 1)
                        .Get();
                    models = response.Models;
                    _hasMoreSuppliers = false;
                }
                else
                {
                    var offset = _supplierResults.Count;
                    var end = offset + PageSize;
                    var orderCol = _sortColumn == "rating" ? "rating" : "username";
                    var orderDir = _sortAsc ? Supabase.Postgrest.Constants.Ordering.Ascending : Supabase.Postgrest.Constants.Ordering.Descending;

                    var response = await query
                        .Order(orderCol, orderDir)
                        .Range(offset, end)
                        .Get();

                    models = response.Models;
                    if (models.Count > PageSize)
                    {
                        _hasMoreSuppliers = true;
                        models = models.Take(PageSize).ToList();
                    }
                    else
                    {
                        _hasMoreSuppliers = false;
                    }
                }

                foreach (var p in models)
                {
                    var user = p.Username ?? "";
                    if (string.IsNullOrEmpty(user)) continue;
                    var img = string.IsNullOrEmpty(p.ProfilePic)
                        ? "/images/fallbackimg.jpg"
                        : _supabase.Storage.From("Avatar").GetPublicUrl(p.ProfilePic) ?? "/images/fallbackimg.jpg";
                    _supplierResults.Add(new SupplierSearchRow
                    {
                        Username = user,
                        Rating = p.Rating ?? 0,
                        ImageUrl = img,
                        ServiceLabels = ParseServiceLabels(p.Services)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Supplier search: " + ex.Message);
            }
            finally
            {
                _supplierSearchLoading = false;
                _supplierLoadMoreLoading = false;
                await InvokeAsync(StateHasChanged);
                _supplierSearchLock.Release();
            }
        }

        private Task LoadMoreSuppliers()
        {
            if (_overviewTab == SupplierOverviewTab.TopRated) return Task.CompletedTask;
            return ExecuteSupplierSearchAsync(resetList: false);
        }

        private void OpenSupplier(string username) => Nav.NavigateTo($"/supplier/{username}");

        private void OpenOrderMapForFilter()
        {
            var t = MapTokenToOrderRouteType(_serviceFilterToken);
            if (t != null) Nav.NavigateTo($"/order/{t}");
        }

        private static string RatingToneClass(double r) => r switch
        {
            >= 4.0 => "sv-rating--high",
            >= 3.0 => "sv-rating--mid",
            _ => "sv-rating--low"
        };

        private string SortIconClass(string column)
        {
            if (_overviewTab == SupplierOverviewTab.TopRated) return "bi-chevron-expand text-muted";
            if (_sortColumn != column) return "bi-chevron-expand sv-th-icon--muted";
            return _sortAsc ? "bi-chevron-up" : "bi-chevron-down";
        }

        private string SortIconUsername() => SortIconClass("username");

        private string SortIconRating() => SortIconClass("rating");

        private static string ClassificationLabel(double rating) => rating switch
        {
            >= 4.5 => "Preferred supplier",
            >= 3.5 => "Active supplier",
            _ => "Standard supplier"
        };
    }
}
