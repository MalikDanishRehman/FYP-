using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Application.DTOs;
using Microsoft.AspNetCore.Components;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class Login : ComponentBase
    {
        [Inject] public IAuthService AuthService { get; set; } = default!;
        [Inject] public Supabase.Client _supabase { get; set; } = default!;
        [Inject] public NavigationManager Nav { get; set; } = default!;
        [Inject] public IToastService _toastService { get; set; } = default!;

        [SupplyParameterFromForm]
        protected LoginModel loginModel { get; set; } = new();

        protected string errorMessage = "";
        protected bool isLoading = false;

        [Table("profiles")]
        public class UserProfile : BaseModel
        {
            [Column("id")] public string Id { get; set; } = "";
            [Column("role")] public string Role { get; set; } = "";
        }

        protected async Task HandleLogin()
        {
            isLoading = true;
            errorMessage = ""; // Reset error

            try
            {
                bool isLoggedIn = await AuthService.SignIn(loginModel.Email, loginModel.Password);

                if (isLoggedIn)
                {
                    var user = AuthService.CurrentUser;
                    if (user == null) user = _supabase.Auth.CurrentUser;

                    if (user != null)
                    {
                        var response = await _supabase.From<UserProfile>()
                                                      .Where(x => x.Id == user.Id)
                                                      .Get();

                        var profile = response.Models.FirstOrDefault();

                        if (profile != null)
                        {
                            // ✅ Updated call using _toastService
                            _toastService.ShowToast("Welcome Back", "Login successful.", "success");

                            if (profile.Role == "Consumer")
                            {
                                Nav.NavigateTo("/Consumer", forceLoad: true);
                            }
                            else if (profile.Role == "admin")
                            {
                                Nav.NavigateTo("/AdminPage", forceLoad: true);
                            }
                            else if (profile.Role == "Provider")
                            {
                                Nav.NavigateTo("/ProviderDashboard", forceLoad: true);
                            }
                            else
                            {
                                // Fallback for unknown role
                                Nav.NavigateTo("/", forceLoad: true);
                            }
                        }
                        else
                        {
                            _toastService.ShowToast("Profile Missing", "Please complete your profile.", "info");
                            Nav.NavigateTo("/get-started");
                        }
                    }
                }
                else
                {
                    errorMessage = "Invalid email or password.";
                    _toastService.ShowToast("Login Failed", errorMessage, "error");
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Login Error: " + ex.Message;
                Console.WriteLine(errorMessage);
                _toastService.ShowToast("System Error", "Unable to log in.", "error");
            }
            finally
            {
                isLoading = false;
            }
        }
    }
}