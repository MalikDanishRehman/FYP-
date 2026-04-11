using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.WebUtilities;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class Signup
    {
        [Inject] private Supabase.Client _supabase { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        [SupplyParameterFromForm]
        private SignupModel signupModel { get; set; } = new();

        private string message = "";
        private bool isSuccess = false;
        private bool isLoading = false;
        private string? servicesParam = null;

        // Yeh 4 lines add karni hain
        protected bool showPassword = false;
        protected string passwordInputType => showPassword ? "text" : "password";
        protected string passwordIcon => showPassword ? "bi-eye-slash" : "bi-eye";

        protected void TogglePasswordVisibility()
        {
            showPassword = !showPassword;
        }

        [Table("profiles")]
        public class UserProfile : BaseModel
        {
            [Column("id")] public string Id { get; set; } = string.Empty;
            [Column("username")] public string Username { get; set; } = string.Empty;
            [Column("role")] public string Role { get; set; } = string.Empty;
            [Column("services")] public string Services { get; set; } = "";
        }

        protected override void OnInitialized()
        {
            var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
            var query = QueryHelpers.ParseQuery(uri.Query);

            if (query.TryGetValue("role", out var r))
            {
                signupModel.Role = r.ToString() ?? "Consumer";
            }
            else
            {
                signupModel.Role = "Consumer";
            }

            if (query.TryGetValue("services", out var s))
            {
                servicesParam = s.ToString();
            }
        }

        private async Task HandleSignup()
        {
            isLoading = true;
            message = "";

            try
            {
                string autoUsername = signupModel.Email.Split('@')[0];

                var options = new Supabase.Gotrue.SignUpOptions { Data = new Dictionary<string, object> { { "username", autoUsername } } };
                var session = await _supabase.Auth.SignUp(signupModel.Email, signupModel.Password, options);

                if (session != null && session.User != null)
                {
                    var newProfile = new UserProfile
                    {
                        Id = session.User.Id,
                        Username = autoUsername,
                        Role = signupModel.Role,
                        Services = (signupModel.Role == "Provider") ? (servicesParam ?? "") : ""
                    };

                    await _supabase.From<UserProfile>().Insert(newProfile);

                    isSuccess = true;
                    message = "Account created! Redirecting...";
                    await Task.Delay(1500);
                    NavigationManager.NavigateTo("/login");
                }
                else
                {
                    isSuccess = false;
                    message = "Signup failed. Please try again.";
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                message = $"Error: {ex.Message}";
            }
            finally
            {
                isLoading = false;
            }
        }

        public class SignupModel
        {
            [Required, EmailAddress] public string Email { get; set; } = "";
            [Required, MinLength(6)] public string Password { get; set; } = "";
            public string Role { get; set; } = "Consumer";
        }
    }
}
