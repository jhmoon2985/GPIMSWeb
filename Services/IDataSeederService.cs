using GPIMSWeb.Models;

namespace GPIMSWeb.Services
{
    public interface IDataSeederService
    {
        Task SeedChannelDataAsync();
    }
}