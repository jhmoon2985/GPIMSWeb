using GPIMSWeb.Data;
using GPIMSWeb.Models;
using GPIMSWeb.Services;
using GPIMSWeb.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace GPIMSWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientDataController : ControllerBase, IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IRealtimeDataService _realtimeDataService;
        private readonly IHubContext<RealtimeDataHub> _hubContext;
        private readonly ILogger<ClientDataController> _logger;
        
        // 고성능 배치 처리를 위한 큐
        private readonly ConcurrentQueue<ChannelDataRequest> _channelDataQueue = new();
        private readonly Timer _batchProcessTimer;
        private readonly SemaphoreSlim _batchSemaphore = new(1, 1);

        // 성능 카운터
        private long _totalRequestsReceived = 0;
        private long _totalRequestsProcessed = 0;
        private DateTime _lastStatsReport = DateTime.Now;

        public ClientDataController(
            ApplicationDbContext context,
            IRealtimeDataService realtimeDataService, 
            IHubContext<RealtimeDataHub> hubContext,
            ILogger<ClientDataController> logger)
        {
            _context = context;
            _realtimeDataService = realtimeDataService;
            _hubContext = hubContext;
            _logger = logger;
            
            // 200ms마다 배치 처리 (100ms보다 여유있게 설정)
            _batchProcessTimer = new Timer(ProcessBatchedChannelData, null, 200, 200);
            
            _logger.LogInformation("High-performance ClientDataController initialized with batch processing");
        }

        #region Single Channel Operations (기존 호환성 유지)

        /// <summary>
        /// 단일 채널 데이터 업데이트 (기존 호환성용)
        /// </summary>
        [HttpPost("channel")]
        public async Task<IActionResult> UpdateChannelData([FromBody] ChannelDataRequest request)
        {
            try
            {
                Interlocked.Increment(ref _totalRequestsReceived);

                // 입력 검증
                if (request == null)
                {
                    return BadRequest(new { success = false, message = "Request data is null" });
                }

                if (request.EquipmentId <= 0 || request.ChannelNumber <= 0)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Invalid EquipmentId or ChannelNumber" 
                    });
                }

                // 빠른 응답을 위해 큐에 추가하고 즉시 반환
                _channelDataQueue.Enqueue(request);

                // 실시간 서비스에 즉시 업데이트 (UI 반응성을 위해)
                var channel = CreateChannelFromRequest(request);
                _realtimeDataService.UpdateChannelData(request.EquipmentId, request.ChannelNumber, channel);

                // SignalR로 즉시 전송 (실시간 UI 업데이트)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _hubContext.Clients.All.SendAsync("UpdateChannelData", 
                            request.EquipmentId, request.ChannelNumber, channel);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending SignalR update for single channel");
                    }
                });

                _logger.LogDebug("Channel data queued - Equipment: {EquipmentId}, Channel: {ChannelNumber}", 
                    request.EquipmentId, request.ChannelNumber);

                return Ok(new { 
                    success = true, 
                    message = "Channel data queued for processing",
                    queued = true,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateChannelData for Equipment {EquipmentId}, Channel {ChannelNumber}",
                    request?.EquipmentId, request?.ChannelNumber);
                
                return StatusCode(500, new { 
                    success = false, 
                    message = "Internal server error",
                    error = ex.Message 
                });
            }
        }

        #endregion

        #region Batch Operations (고성능 처리)

        /// <summary>
        /// 배치 채널 데이터 업데이트 (고성능용)
        /// </summary>
        [HttpPost("channels/batch")]
        public async Task<IActionResult> UpdateChannelDataBatch([FromBody] List<ChannelDataRequest> requests)
        {
            try
            {
                if (requests == null || !requests.Any())
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "No channel data provided" 
                    });
                }

                // 최대 배치 크기 제한 (메모리 보호)
                if (requests.Count > 1000)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Batch size too large. Maximum 1000 channels per request." 
                    });
                }

                var validRequests = requests.Where(r => r != null && 
                    r.EquipmentId > 0 && r.ChannelNumber > 0).ToList();

                if (validRequests.Count == 0)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "No valid channel data found" 
                    });
                }

                Interlocked.Add(ref _totalRequestsReceived, validRequests.Count);

                // 큐에 배치 추가
                foreach (var request in validRequests)
                {
                    _channelDataQueue.Enqueue(request);
                }

                // 실시간 서비스 업데이트
                foreach (var request in validRequests)
                {
                    var channel = CreateChannelFromRequest(request);
                    _realtimeDataService.UpdateChannelData(request.EquipmentId, request.ChannelNumber, channel);
                }

                // SignalR 배치 전송 (논블로킹)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var groupedByEquipment = validRequests.GroupBy(r => r.EquipmentId);
                        foreach (var group in groupedByEquipment)
                        {
                            await _hubContext.Clients.All.SendAsync("UpdateChannelDataBatch", 
                                group.Key, group.ToList());
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending SignalR batch update");
                    }
                });

                _logger.LogDebug("Channel batch data queued - {Count} channels from {EquipmentCount} equipment", 
                    validRequests.Count, validRequests.Select(r => r.EquipmentId).Distinct().Count());

                return Ok(new { 
                    success = true, 
                    message = "Channel batch data queued for processing",
                    queued = validRequests.Count,
                    invalid = requests.Count - validRequests.Count,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateChannelDataBatch");
                return StatusCode(500, new { 
                    success = false, 
                    message = "Internal server error",
                    error = ex.Message 
                });
            }
        }

        #endregion

        #region CAN/LIN Data Operations

        /// <summary>
        /// CAN/LIN 데이터 업데이트
        /// </summary>
        [HttpPost("canlin")]
        public async Task<IActionResult> UpdateCanLinData([FromBody] CanLinDataRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Name))
                {
                    return BadRequest(new { success = false, message = "Invalid CAN/LIN data" });
                }

                // 데이터베이스에서 기존 CAN/LIN 데이터 찾기 또는 생성
                var existingCanLin = await _context.CanLinData
                    .FirstOrDefaultAsync(c => c.EquipmentId == request.EquipmentId && c.Name == request.Name);

                if (existingCanLin != null)
                {
                    // 기존 데이터 업데이트
                    existingCanLin.MinValue = request.MinValue;
                    existingCanLin.MaxValue = request.MaxValue;
                    existingCanLin.CurrentValue = request.CurrentValue;
                    existingCanLin.LastUpdateTime = DateTime.Now;

                    _context.CanLinData.Update(existingCanLin);
                }
                else
                {
                    // 새 데이터 생성
                    var newCanLin = new CanLinData
                    {
                        EquipmentId = request.EquipmentId,
                        Name = request.Name,
                        MinValue = request.MinValue,
                        MaxValue = request.MaxValue,
                        CurrentValue = request.CurrentValue,
                        LastUpdateTime = DateTime.Now
                    };

                    _context.CanLinData.Add(newCanLin);
                    existingCanLin = newCanLin;
                }

                await _context.SaveChangesAsync();

                // 실시간 서비스에도 업데이트
                _realtimeDataService.UpdateCanLinData(request.EquipmentId, request.Name, existingCanLin);

                // SignalR로 실시간 업데이트 전송
                await _hubContext.Clients.All.SendAsync("UpdateCanLinData", 
                    request.EquipmentId, request.Name, existingCanLin);

                _logger.LogDebug("CAN/LIN data updated - Equipment: {EquipmentId}, Name: {Name}, Value: {CurrentValue}",
                    request.EquipmentId, request.Name, request.CurrentValue);

                return Ok(new { 
                    success = true, 
                    message = "CAN/LIN data updated successfully",
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating CAN/LIN data for Equipment {EquipmentId}, Name {Name}",
                    request?.EquipmentId, request?.Name);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Internal server error" 
                });
            }
        }

        #endregion

        #region AUX Data Operations

        /// <summary>
        /// AUX 센서 데이터 업데이트
        /// </summary>
        [HttpPost("aux")]
        public async Task<IActionResult> UpdateAuxData([FromBody] AuxDataRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.SensorId))
                {
                    return BadRequest(new { success = false, message = "Invalid AUX data" });
                }

                // 데이터베이스에서 기존 AUX 데이터 찾기 또는 생성
                var existingAux = await _context.AuxData
                    .FirstOrDefaultAsync(a => a.EquipmentId == request.EquipmentId && a.SensorId == request.SensorId);

                if (existingAux != null)
                {
                    // 기존 데이터 업데이트
                    existingAux.Name = request.Name;
                    existingAux.Type = (AuxDataType)request.Type;
                    existingAux.Value = request.Value;
                    existingAux.LastUpdateTime = DateTime.Now;

                    _context.AuxData.Update(existingAux);
                }
                else
                {
                    // 새 데이터 생성
                    var newAux = new AuxData
                    {
                        EquipmentId = request.EquipmentId,
                        SensorId = request.SensorId,
                        Name = request.Name,
                        Type = (AuxDataType)request.Type,
                        Value = request.Value,
                        LastUpdateTime = DateTime.Now
                    };

                    _context.AuxData.Add(newAux);
                    existingAux = newAux;
                }

                await _context.SaveChangesAsync();

                // 실시간 서비스에도 업데이트
                _realtimeDataService.UpdateAuxData(request.EquipmentId, request.SensorId, existingAux);

                // SignalR로 실시간 업데이트 전송
                await _hubContext.Clients.All.SendAsync("UpdateAuxData", 
                    request.EquipmentId, request.SensorId, existingAux);

                _logger.LogDebug("AUX data updated - Equipment: {EquipmentId}, Sensor: {SensorId}, Value: {Value}",
                    request.EquipmentId, request.SensorId, request.Value);

                return Ok(new { 
                    success = true, 
                    message = "AUX data updated successfully",
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating AUX data for Equipment {EquipmentId}, Sensor {SensorId}",
                    request?.EquipmentId, request?.SensorId);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Internal server error" 
                });
            }
        }

        #endregion

        #region Alarm Operations

        /// <summary>
        /// 알람 추가
        /// </summary>
        [HttpPost("alarm")]
        public async Task<IActionResult> AddAlarm([FromBody] AlarmRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Message))
                {
                    return BadRequest(new { success = false, message = "Invalid alarm data" });
                }

                var alarm = new Alarm
                {
                    EquipmentId = request.EquipmentId,
                    Message = request.Message,
                    Level = (AlarmLevel)request.Level,
                    CreatedAt = DateTime.Now,
                    IsCleared = false,
                    ClearedBy = ""
                };

                _context.Alarms.Add(alarm);
                await _context.SaveChangesAsync();

                // 실시간 서비스에도 추가
                _realtimeDataService.AddAlarm(request.EquipmentId, request.Message, (AlarmLevel)request.Level);

                // SignalR로 실시간 알람 전송
                await _hubContext.Clients.All.SendAsync("NewAlarm", alarm);

                _logger.LogInformation("Alarm added - Equipment: {EquipmentId}, Level: {Level}, Message: {Message}",
                    request.EquipmentId, request.Level, request.Message);

                return Ok(new { 
                    success = true, 
                    message = "Alarm added successfully",
                    alarmId = alarm.Id,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding alarm for Equipment {EquipmentId}",
                    request?.EquipmentId);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Internal server error" 
                });
            }
        }

        #endregion

        #region Test & Monitoring

        /// <summary>
        /// 연결 테스트용 엔드포인트
        /// </summary>
        [HttpGet("test")]
        public IActionResult TestConnection()
        {
            var stats = new
            {
                success = true,
                message = "Connection successful",
                serverTime = DateTime.Now,
                version = "2.0.0-optimized",
                performance = new
                {
                    totalRequestsReceived = _totalRequestsReceived,
                    totalRequestsProcessed = _totalRequestsProcessed,
                    queueSize = _channelDataQueue.Count,
                    uptime = DateTime.Now - _lastStatsReport
                }
            };

            return Ok(stats);
        }

        /// <summary>
        /// 성능 통계 조회
        /// </summary>
        [HttpGet("stats")]
        public IActionResult GetStats()
        {
            var uptime = DateTime.Now - _lastStatsReport;
            var requestsPerSecond = uptime.TotalSeconds > 0 ? _totalRequestsReceived / uptime.TotalSeconds : 0;
            var processedPerSecond = uptime.TotalSeconds > 0 ? _totalRequestsProcessed / uptime.TotalSeconds : 0;

            return Ok(new
            {
                performance = new
                {
                    totalRequestsReceived = _totalRequestsReceived,
                    totalRequestsProcessed = _totalRequestsProcessed,
                    pendingInQueue = _channelDataQueue.Count,
                    requestsPerSecond = Math.Round(requestsPerSecond, 2),
                    processedPerSecond = Math.Round(processedPerSecond, 2),
                    uptime = uptime.ToString(@"hh\:mm\:ss")
                },
                system = new
                {
                    serverTime = DateTime.Now,
                    memoryUsage = GC.GetTotalMemory(false),
                    gcCollections = new
                    {
                        gen0 = GC.CollectionCount(0),
                        gen1 = GC.CollectionCount(1),
                        gen2 = GC.CollectionCount(2)
                    }
                }
            });
        }

        #endregion

        #region Batch Processing Core

        /// <summary>
        /// 배치 처리 메인 메서드
        /// </summary>
        private async void ProcessBatchedChannelData(object state)
        {
            if (!await _batchSemaphore.WaitAsync(50)) // 50ms 타임아웃
                return;

            try
            {
                var batch = new List<ChannelDataRequest>();
                
                // 큐에서 최대 1000개까지 처리 (메모리 보호)
                for (int i = 0; i < 1000 && _channelDataQueue.TryDequeue(out var request); i++)
                {
                    batch.Add(request);
                }

                if (batch.Count == 0) return;

                await ProcessChannelBatch(batch);
                
                Interlocked.Add(ref _totalRequestsProcessed, batch.Count);

                // 성능 리포트 (10초마다)
                if (DateTime.Now - _lastStatsReport > TimeSpan.FromSeconds(10))
                {
                    var uptime = DateTime.Now - _lastStatsReport;
                    var rps = _totalRequestsReceived / uptime.TotalSeconds;
                    var pps = _totalRequestsProcessed / uptime.TotalSeconds;
                    
                    _logger.LogInformation("Performance: {RPS:F1} req/sec, {PPS:F1} processed/sec, Queue: {Queue}",
                        rps, pps, _channelDataQueue.Count);
                    
                    _lastStatsReport = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batched channel data");
            }
            finally
            {
                _batchSemaphore.Release();
            }
        }

        /// <summary>
        /// 채널 배치 처리
        /// </summary>
        private async Task ProcessChannelBatch(List<ChannelDataRequest> batch)
        {
            try
            {
                // 장비별로 그룹화하여 처리
                var groupedByEquipment = batch.GroupBy(r => r.EquipmentId).ToList();

                foreach (var equipmentGroup in groupedByEquipment)
                {
                    var equipmentId = equipmentGroup.Key;
                    var channelUpdates = equipmentGroup.ToList();

                    // 데이터베이스 업데이트 (배치로)
                    await UpdateChannelsInDatabase(equipmentId, channelUpdates);
                }

                _logger.LogDebug("Processed {Count} channel updates in batch for {EquipmentCount} equipment", 
                    batch.Count, groupedByEquipment.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessChannelBatch");
                throw;
            }
        }

        /// <summary>
        /// 데이터베이스 배치 업데이트
        /// </summary>
        private async Task UpdateChannelsInDatabase(int equipmentId, List<ChannelDataRequest> channelUpdates)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 기존 채널들을 한 번에 조회
                var channelNumbers = channelUpdates.Select(r => r.ChannelNumber).ToList();
                var existingChannels = await _context.Channels
                    .Where(c => c.EquipmentId == equipmentId && channelNumbers.Contains(c.ChannelNumber))
                    .ToDictionaryAsync(c => c.ChannelNumber);

                var channelsToAdd = new List<Channel>();
                var channelsToUpdate = new List<Channel>();

                foreach (var request in channelUpdates)
                {
                    if (existingChannels.TryGetValue(request.ChannelNumber, out var existingChannel))
                    {
                        // 기존 채널 업데이트
                        UpdateChannelFromRequest(existingChannel, request);
                        channelsToUpdate.Add(existingChannel);
                    }
                    else
                    {
                        // 새 채널 생성
                        var newChannel = CreateChannelFromRequest(request);
                        channelsToAdd.Add(newChannel);
                    }
                }

                // 배치 추가/업데이트
                if (channelsToAdd.Count > 0)
                {
                    _context.Channels.AddRange(channelsToAdd);
                }

                if (channelsToUpdate.Count > 0)
                {
                    _context.Channels.UpdateRange(channelsToUpdate);
                }

                // 장비 상태 업데이트
                var equipment = await _context.Equipment.FindAsync(equipmentId);
                if (equipment != null)
                {
                    equipment.LastUpdateTime = DateTime.Now;
                    equipment.IsOnline = true;
                    var hasActiveChannels = channelUpdates.Any(r => r.Status == 1); // Active = 1
                    equipment.Status = hasActiveChannels ? EquipmentStatus.Running : EquipmentStatus.Idle;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogDebug("Database batch update completed - Equipment: {EquipmentId}, Channels: {Count}",
                    equipmentId, channelUpdates.Count);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating channels in database for equipment {EquipmentId}", equipmentId);
                throw;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Request에서 Channel 엔티티 생성
        /// </summary>
        private Channel CreateChannelFromRequest(ChannelDataRequest request)
        {
            return new Channel
            {
                EquipmentId = request.EquipmentId,
                ChannelNumber = request.ChannelNumber,
                Status = (ChannelStatus)request.Status,
                Mode = (ChannelMode)request.Mode,
                Voltage = request.Voltage,
                Current = request.Current,
                Capacity = request.Capacity,
                Power = request.Power,
                Energy = request.Energy,
                ScheduleName = request.ScheduleName ?? "",
                LastUpdateTime = DateTime.Now
            };
        }

        /// <summary>
        /// 기존 Channel 엔티티 업데이트
        /// </summary>
        private void UpdateChannelFromRequest(Channel channel, ChannelDataRequest request)
        {
            channel.Status = (ChannelStatus)request.Status;
            channel.Mode = (ChannelMode)request.Mode;
            channel.Voltage = request.Voltage;
            channel.Current = request.Current;
            channel.Capacity = request.Capacity;
            channel.Power = request.Power;
            channel.Energy = request.Energy;
            channel.ScheduleName = request.ScheduleName ?? "";
            channel.LastUpdateTime = DateTime.Now;
        }

        #endregion

        #region Disposal

        private bool _disposed = false;

        /// <summary>
        /// 컨트롤러 리소스 정리
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 리소스 정리 구현
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        _batchProcessTimer?.Dispose();
                        _batchSemaphore?.Dispose();
                        
                        // 남은 큐 데이터 정리
                        var remainingCount = 0;
                        while (_channelDataQueue.TryDequeue(out _)) 
                        {
                            remainingCount++;
                        }
                        
                        _logger.LogInformation("ClientDataController disposed - Processed {TotalProcessed} requests, Cleared {RemainingCount} queued items", 
                            _totalRequestsProcessed, remainingCount);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during ClientDataController disposal");
                    }
                }
                _disposed = true;
            }
        }

        #endregion
    }

    #region Request DTOs

    /// <summary>
    /// 채널 데이터 요청 DTO
    /// </summary>
    public class ChannelDataRequest
    {
        public int EquipmentId { get; set; }
        public int ChannelNumber { get; set; }
        public int Status { get; set; }
        public int Mode { get; set; }
        public double Voltage { get; set; }
        public double Current { get; set; }
        public double Capacity { get; set; }
        public double Power { get; set; }
        public double Energy { get; set; }
        public string? ScheduleName { get; set; }
    }

    /// <summary>
    /// CAN/LIN 데이터 요청 DTO
    /// </summary>
    public class CanLinDataRequest
    {
        public int EquipmentId { get; set; }
        public string Name { get; set; } = "";
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public double CurrentValue { get; set; }
    }

    /// <summary>
    /// AUX 데이터 요청 DTO
    /// </summary>
    public class AuxDataRequest
    {
        public int EquipmentId { get; set; }
        public string SensorId { get; set; } = "";
        public string Name { get; set; } = "";
        public int Type { get; set; }
        public double Value { get; set; }
    }

    /// <summary>
    /// 알람 요청 DTO
    /// </summary>
    public class AlarmRequest
    {
        public int EquipmentId { get; set; }
        public string Message { get; set; } = "";
        public int Level { get; set; }
    }

    #endregion
}