using System.Text;
using Kuyumcu.Api.Auth;
using Kuyumcu.Api.Common;
using Kuyumcu.Api.Data;
using Kuyumcu.Api.Modules;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Yapılandırma ─────────────────────────────────────────────────────────────
var jwtAyarlar = new JwtAyarlar
{
    Secret = builder.Configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret tanımlı değil (appsettings veya ortam değişkeni)"),
    AccessDakika = builder.Configuration.GetValue("Jwt:AccessDakika", 15),
    RefreshGun = builder.Configuration.GetValue("Jwt:RefreshGun", 7),
};
builder.Services.AddSingleton(jwtAyarlar);
builder.Services.AddSingleton<TokenServisi>();

var baglanti = builder.Configuration.GetConnectionString("Kuyumcu")
    ?? throw new InvalidOperationException("ConnectionStrings:Kuyumcu tanımlı değil");
builder.Services.AddDbContext<KuyumcuDbContext>(o =>
    o.UseMySql(baglanti, ServerVersion.AutoDetect(baglanti)));

// ── Kimlik doğrulama ─────────────────────────────────────────────────────────
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtAyarlar.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
        // Nest JwtGuard sözleşmesi: 401 gövdesi { message, error, statusCode }
        //   • Token yoksa: "Token bulunamadı"
        //   • Token geçersiz/süresi dolmuşsa: "Geçersiz veya süresi dolmuş token"
        o.Events = new JwtBearerEvents
        {
            // SignalR WebSocket handshake'i Authorization başlığı gönderemez; token
            // access_token query'sinden okunur (yalnız /hubs yolları için).
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken)
                    && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = accessToken;
                return Task.CompletedTask;
            },
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                var auth = ctx.Request.Headers.Authorization.ToString();
                var tokenVar = auth.StartsWith("Bearer ", StringComparison.Ordinal)
                    && auth.Length > "Bearer ".Length;
                ctx.Response.StatusCode = 401;
                ctx.Response.ContentType = "application/json; charset=utf-8";
                await ctx.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                {
                    message = tokenVar ? "Geçersiz veya süresi dolmuş token" : "Token bulunamadı",
                    error = "Unauthorized",
                    statusCode = 401,
                }));
            },
        };
    });
builder.Services.AddAuthorization();

// ── MVC + doğrulama sözleşmesi ───────────────────────────────────────────────
builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(o =>
    {
        // Nest ValidationPipe formatı: { message: string[], error: "Bad Request", statusCode: 400 }
        o.InvalidModelStateResponseFactory = ctx =>
        {
            var mesajlar = ctx.ModelState
                .Where(kv => kv.Value?.Errors.Count > 0)
                .SelectMany(kv => kv.Value!.Errors.Select(e =>
                    string.IsNullOrWhiteSpace(e.ErrorMessage) ? $"{kv.Key} geçersiz" : e.ErrorMessage))
                .ToArray();
            return new BadRequestObjectResult(new
            {
                message = mesajlar,
                error = "Bad Request",
                statusCode = 400,
            });
        };
    });

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Canlı kur push kanalı (SignalR)
builder.Services.AddSignalR();

// Rol yetki matrisi önbelleği (tenant başına)
builder.Services.AddMemoryCache();

// Modül servisleri (tek kayıt noktası)
builder.Services.AddKuyumcuModulleri();

// Kur çekme zamanlayıcısı (Nest'teki @Cron karşılığı)
builder.Services.AddHostedService<Kuyumcu.Api.Modules.Kur.KurZamanlayici>();
builder.Services.AddHttpClient();

var app = builder.Build();

app.UseMiddleware<HataYakalayici>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Blazor WASM yayın çıktısını sun (üretim: tek süreçten hem web hem API)
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<Kuyumcu.Api.Modules.Kur.KurHub>("/hubs/kur");   // canlı kur push
app.MapFallbackToFile("index.html");   // SPA yönlendirmesi

app.Run();
