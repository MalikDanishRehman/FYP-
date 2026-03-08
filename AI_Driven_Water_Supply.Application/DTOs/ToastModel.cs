using System;

namespace AI_Driven_Water_Supply.Application.DTOs
{
    public class ToastModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Type { get; set; } = "success";
        public DateTime Posted { get; set; } = DateTime.Now;
    }
}
