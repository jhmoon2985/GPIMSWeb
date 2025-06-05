using GPIMSWeb.Data;
using GPIMSWeb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GPIMSWeb.Services
{
    public class EquipmentService : IEquipmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<EquipmentService> _logger;
        
        // 캐시 키 상수
        private const string EQUIPMENT_LIST_CACHE_KEY = "equipment_list";
        private const string EQUIPMENT_DETAIL_CACHE_KEY = "equipment_detail_";
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromSeconds(30); // 30초 캐시

        public EquipmentService(ApplicationDbContext context, IMemoryCache cache, ILogger<EquipmentService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<Equipment>> GetAllEquipmentAsync()
        {
            try
            {
                // 캐시에서 먼저 확인
                if (_cache.TryGetValue(EQUIPMENT_LIST_CACHE_KEY, out List<Equipment>? cachedEquipment))
                {
                    return cachedEquipment!;
                }

                // 대시보드용 최적화된 쿼리 - 필수 데이터만 로드
                var equipment = await _context.Equipment
                    .Select(e => new Equipment
                    {
                        Id = e.Id,
                        Name = e.Name,
                        IsOnline = e.IsOnline,
                        LastUpdateTime = e.LastUpdateTime,
                        Version = e.Version,
                        Status = e.Status,
                        // 채널은 개수만 필요하므로 제한된 수만 로드
                        Channels = _context.Channels
                            .Where(c => c.EquipmentId == e.Id)
                            .Select(c => new Channel
                            {
                                Id = c.Id,
                                ChannelNumber = c.ChannelNumber,
                                Status = c.Status,
                                Mode = c.Mode,
                                Voltage = c.Voltage,
                                Current = c.Current,
                                Capacity = c.Capacity,
                                Power = c.Power,
                                Energy = c.Energy,
                                ScheduleName = c.ScheduleName,
                                LastUpdateTime = c.LastUpdateTime
                            })
                            .Take(10) // 최대 10개만
                            .ToList(),
                        // 활성 알람만 최대 3개
                        Alarms = _context.Alarms
                            .Where(a => a.EquipmentId == e.Id && !a.IsCleared)
                            .OrderByDescending(a => a.CreatedAt)
                            .Take(3)
                            .Select(a => new Alarm
                            {
                                Id = a.Id,
                                EquipmentId = a.EquipmentId,
                                Message = a.Message,
                                Level = a.Level,
                                CreatedAt = a.CreatedAt,
                                IsCleared = a.IsCleared,
                                ClearedAt = a.ClearedAt,
                                ClearedBy = a.ClearedBy
                            })
                            .ToList(),
                        // CAN/LIN과 AUX는 대시보드에서 필요하지 않으므로 빈 리스트
                        CanLinData = new List<CanLinData>(),
                        AuxData = new List<AuxData>()
                    })
                    .AsNoTracking() // 추적 비활성화로 성능 향상
                    .ToListAsync();

                // 캐시에 저장
                _cache.Set(EQUIPMENT_LIST_CACHE_KEY, equipment, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheExpiration,
                    Priority = CacheItemPriority.High,
                    Size = 1
                });

                _logger.LogDebug("Equipment list loaded from database and cached. Count: {Count}", equipment.Count);
                return equipment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving equipment list");
                return new List<Equipment>();
            }
        }

        public async Task<Equipment?> GetEquipmentByIdAsync(int id)
        {
            try
            {
                var cacheKey = EQUIPMENT_DETAIL_CACHE_KEY + id;

                // 캐시에서 먼저 확인
                if (_cache.TryGetValue(cacheKey, out Equipment? cachedEquipment))
                {
                    _logger.LogDebug("Equipment {EquipmentId} loaded from cache", id);
                    return cachedEquipment;
                }

                // 상세 정보는 필요할 때만 로드
                var equipment = await _context.Equipment
                    .Where(e => e.Id == id)
                    .Select(e => new Equipment
                    {
                        Id = e.Id,
                        Name = e.Name,
                        IsOnline = e.IsOnline,
                        LastUpdateTime = e.LastUpdateTime,
                        Version = e.Version,
                        Status = e.Status,
                        Channels = _context.Channels
                            .Where(c => c.EquipmentId == e.Id)
                            .OrderBy(c => c.ChannelNumber)
                            .ToList(),
                        CanLinData = _context.CanLinData
                            .Where(c => c.EquipmentId == e.Id)
                            .OrderBy(c => c.Name)
                            .ToList(),
                        AuxData = _context.AuxData
                            .Where(a => a.EquipmentId == e.Id)
                            .OrderBy(a => a.SensorId)
                            .ToList(),
                        Alarms = _context.Alarms
                            .Where(a => a.EquipmentId == e.Id && !a.IsCleared)
                            .OrderByDescending(a => a.CreatedAt)
                            .ToList()
                    })
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (equipment != null)
                {
                    // 짧은 캐시 시간 (상세 정보는 더 자주 업데이트)
                    _cache.Set(cacheKey, equipment, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10),
                        Priority = CacheItemPriority.Normal,
                        Size = 1
                    });

                    _logger.LogDebug("Equipment {EquipmentId} loaded from database and cached", id);
                }
                else
                {
                    _logger.LogWarning("Equipment {EquipmentId} not found", id);
                }

                return equipment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving equipment {EquipmentId}", id);
                return null;
            }
        }

        public async Task<bool> UpdateEquipmentVersionAsync(int id, string version)
        {
            try
            {
                var equipment = await _context.Equipment.FindAsync(id);
                if (equipment == null)
                {
                    _logger.LogWarning("Equipment {EquipmentId} not found for version update", id);
                    return false;
                }

                equipment.Version = version;
                equipment.LastUpdateTime = DateTime.Now;
                equipment.Status = EquipmentStatus.Updating;

                await _context.SaveChangesAsync();

                // 캐시 무효화
                InvalidateCache(id);

                _logger.LogInformation("Equipment {EquipmentId} version updated to {Version}", id, version);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating equipment version for {EquipmentId}", id);
                return false;
            }
        }

        public async Task<List<Alarm>> GetActiveAlarmsAsync(int? equipmentId = null)
        {
            try
            {
                var cacheKey = $"active_alarms_{equipmentId ?? 0}";

                if (_cache.TryGetValue(cacheKey, out List<Alarm>? cachedAlarms))
                {
                    return cachedAlarms!;
                }

                var query = _context.Alarms
                    .Where(a => !a.IsCleared)
                    .AsNoTracking();

                if (equipmentId.HasValue)
                    query = query.Where(a => a.EquipmentId == equipmentId.Value);

                var alarms = await query
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(50) // 최대 50개로 제한
                    .ToListAsync();

                _cache.Set(cacheKey, alarms, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(15),
                    Priority = CacheItemPriority.Normal,
                    Size = 1
                });

                _logger.LogDebug("Active alarms loaded from database. Count: {Count}", alarms.Count);
                return alarms;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active alarms for equipment {EquipmentId}", equipmentId);
                return new List<Alarm>();
            }
        }

        public async Task<bool> ClearAlarmAsync(int alarmId, string clearedBy)
        {
            try
            {
                var alarm = await _context.Alarms.FindAsync(alarmId);
                if (alarm == null)
                {
                    _logger.LogWarning("Alarm {AlarmId} not found for clearing", alarmId);
                    return false;
                }

                alarm.IsCleared = true;
                alarm.ClearedAt = DateTime.Now;
                alarm.ClearedBy = clearedBy;

                await _context.SaveChangesAsync();

                // 알람 관련 캐시 무효화
                InvalidateAlarmCache(alarm.EquipmentId);

                _logger.LogInformation("Alarm {AlarmId} cleared by {ClearedBy}", alarmId, clearedBy);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing alarm {AlarmId}", alarmId);
                return false;
            }
        }

        // 추가 메서드들

        public async Task<bool> UpdateEquipmentStatusAsync(int equipmentId, EquipmentStatus status)
        {
            try
            {
                var equipment = await _context.Equipment.FindAsync(equipmentId);
                if (equipment == null)
                {
                    _logger.LogWarning("Equipment {EquipmentId} not found for status update", equipmentId);
                    return false;
                }

                equipment.Status = status;
                equipment.LastUpdateTime = DateTime.Now;

                if (status == EquipmentStatus.Running || status == EquipmentStatus.Idle)
                {
                    equipment.IsOnline = true;
                }
                else if (status == EquipmentStatus.Error)
                {
                    equipment.IsOnline = false;
                }

                await _context.SaveChangesAsync();

                // 캐시 무효화
                InvalidateCache(equipmentId);

                _logger.LogDebug("Equipment {EquipmentId} status updated to {Status}", equipmentId, status);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating equipment status for {EquipmentId}", equipmentId);
                return false;
            }
        }

        public async Task<bool> SetEquipmentOnlineStatusAsync(int equipmentId, bool isOnline)
        {
            try
            {
                var equipment = await _context.Equipment.FindAsync(equipmentId);
                if (equipment == null)
                {
                    _logger.LogWarning("Equipment {EquipmentId} not found for online status update", equipmentId);
                    return false;
                }

                equipment.IsOnline = isOnline;
                equipment.LastUpdateTime = DateTime.Now;

                if (!isOnline)
                {
                    equipment.Status = EquipmentStatus.Error;
                }

                await _context.SaveChangesAsync();

                // 캐시 무효화
                InvalidateCache(equipmentId);

                _logger.LogDebug("Equipment {EquipmentId} online status updated to {IsOnline}", equipmentId, isOnline);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating equipment online status for {EquipmentId}", equipmentId);
                return false;
            }
        }

        public async Task<int> GetActiveChannelCountAsync(int equipmentId)
        {
            try
            {
                var cacheKey = $"active_channel_count_{equipmentId}";

                if (_cache.TryGetValue(cacheKey, out int cachedCount))
                {
                    return cachedCount;
                }

                var count = await _context.Channels
                    .Where(c => c.EquipmentId == equipmentId && c.Status == ChannelStatus.Active)
                    .CountAsync();

                _cache.Set(cacheKey, count, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10),
                    Priority = CacheItemPriority.Low,
                    Size = 1
                });

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active channel count for equipment {EquipmentId}", equipmentId);
                return 0;
            }
        }

        public async Task<List<Equipment>> GetOnlineEquipmentAsync()
        {
            try
            {
                var cacheKey = "online_equipment_list";

                if (_cache.TryGetValue(cacheKey, out List<Equipment>? cachedEquipment))
                {
                    return cachedEquipment!;
                }

                var equipment = await _context.Equipment
                    .Where(e => e.IsOnline)
                    .Select(e => new Equipment
                    {
                        Id = e.Id,
                        Name = e.Name,
                        IsOnline = e.IsOnline,
                        LastUpdateTime = e.LastUpdateTime,
                        Version = e.Version,
                        Status = e.Status
                    })
                    .AsNoTracking()
                    .ToListAsync();

                _cache.Set(cacheKey, equipment, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20),
                    Priority = CacheItemPriority.Normal,
                    Size = 1
                });

                return equipment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving online equipment");
                return new List<Equipment>();
            }
        }

        // 캐시 관리 메서드들
        private void InvalidateCache(int? equipmentId = null)
        {
            try
            {
                // 전체 장비 목록 캐시 무효화
                _cache.Remove(EQUIPMENT_LIST_CACHE_KEY);
                _cache.Remove("online_equipment_list");

                // 특정 장비 관련 캐시 무효화
                if (equipmentId.HasValue)
                {
                    _cache.Remove(EQUIPMENT_DETAIL_CACHE_KEY + equipmentId.Value);
                    _cache.Remove($"active_channel_count_{equipmentId.Value}");
                }

                _logger.LogDebug("Cache invalidated for equipment {EquipmentId}", equipmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache");
            }
        }

        private void InvalidateAlarmCache(int? equipmentId = null)
        {
            try
            {
                // 알람 관련 캐시 무효화
                _cache.Remove("active_alarms_0"); // 전체 알람

                if (equipmentId.HasValue)
                {
                    _cache.Remove($"active_alarms_{equipmentId.Value}");
                }
                else
                {
                    // 모든 장비별 알람 캐시 무효화
                    for (int i = 1; i <= 32; i++) // 최대 32개 장비
                    {
                        _cache.Remove($"active_alarms_{i}");
                    }
                }

                // 장비 목록도 무효화 (알람 수가 변경됨)
                InvalidateCache(equipmentId);

                _logger.LogDebug("Alarm cache invalidated for equipment {EquipmentId}", equipmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating alarm cache");
            }
        }

        public void ClearAllCache()
        {
            try
            {
                _cache.Remove(EQUIPMENT_LIST_CACHE_KEY);
                _cache.Remove("online_equipment_list");

                // 장비별 상세 캐시 무효화
                for (int i = 1; i <= 32; i++)
                {
                    _cache.Remove(EQUIPMENT_DETAIL_CACHE_KEY + i);
                    _cache.Remove($"active_alarms_{i}");
                    _cache.Remove($"active_channel_count_{i}");
                }

                _cache.Remove("active_alarms_0");

                _logger.LogInformation("All equipment service cache cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all cache");
            }
        }
    }
}