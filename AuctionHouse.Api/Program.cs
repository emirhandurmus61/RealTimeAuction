using AuctionHouse.Core.Entities;
using AuctionHouse.Infrastructure;
using AuctionHouse.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// EF Core + uygulama servisleri (IAuctionService, IBidService).
builder.Services.AddInfrastructure(builder.Configuration);

// Identity — bearer token tabanlı API kimlik doğrulaması.
// Web ile aynı AuctionDbContext/Identity şemasını paylaşır.
builder.Services
    .AddIdentityApiEndpoints<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AuctionDbContext>();

builder.Services.AddAuthorization();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RealTimeAuction API",
        Version = "v1",
        Description = "Açık artırma ve teklif REST API'si."
    });

    // Swagger UI'da bearer token ile yetkili istek atabilmek için.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Token'ı doğrudan girin (Bearer öneki olmadan)."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// /register, /login vb. Identity endpoint'leri.
app.MapIdentityApi<ApplicationUser>();

app.MapControllers();

app.Run();
