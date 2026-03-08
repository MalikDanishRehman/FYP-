using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AI_Driven_Water_Supply.Application.DTOs;
using AI_Driven_Water_Supply.Application.Interfaces;

namespace AI_Driven_Water_Supply.Infrastructure.Services
{
    public class ToastService : IToastService
    {
        public event Action? OnChange;

        public List<ToastModel> Toasts { get; private set; } = new();

        public void ShowToast(string title, string message, string type = "success")
        {
            var toast = new ToastModel { Title = title, Message = message, Type = type };
            Toasts.Add(toast);
            OnChange?.Invoke();

            StartTimer(toast);
        }

        private async void StartTimer(ToastModel toast)
        {
            await Task.Delay(4000);
            RemoveToast(toast);
        }

        public void RemoveToast(ToastModel toast)
        {
            if (Toasts.Contains(toast))
            {
                Toasts.Remove(toast);
                OnChange?.Invoke();
            }
        }
    }
}
