using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Domain.Entities;
using Supabase.Postgrest.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class Profileprovider : IDisposable
    {
        [Inject] private Supabase.Client _supabase { get; set; } = default!;
        [Inject] private IAuthService AuthService { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        private Profile userProfile = new Profile();
        private string DisplayUrl = "";
        private bool isLoading = true;
        private bool isSaving = false;
        private bool isUploading = false;
        private bool showImageModal = false;
        private bool isMapInitialized = false;
        private DotNetObjectReference<Profileprovider>? dotNetHelper;

        protected override async Task OnInitializedAsync()
        {
            dotNetHelper = DotNetObjectReference.Create(this);
            await LoadProfile();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!isLoading && !isMapInitialized)
            {
                try
                {
                    await Task.Delay(500);
                    await JS.InvokeVoidAsync("mapFunctions.initPickMap", "pickMap", dotNetHelper, userProfile.Location);
                    isMapInitialized = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Map Init Error: " + ex.Message);
                }
            }
        }

        private async Task TriggerFileUpload()
        {
            await JS.InvokeVoidAsync("triggerFileClick");
        }

        [JSInvokable]
        public void UpdateLocationFromMap(string address)
        {
            userProfile.Location = address;
            StateHasChanged();
        }

        [JSInvokable]
        public void UpdateLocationWithCoords(string address, double lat, double lng)
        {
            userProfile.Location = address;
            userProfile.Latitude = lat;
            userProfile.Longitude = lng;
            StateHasChanged();
        }

        private async Task SearchAddress()
        {
            if (!string.IsNullOrEmpty(userProfile.Location))
            {
                await JS.InvokeVoidAsync("window.searchAddressOnMap", userProfile.Location);
            }
        }

        private async Task LoadProfile()
        {
            try
            {
                var user = AuthService.CurrentUser;
                if (user == null)
                {
                    await AuthService.TryRefreshSession();
                    user = AuthService.CurrentUser;
                }

                if (user != null && !string.IsNullOrEmpty(user.Id))
                {
                    var response = await _supabase.From<Profile>().Where(x => x.Id == user.Id).Get();
                    var data = response.Models.FirstOrDefault();

                    if (data != null)
                    {
                        userProfile = data;
                        if (string.IsNullOrEmpty(userProfile.Id)) userProfile.Id = user.Id;
                        if (!string.IsNullOrEmpty(userProfile.ProfilePic))
                            DisplayUrl = _supabase.Storage.From("Avatar").GetPublicUrl(userProfile.ProfilePic);
                    }
                    else
                    {
                        userProfile.Id = user.Id;
                    }
                }
                else
                {
                    Nav.NavigateTo("/login");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Load Error: " + ex.Message);
                ToastService.ShowToast("Error", "Failed to load profile.", "error");
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task UploadImage(InputFileChangeEventArgs e)
        {
            isUploading = true;
            try
            {
                var user = AuthService.CurrentUser;
                if (user == null) return;

                var file = e.File;
                using var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
                using var memoryStream = new System.IO.MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();

                string fileExtension = System.IO.Path.GetExtension(file.Name);
                string uniqueFileId = $"{user.Id}_{DateTime.Now.Ticks}{fileExtension}";

                await _supabase.Storage.From("Avatar").Upload(bytes, uniqueFileId);

                await _supabase.From<Profile>()
                            .Where(x => x.Id == user.Id)
                            .Set(x => x.ProfilePic, uniqueFileId)
                            .Update();

                userProfile.ProfilePic = uniqueFileId;
                DisplayUrl = _supabase.Storage.From("Avatar").GetPublicUrl(uniqueFileId);
                showImageModal = false;
                ToastService.ShowToast("Success", "Profile Picture Updated!", "success");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Upload Error: " + ex.Message);
                ToastService.ShowToast("Error", "Upload failed.", "error");
            }
            finally
            {
                isUploading = false;
                StateHasChanged();
            }
        }

        private async Task HandleSave()
        {
            isSaving = true;
            try
            {
                await _supabase.From<Profile>()
                            .Where(x => x.Id == userProfile.Id)
                            .Set(x => x.Username, userProfile.Username)
                            .Set(x => x.Location, userProfile.Location)
                            .Set(x => x.Latitude, userProfile.Latitude ?? 0.0)
                            .Set(x => x.Longitude, userProfile.Longitude ?? 0.0)
                            .Update();

                ToastService.ShowToast("Success", "Profile & Location Saved!", "success");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Save Error: " + ex.Message);
                ToastService.ShowToast("Error", "Could not save profile.", "error");
            }
            finally
            {
                isSaving = false;
                StateHasChanged();
            }
        }

        private void GoBack()
        {
            if (userProfile.Role == "Provider") Nav.NavigateTo("/ProviderDashboard");
            else Nav.NavigateTo("/Consumer");
        }

        public void Dispose()
        {
            dotNetHelper?.Dispose();
        }
    }
}
