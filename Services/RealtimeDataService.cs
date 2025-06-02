using GPIMSWeb.Models;
using GPIMSWeb.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace GPIMSWeb.Services
{
    public class RealtimeDataService : IRealtimeDataService
    {
        private readonly IHubContext<RealtimeDataHub> _hubContext;
        private readonly ConcurrentDictionary<string, object> _realtimeData;

        public RealtimeDataService(IHubContext<RealtimeDataHub> hubContext)
        {
            _hubContext = hubContext;
            _realtimeData = new ConcurrentDictionary<string, object>();
        }

        public void UpdateChannelData(int equipmentId, int channelNumber, Channel channelData)
        {
            var key = $"equipment_{equipmentId}_channel_{channelNumber}";
            _realtimeData.AddOrUpdate(key, channelData, (k, v) => channelData);

            // SignalR로 실시간 데이터 전송
            _hubContext.Clients.Group($"Equipment_{equipmentId}")
                .SendAsync("UpdateChannelData", channelNumber, channelData);
        }

        public void UpdateCanLinData(int equipmentId, string name, CanLinData canLinData)
        {
            var key = $"equipment_{equipmentId}_canlin_{name}";
            _realtimeData.AddOrUpdate(key, canLinData, (k, v) => canLinData);

            _hubContext.Clients.Group($"Equipment_{equipmentId}")
                .SendAsync("UpdateCanLinData", name, canLinData);
        }

        public void UpdateAuxData(int equipmentId, string sensorId, AuxData auxData)
        {
            var key = $"equipment_{equipmentId}_aux_{sensorId}";
            _realtimeData.AddOrUpdate(key, auxData, (k, v) => auxData);

            _hubContext.Clients.Group($"Equipment_{equipmentId}")
                .SendAsync("UpdateAuxData", sensorId, auxData);
        }

        public void AddAlarm(int equipmentId, string message, AlarmLevel level)
        {
            var alarm = new Alarm
            {
                EquipmentId = equipmentId,
                Message = message,
                Level = level,
                CreatedAt = DateTime.Now,
                IsCleared = false
            };

            _hubContext.Clients.Group($"Equipment_{equipmentId}")
                .SendAsync("NewAlarm", alarm);

            // 모든 클라이언트에게도 알람 전송 (대시보드용)
            _hubContext.Clients.All.SendAsync("NewAlarm", alarm);
        }

        public async Task<Channel> GetChannelDataAsync(int equipmentId, int channelNumber)
        {
            var key = $"equipment_{equipmentId}_channel_{channelNumber}";
            _realtimeData.TryGetValue(key, out var data);
            return data as Channel;
        }

        public async Task<List<Channel>> GetAllChannelDataAsync(int equipmentId)
        {
            var channels = new List<Channel>();
            var keys = _realtimeData.Keys.Where(k => k.StartsWith($"equipment_{equipmentId}_channel_"));

            foreach (var key in keys)
            {
                if (_realtimeData.TryGetValue(key, out var data) && data is Channel channel)
                {
                    channels.Add(channel);
                }
            }

            return channels.OrderBy(c => c.ChannelNumber).ToList();
        }
    }
}