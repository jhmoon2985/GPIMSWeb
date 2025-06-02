using GPIMSWeb.Models;
using GPIMSWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GPIMSWeb.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetAllUsersAsync();
            return View(users);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _userService.CreateUserAsync(model);
            if (!result)
            {
                ModelState.AddModelError("", "Username already exists or creation failed");
                return View(model);
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _userService.LogUserActionAsync(userId, "Create User", $"Created user {model.Username}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();

            var model = new CreateUserViewModel
            {
                Username = user.Username,
                Name = user.Name,
                Department = user.Department,
                Level = user.Level
            };

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Edit(int id, CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _userService.UpdateUserAsync(id, model);
            if (!result)
            {
                ModelState.AddModelError("", "Update failed");
                return View(model);
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _userService.LogUserActionAsync(userId, "Update User", $"Updated user {model.Username}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            var result = await _userService.DeleteUserAsync(id);
            if (result)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _userService.LogUserActionAsync(userId, "Delete User", $"Deleted user {user.Username}",
                    HttpContext.Connection.RemoteIpAddress?.ToString());
            }

            return Json(new { success = result });
        }

        public async Task<IActionResult> History(int? userId, DateTime? fromDate, DateTime? toDate)
        {
            // Admin이 아닌 경우 본인 기록만 조회
            if (!User.IsInRole("Admin"))
            {
                userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            }

            var history = await _userService.GetUserHistoryAsync(userId, fromDate, toDate);
            return View(history);
        }
    }
}