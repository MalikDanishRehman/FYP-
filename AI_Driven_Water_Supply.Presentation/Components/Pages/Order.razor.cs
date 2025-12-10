using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages // ⚠️ Apna Namespace check karein
{
    public partial class Order : ComponentBase
    {
        [Inject] public NavigationManager Nav { get; set; } = default!;
        [Inject] public IJSRuntime JS { get; set; } = default!;
        [Inject] public Supabase.Client SupabaseClient { get; set; } = default!;

        [Parameter] public string? type { get; set; }

        // ✅ UI Record
        public record Supplier(string name, double lat, double lng, double rating);

        // ✅ Supabase Model
        [Table("profiles")]
        public class Profile : BaseModel
        {
            [PrimaryKey("id")]
            public string Id { get; set; } = default!;

            [Column("username")]
            public string Username { get; set; } = string.Empty;

            [Column("role")]
            public string Role { get; set; } = string.Empty;
        }

        protected List<Supplier> allSuppliers = new();
        protected List<Supplier> filteredSuppliers = new();
        protected bool isLoading = true;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                // 1. Fetch Providers from Supabase
                var response = await SupabaseClient.From<Profile>()
                                     .Where(x => x.Role == "Provider")
                                     .Get();

                var dbProfiles = response.Models;

                // 2. Convert to UI Model
                allSuppliers = dbProfiles.Select(p => new Supplier(
                    p.Username,
                    24.8600 + (new Random().NextDouble() * 0.01), // Dummy Lat
                    67.0000 + (new Random().NextDouble() * 0.01), // Dummy Lng
                    4.5 // Default Rating
                )).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching data: " + ex.Message);
            }
            finally
            {
                isLoading = false;
                FilterSuppliers();
            }
        }

        protected override void OnParametersSet()
        {
            FilterSuppliers();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!isLoading && filteredSuppliers.Any())
            {
                try
                {
                    await JS.InvokeVoidAsync("mapFunctions.loadMap", type, filteredSuppliers);
                }
                catch { /* Ignore JS errors if map not ready */ }
            }
        }

        protected string GetStars(double rating)
        {
            int fullStars = (int)Math.Floor(rating);
            bool halfStar = rating - fullStars >= 0.5;
            string stars = new string('★', fullStars);
            if (halfStar) stars += "½";
            return stars;
        }

        protected void FilterSuppliers()
        {
            if (allSuppliers == null || !allSuppliers.Any()) return;

            var t = type?.Trim().ToLower() ?? "";

            // Abhi ke liye sab dikha rahe hain (future mein type se filter karna)
            filteredSuppliers = allSuppliers;
        }

        protected void GoToDetails(string supplierName)
        {
            Nav.NavigateTo($"/supplier/{Uri.EscapeDataString(supplierName)}");
        }
    }
}