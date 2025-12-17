using System;
using System.Collections.Generic;
using System.Threading.Tasks; // Ye zaroori hai Task.Delay ke liye

namespace AI_Driven_Water_Supply.Presentation.Services
{
    // 👇 PEHLE YE "ToastService" THA, AB "ToastModel" HAI
    public class ToastModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Type { get; set; } = "success"; // success, error, warning, info
        public DateTime Posted { get; set; } = DateTime.Now;
    }

    // 👇 YE MAIN SERVICE HAI
    public class ToastService
    {
        public event Action? OnChange;

        // Ab ye line error nahi degi kyunki ToastModel upar bana diya hai
        public List<ToastModel> Toasts { get; private set; } = new();

        public void ShowToast(string title, string message, string type = "success")
        {
            var toast = new ToastModel { Title = title, Message = message, Type = type };
            Toasts.Add(toast);
            OnChange?.Invoke(); // UI update trigger

            // Timer start
            StartTimer(toast);
        }

        private async void StartTimer(ToastModel toast)
        {
            await Task.Delay(4000); // 4 Seconds wait
            RemoveToast(toast);
        }

        public void RemoveToast(ToastModel toast)
        {
            if (Toasts.Contains(toast))
            {
                Toasts.Remove(toast);
                OnChange?.Invoke(); // UI update trigger
            }
        }
    }
}