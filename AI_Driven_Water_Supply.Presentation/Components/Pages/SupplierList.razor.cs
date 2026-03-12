using Microsoft.AspNetCore.Components;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Text.Json.Serialization;
using AI_Driven_Water_Supply.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class SupplierList
    {
        [Parameter] public string? type { get; set; }

        [Inject] private Supabase.Client _supabase { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        private bool isLoading = true;
        private List<SupplierViewModel> filteredSuppliers = new();
        private bool mapInitialized = false;

        public class SupplierViewModel
        {
            public string Name { get; set; } = "";
            public double Rating { get; set; }
            public string ImageUrl { get; set; } = "";
            public double Lat { get; set; }
            public double Lng { get; set; }
            public double Distance { get; set; }
        }

        public class LocationCoords
        {
            [JsonPropertyName("lat")]
            public double Lat { get; set; }
            [JsonPropertyName("lng")]
            public double Lng { get; set; }
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371;
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double angle) => (Math.PI / 180) * angle;

        private async Task<LocationCoords?> GeocodeAddressAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return null;
            try
            {
                return await JSRuntime.InvokeAsync<LocationCoords>("geocodeAddress", address);
            }
            catch
            {
                return null;
            }
        }

        protected override void OnInitialized()
        {
            isLoading = true;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await LoadAndSortSuppliersAsync();
            }
        }

        private async Task LoadAndSortSuppliersAsync()
        {
            try
            {
                isLoading = true;
                StateHasChanged();

                LocationCoords userLoc;
                try
                {
                    userLoc = await JSRuntime.InvokeAsync<LocationCoords>("getUserLocation");
                }
                catch
                {
                    userLoc = new LocationCoords { Lat = 24.8607, Lng = 67.0011 };
                }

                var response = await _supabase.From<Profile>()
                                              .Where(x => x.Role == "Provider")
                                              .Get();

                var unsortedList = new List<SupplierViewModel>();

                foreach (var item in response.Models)
                {
                    bool hasService = false;

                    if (!string.IsNullOrEmpty(item.Services) && !string.IsNullOrEmpty(type))
                    {
                        var providerServices = item.Services.Split(',').Select(s => s.Trim()).ToList();
                        if (providerServices.Contains(type, StringComparer.OrdinalIgnoreCase))
                        {
                            hasService = true;
                        }
                    }

                    if (hasService)
                    {
                        string finalImageUrl = string.IsNullOrEmpty(item.ProfilePic)
                            ? "/images/fallbackimg.jpg"
                            : _supabase.Storage.From("Avatar").GetPublicUrl(item.ProfilePic);

                        double pLat;
                        double pLng;

                        if (item.Latitude.HasValue && item.Longitude.HasValue)
                        {
                            pLat = item.Latitude.Value;
                            pLng = item.Longitude.Value;
                        }
                        else if (!string.IsNullOrWhiteSpace(item.Location))
                        {
                            var coords = await GeocodeAddressAsync(item.Location);
                            if (coords != null)
                            {
                                pLat = coords.Lat;
                                pLng = coords.Lng;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }

                        double dist = CalculateDistance(userLoc.Lat, userLoc.Lng, pLat, pLng);

                        unsortedList.Add(new SupplierViewModel
                        {
                            Name = item.Username ?? "",
                            Rating = item.Rating ?? 0,
                            ImageUrl = finalImageUrl,
                            Lat = pLat,
                            Lng = pLng,
                            Distance = dist
                        });
                    }
                }

                filteredSuppliers = unsortedList
                    .OrderBy(s => s.Distance)
                    .ThenByDescending(s => s.Rating)
                    .ToList();

                if (filteredSuppliers.Count > 0 && !mapInitialized)
                {
                    await JSRuntime.InvokeVoidAsync("initMap", filteredSuppliers, userLoc);
                    mapInitialized = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private void GoToDetails(string name) => Nav.NavigateTo($"/supplier/{name}");
    }
}
