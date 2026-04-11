using Microsoft.AspNetCore.Components;
using Supabase;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class Admin_UserManage : ComponentBase
    {
        [Inject] private Supabase.Client _supabase { get; set; } = default!;

        protected List<ConsumerModel> Users { get; set; } = new();
        protected bool IsLoading { get; set; } = true;
        protected string searchTerm = "";

        protected override async Task OnInitializedAsync()
        {
            await FetchConsumersFromDb();
        }

        private async Task FetchConsumersFromDb()
        {
            IsLoading = true;
            try
            {
                // DB se un users ko laayein jinka role 'Consumer' hai
                var response = await _supabase.From<ConsumerModel>()
                                              .Where(x => x.Role == "Consumer")
                                              .Get();

                if (response.Models != null)
                {
                    Users = response.Models;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching consumers: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Search box ki logic
        protected IEnumerable<ConsumerModel> FilteredUsers =>
            string.IsNullOrWhiteSpace(searchTerm)
                ? Users
                : Users.Where(u =>
                    !string.IsNullOrEmpty(u.Username) &&
                    u.Username.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
    }

    // Supabase DB Mapping
    [Supabase.Postgrest.Attributes.Table("profiles")]
    public class ConsumerModel : Supabase.Postgrest.Models.BaseModel
    {
        [Supabase.Postgrest.Attributes.Column("id")]
        public string Id { get; set; } = string.Empty;

        [Supabase.Postgrest.Attributes.Column("username")]
        public string? Username { get; set; }

        [Supabase.Postgrest.Attributes.Column("role")]
        public string? Role { get; set; }

        [Supabase.Postgrest.Attributes.Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}