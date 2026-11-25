// Uygulamanın giriş noktası; veritabanı, Identity, MVC, servisler ve MCP altyapısını kurar.
// Ayrıca başlangıçta seed işlemini çalıştırıp HTTP istek hattını (middleware) tanımlar.
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Mcp;
using VeterinerKlinigi.Models;
using VeterinerKlinigi.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Veritabanı (SQLite) ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- ASP.NET Core Identity ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Ödev gereği admin şifresi "sau" (3 karakter) olacağından gevşek kurallar.
    options.Password.RequiredLength = 3;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

// --- MVC + HttpContextAccessor (MCP araçları için) ---
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// --- İş (servis) katmanı ---
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IVeterinarianService, VeterinerKlinigi.Services.VeterinarianService>();
builder.Services.AddScoped<IPetService, PetService>();
builder.Services.AddScoped<IReportService, ReportService>();

// --- MCP Sunucusu ---
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<VeterinerMcpTools>();

var app = builder.Build();

// --- Migration & Seed (geliştirme kolaylığı) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await DbSeeder.SeedAsync(services);
}

// Üretim ortamında hata sayfası ve HSTS koruması etkinleştirilir.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// --- REST API rotaları ---
app.MapControllers();

// --- MCP uç noktası (yetkilendirme ile korunur) ---
app.MapMcp("/mcp").RequireAuthorization();

app.Run();