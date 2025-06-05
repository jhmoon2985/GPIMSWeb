using GPIMSWeb.Data;
using GPIMSWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace GPIMSWeb.Services
{
    public class DataSeederService : IDataSeederService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DataSeederService> _logger;

        public DataSeederService(ApplicationDbContext context, ILogger<DataSeederService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedChannelDataAsync()
        {
            try
            {
                // 시스템 설정에서 장비 수와 채널 수 가져오기
                var equipmentCountSetting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.Key == "EquipmentCount");
                var channelsPerEquipmentSetting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.Key == "ChannelsPerEquipment");

                if (equipmentCountSetting == null || channelsPerEquipmentSetting == null)
                {
                    _logger.LogWarning("Equipment or Channel settings not found");
                    return;
                }

                var equipmentCount = int.Parse(equipmentCountSetting.Value);
                var channelsPerEquipment = int.Parse(channelsPerEquipmentSetting.Value);

                _logger.LogInformation($"Seeding data for {equipmentCount} equipment with {channelsPerEquipment} channels each");

                // 기존 장비 확인 및 생성
                for (int equipmentId = 1; equipmentId <= equipmentCount; equipmentId++)
                {
                    var equipment = await _context.Equipment.FindAsync(equipmentId);
                    if (equipment == null)
                    {
                        equipment = new Equipment
                        {
                            Id = equipmentId,
                            Name = $"GPIMS-{equipmentId:D3}",
                            IsOnline = equipmentId <= equipmentCount - 1, // 마지막 장비는 오프라인으로
                            LastUpdateTime = DateTime.Now.AddMinutes(-Random.Shared.Next(0, 30)),
                            Version = equipmentId % 3 == 0 ? "v2.0.5" : "v2.1.0", // 일부는 구 버전
                            Status = equipmentId == equipmentCount ? EquipmentStatus.Error : 
                                    equipmentId % 2 == 0 ? EquipmentStatus.Running : EquipmentStatus.Idle
                        };
                        _context.Equipment.Add(equipment);
                        _logger.LogInformation($"Created equipment: {equipment.Name}");
                    }

                    // 채널 데이터 확인 및 생성
                    for (int channelNum = 1; channelNum <= channelsPerEquipment; channelNum++)
                    {
                        var existingChannel = await _context.Channels
                            .FirstOrDefaultAsync(c => c.EquipmentId == equipmentId && c.ChannelNumber == channelNum);

                        if (existingChannel == null)
                        {
                            var isActive = channelNum <= channelsPerEquipment / 2; // 절반만 활성
                            var channel = new Channel
                            {
                                EquipmentId = equipmentId,
                                ChannelNumber = channelNum,
                                Status = isActive ? ChannelStatus.Active : ChannelStatus.Idle,
                                Mode = channelNum % 2 == 0 ? ChannelMode.Charge : ChannelMode.Discharge,
                                Voltage = isActive ? 3.7 + (channelNum * 0.1) : 0,
                                Current = isActive ? 1.5 + (channelNum * 0.2) : 0,
                                Capacity = 50.0 + (channelNum * 5),
                                Power = isActive ? (3.7 + (channelNum * 0.1)) * (1.5 + (channelNum * 0.2)) : 0,
                                Energy = isActive ? 25.5 + (channelNum * 3) : 0,
                                ScheduleName = isActive ? $"Schedule_{channelNum}" : "",
                                LastUpdateTime = DateTime.Now
                            };
                            _context.Channels.Add(channel);
                        }
                    }

                    // 기존 채널이 설정된 수보다 많으면 삭제
                    var extraChannels = await _context.Channels
                        .Where(c => c.EquipmentId == equipmentId && c.ChannelNumber > channelsPerEquipment)
                        .ToListAsync();

                    if (extraChannels.Any())
                    {
                        _context.Channels.RemoveRange(extraChannels);
                        _logger.LogInformation($"Removed {extraChannels.Count} extra channels from equipment {equipmentId}");
                    }
                }

                // 기존 장비가 설정된 수보다 많으면 삭제
                var extraEquipment = await _context.Equipment
                    .Where(e => e.Id > equipmentCount)
                    .ToListAsync();

                if (extraEquipment.Any())
                {
                    _context.Equipment.RemoveRange(extraEquipment);
                    _logger.LogInformation($"Removed {extraEquipment.Count} extra equipment");
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Channel data seeding completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while seeding channel data");
                throw;
            }
        }
    }
}