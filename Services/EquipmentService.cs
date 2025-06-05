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
            // 기존 복잡한 쿼리 대신 간단한 쿼리 사용
            return await _context.Equipment
                .Include(e => e.Channels.Take(10)) // 채널은 최대 10개만
                .Include(e => e.Alarms.Where(a => !a.IsCleared).Take(5)) // 활성 알람 최대 5개만
                // CanLinData와 AuxData는 대시보드에서 불필요하므로 제거
                // .Include(e => e.CanLinData)
                // .Include(e => e.AuxData)
                .AsNoTracking() // 추적 비활성화로 성능 향상
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