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
        sqlOptions.CommandTimeout(30);
        sqlOptions.EnableRetryOnFailure(3);
    });
    // 개발 환경에서만 민감한 데이터 로깅 비활성화
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging(false); // 이 부분을 false로 변경
        options.EnableDetailedErrors(false); // 이 부분도 false로 변경
    }
}, ServiceLifetime.Scoped);

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
builder.Services.AddScoped<IDataSeederService, DataSeederService>();

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

// 애플리케이션 시작 시 데이터 동기화 - 백그라운드로 실행
if (app.Environment.IsDevelopment())
{
    // 백그라운드에서 실행하여 시작 시간 단축
    _ = Task.Run(async () =>
    {
        await Task.Delay(2000); // 2초 후에 실행
        using var scope = app.Services.CreateScope();
        var seederService = scope.ServiceProvider.GetRequiredService<IDataSeederService>();
        try
        {
            await seederService.SeedChannelDataAsync();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("✅ Channel data synchronized in background");
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "❌ Error synchronizing channel data in background");
        }
    });
}

// 개발 환경에서 API 엔드포인트 정보 출력 - 간소화
if (app.Environment.IsDevelopment())
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("🚀 GPIMSWeb Server Started");
    logger.LogInformation("🌐 Web Interface: https://localhost:7090");
}

app.Run();