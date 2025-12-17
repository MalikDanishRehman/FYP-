using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Presentation.Services;
using Microsoft.AspNetCore.Components;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class Login : ComponentBase
    {
        [Inject] public IAuthService AuthService { get; set; } = default!;
        [Inject] public Supabase.Client _supabase { get; set; } = default!;
        [Inject] public NavigationManager Nav { get; set; } = default!;

        // ⚠️ FIX: Agar "Ambiguity" error aye, to yahan se ye line hata dein 
        // aur Login.razor ke top par "@inject ToastService ToastService" likh dein.
        // Filhal main yahan se hata raha hun taake wo @inject use kare.
       
        // Form Binding
        [SupplyParameterFromForm]
        protected LoginModel loginModel { get; set; } = new();

        // ⚠️ FIX: Ye variables wapis add kiye hain taake HTML error na de
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
                            ToastService.ShowToast("Welcome Back", "Login successful.", "success");

                            if (profile.Role == "Consumer")
                                Nav.NavigateTo("/Consumer", forceLoad: true);
                            else
                                Nav.NavigateTo("/ProviderDashboard", forceLoad: true);
                        }
                        else
                        {
                            ToastService.ShowToast("Profile Missing", "Please complete your profile.", "info");
                            Nav.NavigateTo("/get-started");
                        }
                    }
                }
                else
                {
                    // Error message variable bhi set karein aur Toast bhi dikhayein
                    errorMessage = "Invalid email or password.";
                    ToastService.ShowToast("Login Failed", errorMessage, "error");
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Login Error: " + ex.Message;
                Console.WriteLine(errorMessage);
                ToastService.ShowToast("System Error", "Unable to log in.", "error");
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