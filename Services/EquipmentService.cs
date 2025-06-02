using GPIMSWeb.Data;
using GPIMSWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace GPIMSWeb.Services
{
    public class EquipmentService : IEquipmentService
    {
        private readonly ApplicationDbContext _context;

        public EquipmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Equipment>> GetAllEquipmentAsync()
        {
            return await _context.Equipment
                .Include(e => e.Channels)
                .Include(e => e.CanLinData)
                .Include(e => e.AuxData)
                .Include(e => e.Alarms.Where(a => !a.IsCleared))
                .ToListAsync();
        }

        public async Task<Equipment> GetEquipmentByIdAsync(int id)
        {
            return await _context.Equipment
                .Include(e => e.Channels)
                .Include(e => e.CanLinData)
                .Include(e => e.AuxData)
                .Include(e => e.Alarms.Where(a => !a.IsCleared))
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<bool> UpdateEquipmentVersionAsync(int id, string version)
        {
            try
            {
                var equipment = await _context.Equipment.FindAsync(id);
                if (equipment == null) return false;

                equipment.Version = version;
                equipment.LastUpdateTime = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Alarm>> GetActiveAlarmsAsync(int? equipmentId = null)
        {
            var query = _context.Alarms.Where(a => !a.IsCleared);

            if (equipmentId.HasValue)
                query = query.Where(a => a.EquipmentId == equipmentId.Value);

            return await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> ClearAlarmAsync(int alarmId, string clearedBy)
        {
            try
            {
                var alarm = await _context.Alarms.FindAsync(alarmId);
                if (alarm == null) return false;

                alarm.IsCleared = true;
                alarm.ClearedAt = DateTime.Now;
                alarm.ClearedBy = clearedBy;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}