using GPIMSWeb.Models;
using GPIMSWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GPIMSWeb.Controllers
{
    [Authorize]
    public class EquipmentController : Controller
    {
        private readonly IEquipmentService _equipmentService;
        private readonly IRealtimeDataService _realtimeDataService;
        private readonly IUserService _userService;

        public EquipmentController(IEquipmentService equipmentService, 
            IRealtimeDataService realtimeDataService, IUserService userService)
        {
            _equipmentService = equipmentService;
            _realtimeDataService = realtimeDataService;
            _userService = userService;
        }

        public async Task<IActionResult> Detail(int id)
        {
            var equipment = await _equipmentService.GetEquipmentByIdAsync(id);
            if (equipment == null)
                return NotFound();

            // 사용자 액션 로그
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _userService.LogUserActionAsync(userId, "View Equipment", $"Viewed equipment {id}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return View(equipment);
        }

        [HttpPost]
        public async Task<IActionResult> ClearAlarm(int alarmId)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
            var result = await _equipmentService.ClearAlarmAsync(alarmId, username);

            if (result)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _userService.LogUserActionAsync(userId, "Clear Alarm", $"Cleared alarm {alarmId}",
                    HttpContext.Connection.RemoteIpAddress?.ToString());
            }

            return Json(new { success = result });
        }

        [HttpGet]
        public async Task<IActionResult> GetChannelData(int equipmentId)
        {
            var channels = await _realtimeDataService.GetAllChannelDataAsync(equipmentId);
            return Json(channels);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProgram(int id)
        {
            var userLevel = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userLevel != "Admin" && userLevel != "Maintenance")
            {
                return Json(new { success = false, message = "Insufficient permissions" });
            }

            // 실제 프로그램 업데이트 로직은 클라이언트와의 통신으로 구현
            // 여기서는 시뮬레이션
            var equipment = await _equipmentService.GetEquipmentByIdAsync(id);
            if (equipment == null || equipment.Status != EquipmentStatus.Idle)
            {
                return Json(new { success = false, message = "Equipment not ready for update" });
            }

            // 업데이트 진행 (실제로는 SignalR를 통해 클라이언트에게 명령 전송)
            var result = await _equipmentService.UpdateEquipmentVersionAsync(id, "v2.2.0");

            if (result)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _userService.LogUserActionAsync(userId, "Update Equipment", $"Updated equipment {id} program",
                    HttpContext.Connection.RemoteIpAddress?.ToString());
            }

            return Json(new { success = result });
        }
    }
}