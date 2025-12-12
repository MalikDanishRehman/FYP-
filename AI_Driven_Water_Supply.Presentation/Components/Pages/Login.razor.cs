using Microsoft.AspNetCore.Components;
using AI_Driven_Water_Supply.Application.Interfaces;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages // ⚠️ Check Namespace
{
    public partial class Login : ComponentBase
    {
        [Inject] public IAuthService AuthService { get; set; } = default!;
        [Inject] public Supabase.Client _supabase { get; set; } = default!;
        [Inject] public NavigationManager Nav { get; set; } = default!;

        // Form Binding
        [SupplyParameterFromForm]
        protected LoginModel loginModel { get; set; } = new();

        protected string errorMessage = "";
        protected bool isLoading = false;

        // 👇 Supabase Model for Role Checking
        [Table("profiles")]
        public class UserProfile : BaseModel
        {
            [Column("id")] public string Id { get; set; } = "";
            [Column("role")] public string Role { get; set; } = "";
        }

        protected async Task HandleLogin()
        {
            isLoading = true;
            errorMessage = "";

            try
            {
                // 1. Auth Service se Login
                bool isLoggedIn = await AuthService.SignIn(loginModel.Email, loginModel.Password);

                if (isLoggedIn)
                {
                    // 2. User ID nikalo
                    var user = AuthService.CurrentUser;
                    if (user == null) user = _supabase.Auth.CurrentUser;

                    if (user != null)
                    {
                        // 3. Supabase se Role check karo
                        var response = await _supabase.From<UserProfile>()
                                                      .Where(x => x.Id == user.Id)
                                                      .Get();

                        var profile = response.Models.FirstOrDefault();

                        if (profile != null)
                        {
                            // 4. Role Based Redirect
                            if (profile.Role == "Consumer")
                            {
                                Nav.NavigateTo("/Consumer", forceLoad: true);
                            }
                            else
                            {
                                Nav.NavigateTo("/ProviderDashboard", forceLoad: true);
                            }
                        }
                        else
                        {
                            // Agar Profile nahi mili (New User?)
                            Nav.NavigateTo("/get-started");
                        }
                    }
                }
                else
                {
                    errorMessage = "Invalid email or password.";
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Login Error: " + ex.Message;
            }
            finally
            {
                isLoading = false;
            }
        }

        public class LoginModel
        {
            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email address")]
            public string Email { get; set; } = "";

            [Required(ErrorMessage = "Password is required")]
            public string Password { get; set; } = "";
        }
    }
}