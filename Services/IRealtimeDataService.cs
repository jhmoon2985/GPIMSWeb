using GPIMSWeb.Models;

namespace GPIMSWeb.Services
{
    public interface IRealtimeDataService
    {
        void UpdateChannelData(int equipmentId, int channelNumber, Channel channelData);
        void UpdateCanLinData(int equipmentId, string name, CanLinData canLinData);
        void UpdateAuxData(int equipmentId, string sensorId, AuxData auxData);
        void AddAlarm(int equipmentId, string message, AlarmLevel level);
        Task<Channel> GetChannelDataAsync(int equipmentId, int channelNumber);
        Task<List<Channel>> GetAllChannelDataAsync(int equipmentId);
    }
}