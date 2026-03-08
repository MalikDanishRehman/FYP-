using System;
using System.Collections.Generic;
using AI_Driven_Water_Supply.Application.DTOs;

namespace AI_Driven_Water_Supply.Application.Interfaces
{
    public interface IToastService
    {
        void ShowToast(string title, string message, string type = "success");
        void RemoveToast(ToastModel toast);
        List<ToastModel> Toasts { get; }
        event Action? OnChange;
    }
}
