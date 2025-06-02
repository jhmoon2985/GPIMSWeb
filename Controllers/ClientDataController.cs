using GPIMSWeb.Models;
using GPIMSWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace GPIMSWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientDataController : ControllerBase
    {
        private readonly IRealtimeDataService _realtimeDataService;
        private readonly ILogger<ClientDataController> _logger;

        public ClientDataController(IRealtimeDataService realtimeDataService, 
            ILogger<ClientDataController> logger)
        {
            _realtimeDataService = realtimeDataService;
            _logger = logger;
        }

        [HttpPost("channel")]
        public IActionResult UpdateChannelData([FromBody] ChannelDataRequest request)
        {
            try
            {
                var channelData = new Channel
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
                    ScheduleName = request.ScheduleName,
                    LastUpdateTime = DateTime.Now
                };

                _realtimeDataService.UpdateChannelData(request.EquipmentId, request.ChannelNumber, channelData);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating channel data");
                return BadRequest();
            }
        }

        [HttpPost("canlin")]
        public IActionResult UpdateCanLinData([FromBody] CanLinDataRequest request)
        {
            try
            {
                var canLinData = new CanLinData
                {
                    EquipmentId = request.EquipmentId,
                    Name = request.Name,
                    MinValue = request.MinValue,
                    MaxValue = request.MaxValue,
                    CurrentValue = request.CurrentValue,
                    LastUpdateTime = DateTime.Now
                };

                _realtimeDataService.UpdateCanLinData(request.EquipmentId, request.Name, canLinData);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating CAN/LIN data");
                return BadRequest();
            }
        }

        [HttpPost("aux")]
        public IActionResult UpdateAuxData([FromBody] AuxDataRequest request)
        {
            try
            {
                var auxData = new AuxData
                {
                    EquipmentId = request.EquipmentId,
                    SensorId = request.SensorId,
                    Name = request.Name,
                    Type = (AuxDataType)request.Type,
                    Value = request.Value,
                    LastUpdateTime = DateTime.Now
                };

                _realtimeDataService.UpdateAuxData(request.EquipmentId, request.SensorId, auxData);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating AUX data");
                return BadRequest();
            }
        }

        [HttpPost("alarm")]
        public IActionResult AddAlarm([FromBody] AlarmRequest request)
        {
            try
            {
                _realtimeDataService.AddAlarm(request.EquipmentId, request.Message, (AlarmLevel)request.Level);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding alarm");
                return BadRequest();
            }
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
        public string ScheduleName { get; set; }
    }

    public class CanLinDataRequest
    {
        public int EquipmentId { get; set; }
        public string Name { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public double CurrentValue { get; set; }
    }

    public class AuxDataRequest
    {
        public int EquipmentId { get; set; }
        public string SensorId { get; set; }
        public string Name { get; set; }
        public int Type { get; set; }
        public double Value { get; set; }
    }

    public class AlarmRequest
    {
        public int EquipmentId { get; set; }
        public string Message { get; set; }
        public int Level { get; set; }
    }
}