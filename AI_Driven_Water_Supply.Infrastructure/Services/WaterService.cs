using AI_Driven_Water_Supply.Application.Interfaces;

namespace AI_Driven_Water_Supply.Infrastructure.Services
{
    public class WaterService : IWaterService
    {
        public Task<string> GetWaterStatus()
        {
            return Task.FromResult("Water delivery on the way!");
        }
    }
}
