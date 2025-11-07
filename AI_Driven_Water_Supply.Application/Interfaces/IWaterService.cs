namespace AI_Driven_Water_Supply.Application.Interfaces
{
    public interface IWaterService
    {
        Task<string> GetWaterStatus();
    }
}
