using AI_Driven_Water_Supply.Application.Interfaces;

namespace AI_Driven_Water_Supply.Infrastructure.Services
{
    public class WaterService : IWaterService
    {
        public string GetStatus() => "Water system running ✅";
    }
}
