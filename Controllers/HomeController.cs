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

        public IActionResult Error()
        {
            return View();
        }
    }
}
