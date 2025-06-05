using GPIMSWeb.Data;
using GPIMSWeb.Models;
using GPIMSWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

namespace GPIMSWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly IDataSeederService _dataSeederService;
        private readonly IOptionsSnapshot<RequestLocalizationOptions> _localizationOptions;

        public SettingsController(ApplicationDbContext context, IUserService userService,
            IDataSeederService dataSeederService,
            IOptionsSnapshot<RequestLocalizationOptions> localizationOptions)
        {
            _context = context;
            _userService = userService;
            _dataSeederService = dataSeederService;
            _localizationOptions = localizationOptions;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var settings = await _context.SystemSettings.ToListAsync();
            var model = new SettingsViewModel
            {
                EquipmentCount = int.Parse(settings.FirstOrDefault(s => s.Key == "EquipmentCount")?.Value ?? "4"),
                ChannelsPerEquipment = int.Parse(settings.FirstOrDefault(s => s.Key == "ChannelsPerEquipment")?.Value ?? "8"),
                DefaultLanguage = settings.FirstOrDefault(s => s.Key == "DefaultLanguage")?.Value ?? "en",
                DateFormat = settings.FirstOrDefault(s => s.Key == "DateFormat")?.Value ?? "yyyy-MM-dd HH:mm:ss"
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveEquipmentSettings(int equipmentCount, int channelsPerEquipment)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                var equipmentSetting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.Key == "EquipmentCount");
                if (equipmentSetting != null)
                {
                    equipmentSetting.Value = equipmentCount.ToString();
                    equipmentSetting.UpdatedAt = DateTime.Now;
                    equipmentSetting.UpdatedBy = username;
                }

                var channelSetting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.Key == "ChannelsPerEquipment");
                if (channelSetting != null)
                {
                    channelSetting.Value = channelsPerEquipment.ToString();
                    channelSetting.UpdatedAt = DateTime.Now;
                    channelSetting.UpdatedBy = username;
                }

                await _context.SaveChangesAsync();

                // 설정 변경 후 채널 데이터 동기화
                await _dataSeederService.SeedChannelDataAsync();

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _userService.LogUserActionAsync(userId, "Update Settings", 
                    $"Equipment: {equipmentCount}, Channels: {channelsPerEquipment} - Data synchronized",
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                return Json(new { 
                    success = true, 
                    message = $"Settings updated successfully! Equipment: {equipmentCount}, Channels per equipment: {channelsPerEquipment}. Database has been synchronized with new settings." 
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = $"Error updating settings: {ex.Message}" 
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveLanguageSettings(string defaultLanguage, string dateFormat)
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                var languageSetting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.Key == "DefaultLanguage");
                if (languageSetting != null)
                {
                    languageSetting.Value = defaultLanguage;
                    languageSetting.UpdatedAt = DateTime.Now;
                    languageSetting.UpdatedBy = username;
                }

                var formatSetting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.Key == "DateFormat");
                if (formatSetting != null)
                {
                    formatSetting.Value = dateFormat;
                    formatSetting.UpdatedAt = DateTime.Now;
                    formatSetting.UpdatedBy = username;
                }

                await _context.SaveChangesAsync();

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _userService.LogUserActionAsync(userId, "Update Settings", 
                    $"Default Language: {defaultLanguage}, Date Format: {dateFormat}",
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                return Json(new { 
                    success = true, 
                    message = "Language settings saved. Restart the application to apply the new default language." 
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = $"Failed to save language settings: {ex.Message}" 
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SyncChannelData()
        {
            try
            {
                await _dataSeederService.SeedChannelDataAsync();
                
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _userService.LogUserActionAsync(userId, "Sync Channel Data", 
                    "Manually synchronized channel data with current settings",
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                return Json(new { 
                    success = true, 
                    message = "Channel data synchronized successfully with current settings!" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = $"Error synchronizing channel data: {ex.Message}" 
                });
            }
        }
    }
}