using GPIMSWeb.Models;
using GPIMSWeb.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace GPIMSWeb.Services
{
    public class RealtimeDataService : IRealtimeDataService, IDisposable
    {
        private readonly IHubContext<RealtimeDataHub> _hubContext;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<RealtimeDataService> _logger;
        
        // 고성능 동시성 컬렉션 (메인 데이터 저장소)
        private readonly ConcurrentDictionary<string, Channel> _channelData = new();
        private readonly ConcurrentDictionary<string, CanLinData> _canLinData = new();
        private readonly ConcurrentDictionary<string, AuxData> _auxData = new();
        
        // 장비별 마지막 업데이트 시간 추적
        private readonly ConcurrentDictionary<int, DateTime> _lastUpdateTimes = new();
        
        // 성능 모니터링
        private readonly Timer _performanceMonitor;
        private readonly Timer _cleanupTimer;
        private long _totalUpdates = 0;
        private long _totalCacheHits = 0;
        private long _totalCacheMisses = 0;
        private DateTime _serviceStartTime;

        // 설정값
        private readonly TimeSpan _defaultCacheTtl = TimeSpan.FromSeconds(30);
        private readonly TimeSpan _equipmentOfflineThreshold = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _dataCleanupThreshold = TimeSpan.FromMinutes(10);

        public RealtimeDataService(
            IHubContext<RealtimeDataHub> hubContext, 
            IMemoryCache memoryCache, 
            ILogger<RealtimeDataService> logger)
        {
            _hubContext = hubContext;
            _memoryCache = memoryCache;
            _logger = logger;
            _serviceStartTime = DateTime.Now;
            
            // 5초마다 성능 모니터링
            _performanceMonitor = new Timer(MonitorPerformance, null, 
                TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            
            // 1분마다 데이터 정리
            _cleanupTimer = new Timer(CleanupOldData, null, 
                TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            
            _logger.LogInformation("High-performance RealtimeDataService initialized");
        }

        #region Channel Data Operations

        /// <summary>
        /// 채널 데이터 업데이트 (O(1) 성능)
        /// </summary>
        public void UpdateChannelData(int equipmentId, int channelNumber, Channel channelData)
        {
            try
            {
                var key = GetChannelKey(equipmentId, channelNumber);
                
                // 메모리에 저장 (O(1) 성능)
                _channelData.AddOrUpdate(key, channelData, (k, v) => channelData);
                
                // 이중 캐시 전략: MemoryCache에도 저장 (TTL 적용)
                _memoryCache.Set(key, channelData, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _defaultCacheTtl,
                    Priority = CacheItemPriority.High,
                    Size = 1,
                    SlidingExpiration = TimeSpan.FromSeconds(10)
                });
                
                // 장비 마지막 업데이트 시간 기록
                _lastUpdateTimes.AddOrUpdate(equipmentId, DateTime.Now, (k, v) => DateTime.Now);
                
                // 성능 카운터 증가
                Interlocked.Increment(ref _totalUpdates);

                _logger.LogDebug("Channel data updated - Equipment: {EquipmentId}, Channel: {ChannelNumber}, Status: {Status}",
                    equipmentId, channelNumber, channelData.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating channel data for Equipment {EquipmentId}, Channel {ChannelNumber}",
                    equipmentId, channelNumber);
            }
        }

        /// <summary>
        /// 단일 채널 데이터 조회
        /// </summary>
        public async Task<Channel> GetChannelDataAsync(int equipmentId, int channelNumber)
        {
            try
            {
                var key = GetChannelKey(equipmentId, channelNumber);
                
                // 1차: 메모리에서 확인 (가장 빠름)
                if (_channelData.TryGetValue(key, out var channel))
                {
                    Interlocked.Increment(ref _totalCacheHits);
                    return channel;
                }
                
                // 2차: MemoryCache에서 확인
                if (_memoryCache.TryGetValue(key, out Channel cachedChannel))
                {
                    // 메모리 딕셔너리에 다시 추가 (성능 최적화)
                    _channelData.TryAdd(key, cachedChannel);
                    Interlocked.Increment(ref _totalCacheHits);
                    return cachedChannel;
                }
                
                Interlocked.Increment(ref _totalCacheMisses);
                _logger.LogDebug("Channel data not found - Equipment: {EquipmentId}, Channel: {ChannelNumber}",
                    equipmentId, channelNumber);
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving channel data for Equipment {EquipmentId}, Channel {ChannelNumber}",
                    equipmentId, channelNumber);
                return null;
            }
        }

        /// <summary>
        /// 장비의 모든 채널 데이터 조회 (고성능)
        /// </summary>
        public async Task<List<Channel>> GetAllChannelDataAsync(int equipmentId)
        {
            try
            {
                var prefix = GetEquipmentChannelPrefix(equipmentId);
                
                // 병렬 처리로 성능 최적화
                var channels = _channelData
                    .Where(kvp => kvp.Key.StartsWith(prefix))
                    .AsParallel()
                    .Select(kvp => kvp.Value)
                    .OrderBy(c => c.ChannelNumber)
                    .ToList();
                
                _logger.LogDebug("Retrieved {Count} channels for Equipment {EquipmentId}", 
                    channels.Count, equipmentId);
                
                return channels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all channel data for Equipment {EquipmentId}", equipmentId);
                return new List<Channel>();
            }
        }

        /// <summary>
        /// 배치 채널 데이터 업데이트 (고성능)
        /// </summary>
        public void UpdateChannelDataBatch(int equipmentId, List<Channel> channels)
        {
            try
            {
                if (channels == null || !channels.Any())
                    return;

                // 병렬 처리로 배치 업데이트
                Parallel.ForEach(channels, channel =>
                {
                    var key = GetChannelKey(equipmentId, channel.ChannelNumber);
                    _channelData.AddOrUpdate(key, channel, (k, v) => channel);
                    
                    // 캐시에도 저장
                    _memoryCache.Set(key, channel, _defaultCacheTtl);
                });

                // 장비 업데이트 시간 기록
                _lastUpdateTimes.AddOrUpdate(equipmentId, DateTime.Now, (k, v) => DateTime.Now);
                
                Interlocked.Add(ref _totalUpdates, channels.Count);
                
                _logger.LogDebug("Batch updated {Count} channels for Equipment {EquipmentId}", 
                    channels.Count, equipmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch channel update for Equipment {EquipmentId}", equipmentId);
            }
        }

        #endregion

        #region CAN/LIN Data Operations

        /// <summary>
        /// CAN/LIN 데이터 업데이트
        /// </summary>
        public void UpdateCanLinData(int equipmentId, string name, CanLinData canLinData)
        {
            try
            {
                var key = GetCanLinKey(equipmentId, name);
                
                _canLinData.AddOrUpdate(key, canLinData, (k, v) => canLinData);
                _memoryCache.Set(key, canLinData, _defaultCacheTtl);
                
                _lastUpdateTimes.AddOrUpdate(equipmentId, DateTime.Now, (k, v) => DateTime.Now);
                Interlocked.Increment(ref _totalUpdates);
                
                _logger.LogDebug("CAN/LIN data updated - Equipment: {EquipmentId}, Name: {Name}, Value: {CurrentValue}",
                    equipmentId, name, canLinData.CurrentValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating CAN/LIN data for Equipment {EquipmentId}, Name {Name}",
                    equipmentId, name);
            }
        }

        /// <summary>
        /// CAN/LIN 데이터 조회
        /// </summary>
        public async Task<CanLinData> GetCanLinDataAsync(int equipmentId, string name)
        {
            try
            {
                var key = GetCanLinKey(equipmentId, name);
                
                if (_canLinData.TryGetValue(key, out var canLinData))
                {
                    Interlocked.Increment(ref _totalCacheHits);
                    return canLinData;
                }
                
                if (_memoryCache.TryGetValue(key, out CanLinData cachedCanLin))
                {
                    _canLinData.TryAdd(key, cachedCanLin);
                    Interlocked.Increment(ref _totalCacheHits);
                    return cachedCanLin;
                }
                
                Interlocked.Increment(ref _totalCacheMisses);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving CAN/LIN data for Equipment {EquipmentId}, Name {Name}",
                    equipmentId, name);
                return null;
            }
        }

        /// <summary>
        /// 장비의 모든 CAN/LIN 데이터 조회
        /// </summary>
        public async Task<List<CanLinData>> GetAllCanLinDataAsync(int equipmentId)
        {
            try
            {
                var prefix = GetEquipmentCanLinPrefix(equipmentId);
                
                var canLinDataList = _canLinData
                    .Where(kvp => kvp.Key.StartsWith(prefix))
                    .Select(kvp => kvp.Value)
                    .OrderBy(c => c.Name)
                    .ToList();
                
                return canLinDataList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all CAN/LIN data for Equipment {EquipmentId}", equipmentId);
                return new List<CanLinData>();
            }
        }

        #endregion

        #region AUX Data Operations

        /// <summary>
        /// AUX 데이터 업데이트
        /// </summary>
        public void UpdateAuxData(int equipmentId, string sensorId, AuxData auxData)
        {
            try
            {
                var key = GetAuxKey(equipmentId, sensorId);
                
                _auxData.AddOrUpdate(key, auxData, (k, v) => auxData);
                _memoryCache.Set(key, auxData, _defaultCacheTtl);
                
                _lastUpdateTimes.AddOrUpdate(equipmentId, DateTime.Now, (k, v) => DateTime.Now);
                Interlocked.Increment(ref _totalUpdates);
                
                _logger.LogDebug("AUX data updated - Equipment: {EquipmentId}, Sensor: {SensorId}, Value: {Value}",
                    equipmentId, sensorId, auxData.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating AUX data for Equipment {EquipmentId}, Sensor {SensorId}",
                    equipmentId, sensorId);
            }
        }

        /// <summary>
        /// AUX 데이터 조회
        /// </summary>
        public async Task<AuxData> GetAuxDataAsync(int equipmentId, string sensorId)
        {
            try
            {
                var key = GetAuxKey(equipmentId, sensorId);
                
                if (_auxData.TryGetValue(key, out var auxData))
                {
                    Interlocked.Increment(ref _totalCacheHits);
                    return auxData;
                }
                
                if (_memoryCache.TryGetValue(key, out AuxData cachedAux))
                {
                    _auxData.TryAdd(key, cachedAux);
                    Interlocked.Increment(ref _totalCacheHits);
                    return cachedAux;
                }
                
                Interlocked.Increment(ref _totalCacheMisses);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving AUX data for Equipment {EquipmentId}, Sensor {SensorId}",
                    equipmentId, sensorId);
                return null;
            }
        }

        /// <summary>
        /// 장비의 모든 AUX 데이터 조회
        /// </summary>
        public async Task<List<AuxData>> GetAllAuxDataAsync(int equipmentId)
        {
            try
            {
                var prefix = GetEquipmentAuxPrefix(equipmentId);
                
                var auxDataList = _auxData
                    .Where(kvp => kvp.Key.StartsWith(prefix))
                    .Select(kvp => kvp.Value)
                    .OrderBy(a => a.SensorId)
                    .ToList();
                
                return auxDataList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all AUX data for Equipment {EquipmentId}", equipmentId);
                return new List<AuxData>();
            }
        }

        #endregion

        #region Alarm Operations

        /// <summary>
        /// 알람 추가 (논블로킹 SignalR 전송)
        /// </summary>
        public void AddAlarm(int equipmentId, string message, AlarmLevel level)
        {
            try
            {
                var alarm = new Alarm
                {
                    EquipmentId = equipmentId,
                    Message = message,
                    Level = level,
                    CreatedAt = DateTime.Now,
                    IsCleared = false
                };

                // 논블로킹 방식으로 SignalR 전송 (성능 최적화)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _hubContext.Clients.All.SendAsync("NewAlarm", alarm);
                        _logger.LogInformation("Alarm sent via SignalR - Equipment: {EquipmentId}, Level: {Level}, Message: {Message}",
                            equipmentId, level, message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending alarm via SignalR - Equipment: {EquipmentId}", equipmentId);
                    }
                });
                
                Interlocked.Increment(ref _totalUpdates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding alarm for Equipment {EquipmentId}", equipmentId);
            }
        }

        #endregion

        #region Equipment Status Operations

        /// <summary>
        /// 장비 온라인 상태 확인
        /// </summary>
        public bool IsEquipmentOnline(int equipmentId)
        {
            if (_lastUpdateTimes.TryGetValue(equipmentId, out var lastUpdate))
            {
                return DateTime.Now - lastUpdate < _equipmentOfflineThreshold;
            }
            return false;
        }

        /// <summary>
        /// 온라인 장비 목록 조회
        /// </summary>
        public List<int> GetOnlineEquipmentIds()
        {
            var now = DateTime.Now;
            return _lastUpdateTimes
                .Where(kvp => now - kvp.Value < _equipmentOfflineThreshold)
                .Select(kvp => kvp.Key)
                .OrderBy(id => id)
                .ToList();
        }

        /// <summary>
        /// 장비별 데이터 통계
        /// </summary>
        public async Task<Dictionary<int, EquipmentDataStats>> GetEquipmentStatsAsync()
        {
            try
            {
                var stats = new Dictionary<int, EquipmentDataStats>();
                var onlineEquipment = GetOnlineEquipmentIds();

                await Task.Run(() =>
                {
                    Parallel.ForEach(onlineEquipment, equipmentId =>
                    {
                        var channelCount = _channelData.Keys.Count(k => k.StartsWith($"eq_{equipmentId}_ch_"));
                        var canLinCount = _canLinData.Keys.Count(k => k.StartsWith($"eq_{equipmentId}_canlin_"));
                        var auxCount = _auxData.Keys.Count(k => k.StartsWith($"eq_{equipmentId}_aux_"));
                        var lastUpdate = _lastUpdateTimes.TryGetValue(equipmentId, out var time) ? time : DateTime.MinValue;

                        lock (stats)
                        {
                            stats[equipmentId] = new EquipmentDataStats
                            {
                                EquipmentId = equipmentId,
                                ChannelCount = channelCount,
                                CanLinCount = canLinCount,
                                AuxCount = auxCount,
                                LastUpdateTime = lastUpdate,
                                IsOnline = IsEquipmentOnline(equipmentId)
                            };
                        }
                    });
                });

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting equipment stats");
                return new Dictionary<int, EquipmentDataStats>();
            }
        }

        #endregion

        #region Key Generation Helpers

        private static string GetChannelKey(int equipmentId, int channelNumber) 
            => $"eq_{equipmentId}_ch_{channelNumber}";

        private static string GetCanLinKey(int equipmentId, string name) 
            => $"eq_{equipmentId}_canlin_{name}";

        private static string GetAuxKey(int equipmentId, string sensorId) 
            => $"eq_{equipmentId}_aux_{sensorId}";

        private static string GetEquipmentChannelPrefix(int equipmentId) 
            => $"eq_{equipmentId}_ch_";

        private static string GetEquipmentCanLinPrefix(int equipmentId) 
            => $"eq_{equipmentId}_canlin_";

        private static string GetEquipmentAuxPrefix(int equipmentId) 
            => $"eq_{equipmentId}_aux_";

        #endregion

        #region Performance Monitoring & Cleanup

        /// <summary>
        /// 성능 모니터링 (5초마다 실행)
        /// </summary>
        private void MonitorPerformance(object state)
        {
            try
            {
                var now = DateTime.Now;
                var uptime = now - _serviceStartTime;
                var onlineEquipment = GetOnlineEquipmentIds();

                var totalChannels = _channelData.Count;
                var totalCanLin = _canLinData.Count;
                var totalAux = _auxData.Count;
                var totalCacheSize = totalChannels + totalCanLin + totalAux;

                var cacheHitRate = _totalCacheHits + _totalCacheMisses > 0 
                    ? (_totalCacheHits * 100.0) / (_totalCacheHits + _totalCacheMisses) 
                    : 0;

                var updatesPerSecond = uptime.TotalSeconds > 0 ? _totalUpdates / uptime.TotalSeconds : 0;

                _logger.LogInformation(
                    "RealtimeDataService Performance - " +
                    "Uptime: {Uptime}, Online Equipment: {OnlineCount}, " +
                    "Cache Size: {CacheSize} (Channels: {Channels}, CAN/LIN: {CanLin}, AUX: {Aux}), " +
                    "Cache Hit Rate: {CacheHitRate:F1}%, Updates/sec: {UpdatesPerSec:F1}, " +
                    "Memory Usage: {MemoryMB:F1} MB",
                    uptime.ToString(@"hh\:mm\:ss"), onlineEquipment.Count,
                    totalCacheSize, totalChannels, totalCanLin, totalAux,
                    cacheHitRate, updatesPerSecond,
                    GC.GetTotalMemory(false) / 1024.0 / 1024.0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in performance monitoring");
            }
        }

        /// <summary>
        /// 오래된 데이터 정리 (1분마다 실행)
        /// </summary>
        private void CleanupOldData(object state)
        {
            try
            {
                var now = DateTime.Now;
                var cutoffTime = now - _dataCleanupThreshold;
                var removedCount = 0;

                // 오프라인 장비 식별
                var offlineEquipment = _lastUpdateTimes
                    .Where(kvp => kvp.Value < cutoffTime)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var equipmentId in offlineEquipment)
                {
                    // 장비 업데이트 시간 제거
                    _lastUpdateTimes.TryRemove(equipmentId, out _);
                    
                    // 해당 장비의 모든 데이터 제거
                    var channelKeys = _channelData.Keys
                        .Where(k => k.StartsWith($"eq_{equipmentId}_"))
                        .ToList();
                    
                    var canLinKeys = _canLinData.Keys
                        .Where(k => k.StartsWith($"eq_{equipmentId}_"))
                        .ToList();
                    
                    var auxKeys = _auxData.Keys
                        .Where(k => k.StartsWith($"eq_{equipmentId}_"))
                        .ToList();

                    // 병렬로 제거 (성능 최적화)
                    Parallel.ForEach(channelKeys, key =>
                    {
                        _channelData.TryRemove(key, out _);
                        _memoryCache.Remove(key);
                        Interlocked.Increment(ref removedCount);
                    });

                    Parallel.ForEach(canLinKeys, key =>
                    {
                        _canLinData.TryRemove(key, out _);
                        _memoryCache.Remove(key);
                        Interlocked.Increment(ref removedCount);
                    });

                    Parallel.ForEach(auxKeys, key =>
                    {
                        _auxData.TryRemove(key, out _);
                        _memoryCache.Remove(key);
                        Interlocked.Increment(ref removedCount);
                    });
                }

                if (removedCount > 0)
                {
                    _logger.LogInformation("Cleanup completed - Removed {RemovedCount} data points from {OfflineCount} offline equipment",
                        removedCount, offlineEquipment.Count);
                }

                // 강제 가비지 컬렉션 (필요시)
                if (removedCount > 1000)
                {
                    GC.Collect(2, GCCollectionMode.Optimized);
                    _logger.LogDebug("Forced garbage collection after cleanup");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during data cleanup");
            }
        }

        #endregion

        #region Service Control

        /// <summary>
        /// 모든 데이터 클리어
        /// </summary>
        public void ClearAllData()
        {
            try
            {
                var totalCount = _channelData.Count + _canLinData.Count + _auxData.Count;
                
                _channelData.Clear();
                _canLinData.Clear();
                _auxData.Clear();
                _lastUpdateTimes.Clear();
                
                _logger.LogInformation("All realtime data cleared - {TotalCount} items removed", totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all data");
            }
        }

        /// <summary>
        /// 서비스 상태 정보
        /// </summary>
        public RealtimeServiceStatus GetServiceStatus()
        {
            var uptime = DateTime.Now - _serviceStartTime;
            var cacheHitRate = _totalCacheHits + _totalCacheMisses > 0 
                ? (_totalCacheHits * 100.0) / (_totalCacheHits + _totalCacheMisses) 
                : 0;

            return new RealtimeServiceStatus
            {
                Uptime = uptime,
                TotalUpdates = _totalUpdates,
                CacheHitRate = cacheHitRate,
                OnlineEquipmentCount = GetOnlineEquipmentIds().Count,
                TotalChannels = _channelData.Count,
                TotalCanLinData = _canLinData.Count,
                TotalAuxData = _auxData.Count,
                MemoryUsageMB = GC.GetTotalMemory(false) / 1024.0 / 1024.0
            };
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
            try
            {
                _performanceMonitor?.Dispose();
                _cleanupTimer?.Dispose();
                
                var totalDataPoints = _channelData.Count + _canLinData.Count + _auxData.Count;
                _logger.LogInformation("RealtimeDataService disposed - Total data points: {TotalDataPoints}, Total updates: {TotalUpdates}",
                    totalDataPoints, _totalUpdates);
                
                ClearAllData();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during RealtimeDataService disposal");
            }
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// 장비 데이터 통계
    /// </summary>
    public class EquipmentDataStats
    {
        public int EquipmentId { get; set; }
        public int ChannelCount { get; set; }
        public int CanLinCount { get; set; }
        public int AuxCount { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public bool IsOnline { get; set; }
    }

    /// <summary>
    /// 실시간 서비스 상태
    /// </summary>
    public class RealtimeServiceStatus
    {
        public TimeSpan Uptime { get; set; }
        public long TotalUpdates { get; set; }
        public double CacheHitRate { get; set; }
        public int OnlineEquipmentCount { get; set; }
        public int TotalChannels { get; set; }
        public int TotalCanLinData { get; set; }
        public int TotalAuxData { get; set; }
        public double MemoryUsageMB { get; set; }
    }

    #endregion
}