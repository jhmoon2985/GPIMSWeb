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

// Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

app.Run();