using GPIMSWeb.Data;
using GPIMSWeb.Models;
using GPIMSWeb.Services;
using GPIMSWeb.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GPIMSWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientDataController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRealtimeDataService _realtimeDataService;
        private readonly IHubContext<RealtimeDataHub> _hubContext;
        private readonly ILogger<ClientDataController> _logger;

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
        }

        [HttpPost("channel")]
        public async Task<IActionResult> UpdateChannelData([FromBody] ChannelDataRequest request)
        {
            try
            {
                // 데이터베이스에서 기존 채널 찾기 또는 생성
                var existingChannel = await _context.Channels
                    .FirstOrDefaultAsync(c => c.EquipmentId == request.EquipmentId && 
                                            c.ChannelNumber == request.ChannelNumber);

                if (existingChannel != null)
                {
                    // 기존 채널 업데이트
                    existingChannel.Status = (ChannelStatus)request.Status;
                    existingChannel.Mode = (ChannelMode)request.Mode;
                    existingChannel.Voltage = request.Voltage;
                    existingChannel.Current = request.Current;
                    existingChannel.Capacity = request.Capacity;
                    existingChannel.Power = request.Power;
                    existingChannel.Energy = request.Energy;
                    existingChannel.ScheduleName = request.ScheduleName ?? "";
                    existingChannel.LastUpdateTime = DateTime.Now;

                    _context.Channels.Update(existingChannel);
                }
                else
                {
                    // 새 채널 생성
                    var newChannel = new Channel
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

                    _context.Channels.Add(newChannel);
                    existingChannel = newChannel;
                }

                // 장비 상태도 업데이트
                var equipment = await _context.Equipment.FindAsync(request.EquipmentId);
                if (equipment != null)
                {
                    equipment.LastUpdateTime = DateTime.Now;
                    equipment.IsOnline = true;
                    // 활성 채널이 있으면 Running, 없으면 Idle
                    var hasActiveChannels = await _context.Channels
                        .AnyAsync(c => c.EquipmentId == request.EquipmentId && c.Status == ChannelStatus.Active);
                    equipment.Status = hasActiveChannels ? EquipmentStatus.Running : EquipmentStatus.Idle;
                    
                    _context.Equipment.Update(equipment);
                }

                await _context.SaveChangesAsync();

                // 실시간 서비스에도 업데이트
                _realtimeDataService.UpdateChannelData(request.EquipmentId, request.ChannelNumber, existingChannel);

                // SignalR로 실시간 업데이트 전송
                await _hubContext.Clients.All.SendAsync("UpdateChannelData", request.EquipmentId, request.ChannelNumber, existingChannel);

                _logger.LogDebug("Channel data updated - Equipment: {EquipmentId}, Channel: {ChannelNumber}, Status: {Status}, Voltage: {Voltage}V, Current: {Current}A",
                    request.EquipmentId, request.ChannelNumber, request.Status, request.Voltage, request.Current);

                return Ok(new { success = true, message = "Channel data updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating channel data for Equipment {EquipmentId}, Channel {ChannelNumber}",
                    request.EquipmentId, request.ChannelNumber);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("canlin")]
        public async Task<IActionResult> UpdateCanLinData([FromBody] CanLinDataRequest request)
        {
            try
            {
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
                await _hubContext.Clients.All.SendAsync("UpdateCanLinData", request.EquipmentId, request.Name, existingCanLin);

                _logger.LogDebug("CAN/LIN data updated - Equipment: {EquipmentId}, Name: {Name}, Value: {CurrentValue}",
                    request.EquipmentId, request.Name, request.CurrentValue);

                return Ok(new { success = true, message = "CAN/LIN data updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating CAN/LIN data for Equipment {EquipmentId}, Name {Name}",
                    request.EquipmentId, request.Name);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("aux")]
        public async Task<IActionResult> UpdateAuxData([FromBody] AuxDataRequest request)
        {
            try
            {
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
                await _hubContext.Clients.All.SendAsync("UpdateAuxData", request.EquipmentId, request.SensorId, existingAux);

                _logger.LogDebug("AUX data updated - Equipment: {EquipmentId}, Sensor: {SensorId}, Name: {Name}, Type: {Type}, Value: {Value}",
                    request.EquipmentId, request.SensorId, request.Name, request.Type, request.Value);

                return Ok(new { success = true, message = "AUX data updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating AUX data for Equipment {EquipmentId}, Sensor {SensorId}",
                    request.EquipmentId, request.SensorId);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("alarm")]
        public async Task<IActionResult> AddAlarm([FromBody] AlarmRequest request)
        {
            try
            {
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

                return Ok(new { success = true, message = "Alarm added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding alarm for Equipment {EquipmentId}",
                    request.EquipmentId);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // 연결 테스트용 엔드포인트
        [HttpGet("test")]
        public IActionResult TestConnection()
        {
            return Ok(new { 
                success = true, 
                message = "Connection successful", 
                serverTime = DateTime.Now,
                version = "1.0.0"
            });
        }
    }

    // Request DTOs
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

    public class CanLinDataRequest
    {
        public int EquipmentId { get; set; }
        public string Name { get; set; } = "";
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public double CurrentValue { get; set; }
    }

    public class AuxDataRequest
    {
        public int EquipmentId { get; set; }
        public string SensorId { get; set; } = "";
        public string Name { get; set; } = "";
        public int Type { get; set; }
        public double Value { get; set; }
    }

    public class AlarmRequest
    {
        public int EquipmentId { get; set; }
        public string Message { get; set; } = "";
        public int Level { get; set; }
    }
}