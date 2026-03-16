using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using AI_Driven_Water_Supply.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class Helper_Agent
    {
        [Inject] private NavigationManager Nav { get; set; } = default!;
        [Inject] private IAuthService AuthService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private HttpClient Http { get; set; } = default!;

        private string MyName = "User";
        private bool hasChatStarted = false;
        private bool isThinking = false;
        private string userInput = "";
        private string loadingText = "Processing...";
        private System.Timers.Timer? _timer;
        private string? selectedImageBase64 = null;
        private string? selectedImagePreview = null;
        private string inputFileId = Guid.NewGuid().ToString();
        private const long MaxFileSize = 5 * 1024 * 1024;

        public class ChatMessage
        {
            public string Text { get; set; } = "";
            public bool IsUser { get; set; }
            public string? ImageUrl { get; set; }
        }

        private List<ChatMessage> CurrentConversation = new();

        protected override async Task OnInitializedAsync()
        {
            var user = AuthService.CurrentUser;
            if (user == null) { await AuthService.TryRefreshSession(); user = AuthService.CurrentUser; }

            if (user != null)
            {
                if (user.UserMetadata != null && user.UserMetadata.TryGetValue("full_name", out var nameObj))
                {
                    MyName = nameObj?.ToString() ?? "User";
                }
                else
                {
                    MyName = !string.IsNullOrEmpty(user.Email) ? user.Email.Split('@')[0] : "User";
                }
            }
            else
            {
                Nav.NavigateTo("/login");
            }
        }

        private async Task HandleFileSelection(InputFileChangeEventArgs e)
        {
            try
            {
                var file = e.File;
                if (file != null)
                {
                    var resizedImage = await file.RequestImageFileAsync(file.ContentType, 800, 800);
                    using (var ms = new MemoryStream())
                    {
                        await resizedImage.OpenReadStream(MaxFileSize).CopyToAsync(ms);
                        var buffer = ms.ToArray();
                        string base64 = Convert.ToBase64String(buffer);
                        string format = file.ContentType;
                        selectedImagePreview = $"data:{format};base64,{base64}";
                        selectedImageBase64 = selectedImagePreview;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Image Error: {ex.Message}");
            }
        }

        private void ClearImageAttachment()
        {
            selectedImageBase64 = null;
            selectedImagePreview = null;
            inputFileId = Guid.NewGuid().ToString();
        }

        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(userInput) && string.IsNullOrEmpty(selectedImageBase64)) return;

            hasChatStarted = true;
            string msgText = userInput;
            string? imgData = selectedImageBase64;

            CurrentConversation.Add(new ChatMessage
            {
                Text = msgText,
                IsUser = true,
                ImageUrl = imgData
            });

            userInput = "";
            ClearImageAttachment();

            isThinking = true;
            StartLoadingTimer();
            await ScrollToBottom();

            try
            {
                var payload = new
                {
                    message = msgText,
                    image = imgData
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await Http.PostAsync("chat", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseString);

                    if (doc.RootElement.TryGetProperty("response", out var respElement))
                    {
                        string botReply = respElement.GetString() ?? "";
                        if (!botReply.Trim().StartsWith("<div"))
                        {
                            botReply = botReply.Replace("\n", "<br>");
                        }
                        CurrentConversation.Add(new ChatMessage { Text = botReply, IsUser = false });
                    }
                }
                else
                {
                    CurrentConversation.Add(new ChatMessage { Text = "⚠️ Backend Error. Check Python Console.", IsUser = false });
                }
            }
            catch (Exception ex)
            {
                CurrentConversation.Add(new ChatMessage { Text = $"⚠️ Connection Failed: {ex.Message}", IsUser = false });
            }
            finally
            {
                StopLoadingTimer();
                isThinking = false;
                await ScrollToBottom();
            }
        }

    private void StartLoadingTimer()
    {
        _timer = new System.Timers.Timer(2000);
            _timer.Elapsed += (s, e) =>
            {
                if (loadingText == "Processing...") loadingText = "Fetching Data...";
                else loadingText = "Processing...";
                InvokeAsync(StateHasChanged);
            };
            _timer.Start();
        }

        private void StopLoadingTimer() { _timer?.Stop(); _timer?.Dispose(); }
        private async Task HandleKeyPress(KeyboardEventArgs e) { if (e.Key == "Enter") await SendMessage(); }
        private async Task ScrollToBottom() { await Task.Delay(100); await JS.InvokeVoidAsync("scrollToBottom", "chatContainer"); }
        private void CloseDropdowns() { }
    }
}
