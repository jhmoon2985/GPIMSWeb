using GPIMSWeb.Models;

namespace GPIMSWeb.Services
{
    public interface IEquipmentService
    {
        Task<List<Equipment>> GetAllEquipmentAsync();
        Task<Equipment> GetEquipmentByIdAsync(int id);
        Task<bool> UpdateEquipmentVersionAsync(int id, string version);
        Task<List<Alarm>> GetActiveAlarmsAsync(int? equipmentId = null);
        Task<bool> ClearAlarmAsync(int alarmId, string clearedBy);
    }
}