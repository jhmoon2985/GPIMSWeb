using GPIMSWeb.Data;
using GPIMSWeb.Services;
using GPIMSWeb.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

// Entity Framework with optimized settings
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sqlOptions =>
    {
        sqlOptions.CommandTimeout(30); // 명령 타임아웃 설정
        sqlOptions.EnableRetryOnFailure(3); // 재시도 로직
    });
}, ServiceLifetime.Scoped); // Scoped 명시적 설정

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

// SignalR
builder.Services.AddSignalR();

// Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEquipmentService, EquipmentService>();
builder.Services.AddSingleton<IRealtimeDataService, RealtimeDataService>();
builder.Services.AddScoped<IDataSeederService, DataSeederService>(); // 새로 추가

// Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("en"),
        new CultureInfo("ko"), 
        new CultureInfo("zh")
    };

    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    
    options.RequestCultureProviders.Clear();
    options.RequestCultureProviders.Add(new CookieRequestCultureProvider());
    options.RequestCultureProviders.Add(new QueryStringRequestCultureProvider());
    options.RequestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider());
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // 개발 환경에서 CORS 허용 (클라이언트 테스트용)
    app.UseCors(policy => policy
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseRequestLocalization();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.MapHub<RealtimeDataHub>("/realtimeDataHub");

// 애플리케이션 시작 시 데이터 동기화 (개발 환경에서만)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var seederService = scope.ServiceProvider.GetRequiredService<IDataSeederService>();
    try
    {
        await seederService.SeedChannelDataAsync();
        Console.WriteLine("✅ Channel data synchronized on startup");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error synchronizing channel data on startup: {ex.Message}");
    }
}

// 개발 환경에서 API 엔드포인트 정보 출력
if (app.Environment.IsDevelopment())
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("🚀 GPIMSWeb Server Started");
    logger.LogInformation("📡 SignalR Hub: /realtimeDataHub");
    logger.LogInformation("🔗 API Endpoints:");
    logger.LogInformation("   POST /api/ClientData/channel - Channel data");
    logger.LogInformation("   POST /api/ClientData/canlin - CAN/LIN data");
    logger.LogInformation("   POST /api/ClientData/aux - AUX sensor data");
    logger.LogInformation("   POST /api/ClientData/alarm - Alarm data");
    logger.LogInformation("   GET  /api/ClientData/test - Connection test");
    logger.LogInformation("🌐 Web Interface: https://localhost:7090");
    logger.LogInformation("👤 Default Login: admin / admin123");
}

app.Run();