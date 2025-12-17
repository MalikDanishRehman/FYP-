using System;
using System.Collections.Generic;
using System.Timers;

namespace AI_Driven_Water_Supply.Client.Services // Namespace check karlena
{
    public class ToastModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Type { get; set; } = "success"; // success, error, warning, info
        public DateTime Posted { get; set; } = DateTime.Now;
    }

    public class ToastService
    {
        public event Action? OnChange;
        public List<ToastModel> Toasts { get; private set; } = new();

        public void ShowToast(string title, string message, string type = "success")
        {
            var toast = new ToastModel { Title = title, Message = message, Type = type };
            Toasts.Add(toast);
            OnChange?.Invoke(); // UI ko batao ke naya toast aaya hai

            // 3 Second baad khud gayab ho jaye
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
                OnChange?.Invoke(); // UI update
            }
        }
    }
}