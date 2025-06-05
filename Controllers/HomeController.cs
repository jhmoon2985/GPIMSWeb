using GPIMSWeb.Models;
using GPIMSWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GPIMSWeb.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IEquipmentService _equipmentService;

        public HomeController(IEquipmentService equipmentService)
        {
            _equipmentService = equipmentService;
        }

        public async Task<IActionResult> Index()
        {
            var equipment = await _equipmentService.GetAllEquipmentAsync();
            return View(equipment);
        }

        // 이 메서드가 없다면 추가하세요
        public async Task<IActionResult> Detail(int id)
        {
            // Equipment Detail은 EquipmentController로 리다이렉트
            return RedirectToAction("Detail", "Equipment", new { id = id });
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
