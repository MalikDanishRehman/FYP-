using Microsoft.AspNetCore.Components;
using Supabase;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    // Class ka naam ab theek aapki file ke naam "Admin_VendorMangae" jaisa hai
    public partial class Admin_VendorMangae : ComponentBase
    {
        [Inject] private Supabase.Client _supabase { get; set; } = default!;

        protected List<VendorModel> Vendors { get; set; } = new();
        protected bool IsLoading { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            await FetchVendorsFromDb();
        }

        private async Task FetchVendorsFromDb()
        {
            IsLoading = true;
            try
            {
                // DB se un users ko laayein jinka role 'Provider' hai
                var response = await _supabase.From<VendorModel>()
                                              .Where(x => x.Role == "Provider")
                                              .Get();

                if (response.Models != null)
                {
                    Vendors = response.Models;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching vendors: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [Supabase.Postgrest.Attributes.Table("profiles")]
    public class VendorModel : Supabase.Postgrest.Models.BaseModel
    {
        [Supabase.Postgrest.Attributes.Column("id")]
        public string Id { get; set; } = string.Empty; // Warning hatane ke liye default value di

        [Supabase.Postgrest.Attributes.Column("username")]
        public string? Username { get; set; } // '?' lagane se nullable warnings khatam ho jayengi

        [Supabase.Postgrest.Attributes.Column("role")]
        public string? Role { get; set; }

        [Supabase.Postgrest.Attributes.Column("address")]
        public string? Address { get; set; }

        [Supabase.Postgrest.Attributes.Column("rating")]
        public float? Rating { get; set; }
    }
}