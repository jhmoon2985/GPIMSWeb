using GPIMSWeb.Models;
using GPIMSWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using System.Security.Claims;
using System.Globalization;

namespace GPIMSWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;

        public AccountController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userService.AuthenticateAsync(model.Username, model.Password);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username or password");
                return View(model);
            }

            // 언어 설정 - 쿠키 직접 설정
            if (!string.IsNullOrEmpty(model.Language))
            {
                Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(model.Language)),
                    new CookieOptions 
                    { 
                        Expires = DateTimeOffset.UtcNow.AddYears(1),
                        HttpOnly = false,
                        Secure = false,
                        SameSite = SameSiteMode.Lax,
                        Path = "/",
                        Domain = null
                    }
                );
            }

            // 인증 쿠키 생성
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("FullName", user.Name),
                new Claim(ClaimTypes.Role, user.Level.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity), authProperties);

            // 로그인 기록
            await _userService.LogUserActionAsync(user.Id, "Login", "User logged in", 
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _userService.LogUserActionAsync(userId, "Logout", "User logged out",
                    HttpContext.Connection.RemoteIpAddress?.ToString());
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetLanguage(string culture, string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(culture))
            {
                // 지원되는 문화권인지 확인
                var supportedCultures = new[] { "en", "ko", "zh" };
                if (!supportedCultures.Contains(culture))
                {
                    culture = "en"; // 기본값으로 설정
                }

                // 기존 쿠키 완전 삭제
                var cookieName = CookieRequestCultureProvider.DefaultCookieName;
                Response.Cookies.Delete(cookieName);
                Response.Cookies.Delete(cookieName, new CookieOptions { Path = "/" });
                
                // 새 쿠키 설정
                var requestCulture = new RequestCulture(culture, culture);
                var cookieValue = CookieRequestCultureProvider.MakeCookieValue(requestCulture);
                
                Response.Cookies.Append(
                    cookieName,
                    cookieValue,
                    new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddYears(1),
                        HttpOnly = false,
                        Secure = false, // 개발 환경에서는 false
                        SameSite = SameSiteMode.Lax,
                        Path = "/",
                        Domain = null
                    }
                );
                
                // 즉시 현재 스레드의 문화권도 변경
                var cultureInfo = new CultureInfo(culture);
                CultureInfo.CurrentCulture = cultureInfo;
                CultureInfo.CurrentUICulture = cultureInfo;
                Thread.CurrentThread.CurrentCulture = cultureInfo;
                Thread.CurrentThread.CurrentUICulture = cultureInfo;
            }

            return LocalRedirect(returnUrl ?? "/");
        }
    }
}