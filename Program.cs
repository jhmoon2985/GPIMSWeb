using GPIMSWeb.Data;
using GPIMSWeb.Services;
using GPIMSWeb.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// 성능 최적화 설정
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 1048576; // 1MB
});

// 연결 풀 최적화된 DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sqlOptions =>
    {
        sqlOptions.CommandTimeout(10); // 30초에서 10초로 단축
        sqlOptions.EnableRetryOnFailure(2, TimeSpan.FromSeconds(1), null); // 재시도 최적화
    });
    
    // 성능 최적화
    options.EnableSensitiveDataLogging(false);
    options.EnableDetailedErrors(false);
    options.EnableServiceProviderCaching(true);
    options.EnableThreadSafetyChecks(false); // 성능 향상
}, ServiceLifetime.Scoped);

// 메모리 캐시 최적화
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1000; // 최대 1000개 항목
    options.CompactionPercentage = 0.25; // 25% 압축
});

// SignalR 최적화
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = false;
    options.MaximumReceiveMessageSize = 32768; // 32KB
    options.StreamBufferCapacity = 10;
    options.MaximumParallelInvocationsPerClient = 2;
}).AddMessagePackProtocol(); // JSON 대신 MessagePack 사용

// 압축 활성화
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// HTTP 클라이언트 팩토리
builder.Services.AddHttpClient();

// 서비스 등록
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEquipmentService, EquipmentService>();
builder.Services.AddSingleton<IRealtimeDataService, RealtimeDataService>();
builder.Services.AddScoped<IDataSeederService, DataSeederService>();

// 컨트롤러 최적화
builder.Services.AddControllersWithViews(options =>
{
    options.ModelMetadataDetailsProviders.Clear(); // 메타데이터 프로바이더 제거로 성능 향상
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null; // camelCase 변환 비활성화
});

var app = builder.Build();

// 압축 활성화
app.UseResponseCompression();

// 정적 파일 캐싱
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000");
    }
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// SignalR 허브
app.MapHub<RealtimeDataHub>("/realtimeDataHub");

// 개발 환경에서 성능 정보 출력
if (app.Environment.IsDevelopment())
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("🚀 High-Performance GPIMSWeb Server Started");
    logger.LogInformation("⚡ Optimized for 128 channels at 100ms intervals");
}

app.Run();