using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using AI_Driven_Water_Supply.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class Helper_Agent
    {
        private static readonly Regex SupplierMarkdownLink = new(
            @"\[(?<label>[^\]]*)\]\(\s*(?<url>(?:https?://[^)\s]+)?/supplier/[^)\s]+)\s*\)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>Opening tag for supplier grid HTML returned by HydroAI <c>find_water_providers</c>.</summary>
        private static readonly Regex RowG3HtmlOpen = new(
            @"<div\b[^>]*\bclass\s*=\s*[""'][^""']*\brow\s+g-3\b[^""']*[""'][^>]*>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        [Inject] private NavigationManager Nav { get; set; } = default!;
        [Inject] private IAuthService AuthService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;

        private string? _userEmail;

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

        public enum AssistantBlockKind
        {
            Text,
            SupplierCard,
            RawHtml
        }

        public class AssistantContentBlock
        {
            public AssistantBlockKind Kind { get; set; }
            /// <summary>Pre-escaped HTML for Text and RawHtml blocks.</summary>
            public string Html { get; set; } = "";
            public string? CardLabel { get; set; }
            public string? SupplierRoute { get; set; }
        }

        public class ChatMessage
        {
            public string Text { get; set; } = "";
            public bool IsUser { get; set; }
            public string? ImageUrl { get; set; }
            /// <summary>Structured assistant content (e.g. supplier link cards). When null/empty, <see cref="Text"/> is used.</summary>
            public List<AssistantContentBlock>? AssistantBlocks { get; set; }
        }

        private List<ChatMessage> CurrentConversation = new();

        protected override async Task OnInitializedAsync()
        {
            var user = AuthService.CurrentUser;
            if (user == null) { await AuthService.TryRefreshSession(); user = AuthService.CurrentUser; }

            if (user != null)
            {
                _userEmail = user.Email;
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
                Text = WebUtility.HtmlEncode(msgText).Replace("\n", "<br>", StringComparison.Ordinal),
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
                    image = imgData,
                    user_email = _userEmail
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var http = HttpClientFactory.CreateClient("AiAgent");
                var response = await http.PostAsync("chat", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseString);

                    if (doc.RootElement.TryGetProperty("response", out var respElement))
                    {
                        string botReply = respElement.GetString() ?? "";
                        var blocks = ParseAssistantReply(botReply);
                        CurrentConversation.Add(new ChatMessage
                        {
                            Text = blocks.Count == 0 ? WebUtility.HtmlEncode(botReply).Replace("\n", "<br>", StringComparison.Ordinal) : "",
                            AssistantBlocks = blocks.Count > 0 ? blocks : null,
                            IsUser = false
                        });
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

        private void NavigateToSupplier(string? route)
        {
            if (string.IsNullOrWhiteSpace(route)) return;
            Nav.NavigateTo(route.Trim());
        }

        private void SupplierCardKeyNavigate(KeyboardEventArgs e, string? route)
        {
            if (e.Key == "Enter" || e.Key == " ")
                NavigateToSupplier(route);
        }

        private static string FormatPlainAssistantHtml(string plain)
        {
            return WebUtility.HtmlEncode(plain).Replace("\n", "<br>", StringComparison.Ordinal);
        }

        private static string NormalizeSupplierAppPath(string urlOrPath)
        {
            var u = urlOrPath.Trim();
            if (u.StartsWith("/supplier/", StringComparison.OrdinalIgnoreCase))
                return Uri.UnescapeDataString(u);
            if (Uri.TryCreate(u, UriKind.Absolute, out var abs))
            {
                var path = abs.AbsolutePath;
                return path.StartsWith("/supplier/", StringComparison.OrdinalIgnoreCase)
                    ? Uri.UnescapeDataString(path)
                    : Uri.UnescapeDataString(path);
            }
            return Uri.UnescapeDataString(u);
        }

        private static void AppendMarkdownSupplierSegments(List<AssistantContentBlock> blocks, string segment)
        {
            if (string.IsNullOrEmpty(segment))
                return;

            var matches = SupplierMarkdownLink.Matches(segment);
            if (matches.Count == 0)
            {
                blocks.Add(new AssistantContentBlock
                {
                    Kind = AssistantBlockKind.Text,
                    Html = FormatPlainAssistantHtml(segment)
                });
                return;
            }

            var last = 0;
            foreach (Match m in matches)
            {
                if (m.Index > last)
                {
                    var before = segment.Substring(last, m.Index - last);
                    if (before.Length > 0)
                    {
                        blocks.Add(new AssistantContentBlock
                        {
                            Kind = AssistantBlockKind.Text,
                            Html = FormatPlainAssistantHtml(before)
                        });
                    }
                }

                var route = NormalizeSupplierAppPath(m.Groups["url"].Value);
                var label = m.Groups["label"].Value.Trim();
                if (string.IsNullOrEmpty(label))
                    label = "View supplier profile";

                blocks.Add(new AssistantContentBlock
                {
                    Kind = AssistantBlockKind.SupplierCard,
                    CardLabel = label,
                    SupplierRoute = route
                });
                last = m.Index + m.Length;
            }

            if (last < segment.Length)
            {
                var tail = segment.Substring(last);
                if (tail.Length > 0)
                {
                    blocks.Add(new AssistantContentBlock
                    {
                        Kind = AssistantBlockKind.Text,
                        Html = FormatPlainAssistantHtml(tail)
                    });
                }
            }
        }

        private static bool TryCloseDivAt(string s, int i, out int endAfterTag)
        {
            endAfterTag = i;
            if (i >= s.Length || s[i] != '<')
                return false;
            if (i + 2 >= s.Length || s[i + 1] != '/')
                return false;
            var j = i + 2;
            while (j < s.Length && char.IsWhiteSpace(s[j]))
                j++;
            if (j + 3 > s.Length || !s.AsSpan(j, 3).Equals("div", StringComparison.OrdinalIgnoreCase))
                return false;
            j += 3;
            while (j < s.Length && char.IsWhiteSpace(s[j]))
                j++;
            if (j >= s.Length || s[j] != '>')
                return false;
            endAfterTag = j + 1;
            return true;
        }

        private static bool TryOpenDivAt(string s, int i, out int endAfterTag, out bool selfClosing)
        {
            selfClosing = false;
            endAfterTag = i;
            if (i >= s.Length || s[i] != '<')
                return false;
            if (i + 4 > s.Length || !s.AsSpan(i, 4).Equals("<div", StringComparison.OrdinalIgnoreCase))
                return false;
            if (i + 4 < s.Length && char.IsLetterOrDigit(s[i + 4]))
                return false;
            var gt = s.IndexOf('>', i);
            if (gt < 0)
                return false;
            var inner = s.AsSpan(i, gt - i + 1);
            selfClosing = inner.IndexOf("/>".AsSpan(), StringComparison.Ordinal) >= 0
                || (inner.Length >= 2 && inner[^2] == '/' && inner[^1] == '>');
            endAfterTag = gt + 1;
            return true;
        }

        private static int FindBalancedDivEndFromOpenDiv(string s, int openAngleIndex)
        {
            if (!TryOpenDivAt(s, openAngleIndex, out var afterOpen, out var selfClose))
                return Math.Min(s.Length, openAngleIndex + 1);
            if (selfClose)
                return afterOpen;

            var depth = 1;
            var i = afterOpen;
            while (i < s.Length)
            {
                if (i + 4 <= s.Length && s.AsSpan(i, 4).Equals("<!--", StringComparison.Ordinal))
                {
                    var endCom = s.IndexOf("-->", i + 4, StringComparison.Ordinal);
                    i = endCom >= 0 ? endCom + 3 : s.Length;
                    continue;
                }

                if (s[i] != '<')
                {
                    i++;
                    continue;
                }

                if (TryCloseDivAt(s, i, out var afterClose))
                {
                    depth--;
                    i = afterClose;
                    if (depth == 0)
                        return i;
                    continue;
                }

                if (TryOpenDivAt(s, i, out var afterInnerOpen, out var innerSelfClose))
                {
                    if (!innerSelfClose)
                        depth++;
                    i = afterInnerOpen;
                    continue;
                }

                var nextGt = s.IndexOf('>', i);
                i = nextGt >= 0 ? nextGt + 1 : i + 1;
            }

            return s.Length;
        }

        private static List<AssistantContentBlock> ParseAssistantReply(string botReply)
        {
            var blocks = new List<AssistantContentBlock>();
            if (string.IsNullOrEmpty(botReply))
                return blocks;

            if (RowG3HtmlOpen.IsMatch(botReply))
            {
                var cursor = 0;
                while (cursor < botReply.Length)
                {
                    var m = RowG3HtmlOpen.Match(botReply, cursor);
                    if (!m.Success)
                    {
                        AppendMarkdownSupplierSegments(blocks, botReply.Substring(cursor));
                        return blocks;
                    }

                    if (m.Index > cursor)
                        AppendMarkdownSupplierSegments(blocks, botReply.Substring(cursor, m.Index - cursor));

                    var end = FindBalancedDivEndFromOpenDiv(botReply, m.Index);
                    if (end <= m.Index)
                    {
                        cursor = m.Index + 1;
                        continue;
                    }

                    blocks.Add(new AssistantContentBlock
                    {
                        Kind = AssistantBlockKind.RawHtml,
                        Html = botReply.Substring(m.Index, end - m.Index)
                    });
                    cursor = end;
                }

                return blocks;
            }

            var trimmedStart = botReply.TrimStart();
            if (trimmedStart.StartsWith("<div", StringComparison.OrdinalIgnoreCase))
            {
                blocks.Add(new AssistantContentBlock { Kind = AssistantBlockKind.RawHtml, Html = botReply });
                return blocks;
            }

            AppendMarkdownSupplierSegments(blocks, botReply);
            return blocks;
        }
    }
}
