using AuctionHouse.Core.Entities;
using AuctionHouse.Hubs;
using AuctionHouse.Infrastructure;
using AuctionHouse.Infrastructure.Data;
using AuctionHouse.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog — yapılandırmadan oku, konsola ve dosyaya yaz.
builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/auction-.log", rollingInterval: RollingInterval.Day));

// EF Core (SQLite) — Infrastructure katmanından.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity: ApplicationUser + roller, AuctionDbContext üzerinde.
builder.Services
    .AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AuctionDbContext>();

builder.Services.AddControllersWithViews();

// Giriş yapan her kullanıcıyı otomatik olarak Seller rolüne ekler
// (yeni kayıtların açık artırma açabilmesi için).
builder.Services.AddScoped<IClaimsTransformation, AutoSellerClaimsTransformation>();

// SignalR + presence tracker + IAuctionNotifier (canlı yayın).
builder.Services.AddAuctionRealtime();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// HTTP isteklerini Serilog ile özet logla.
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// SignalR hub endpoint'i.
app.MapHub<AuctionHub>("/hubs/auction");

try
{
    // Veritabanını migrate et + seed (roller, örnek satıcı, açık artırmalar).
    await DbSeeder.SeedAsync(app.Services);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Uygulama beklenmedik şekilde sonlandı.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
