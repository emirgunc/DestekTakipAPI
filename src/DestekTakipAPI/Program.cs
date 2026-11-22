using System.Text;
using DestekTakipAPI.API.Middleware;
using DestekTakipAPI.Application.Settings;
using DestekTakipAPI.Infrastructure.Persistence;
using DestekTakipAPI.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// =============================================================================
// SERVIS KAYITLARI
// =============================================================================

// JWT Ayarlari strongly-typed olarak bind edilir
builder.Services.Configure<JwtAyarlariSecenekleri>(
    builder.Configuration.GetSection(JwtAyarlariSecenekleri.BolumAdi));

JwtAyarlariSecenekleri jwtAyarlari = builder.Configuration
    .GetSection(JwtAyarlariSecenekleri.BolumAdi)
    .Get<JwtAyarlariSecenekleri>()
    ?? throw new InvalidOperationException("JWT ayarlari bulunamadi.");

bool anahtarYetersiz = string.IsNullOrWhiteSpace(jwtAyarlari.GizliAnahtar)
                    || jwtAyarlari.GizliAnahtar.Length < 32;
if (anahtarYetersiz)
{
    throw new InvalidOperationException("JWT gizli anahtari tanimlanmamis veya 32 karakterden kisa.");
}

// DbContext
string? baglantiBilgisi = builder.Configuration.GetConnectionString("DestekTakipBaglantisi");

bool baglantiBilgisiEksik = string.IsNullOrWhiteSpace(baglantiBilgisi);
if (baglantiBilgisiEksik)
{
    throw new InvalidOperationException("Veritabani baglanti bilgisi bulunamadi.");
}

builder.Services.AddDbContext<DestekTakipDbContext>(opt =>
{
    opt.UseSqlServer(baglantiBilgisi, sql =>
    {
        sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
    });

    bool dev = builder.Environment.IsDevelopment();
    if (dev)
    {
        opt.EnableSensitiveDataLogging();
        opt.EnableDetailedErrors();
    }
});

// JWT Token Uretici
builder.Services.AddScoped<IJwtTokenUretici, JwtTokenUretici>();

// E-posta Bildirim Ayarlari ve Servisi
builder.Services.Configure<EpostaAyarlariSecenekleri>(
    builder.Configuration.GetSection(EpostaAyarlariSecenekleri.BolumAdi));
builder.Services.AddScoped<IEpostaServisi, EpostaServisi>();

// JWT Authentication
byte[] anahtarBytes = Encoding.UTF8.GetBytes(jwtAyarlari.GizliAnahtar);

builder.Services
    .AddAuthentication(opt =>
    {
        opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        opt.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(opt =>
    {
        opt.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        opt.SaveToken            = true;
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidIssuer              = jwtAyarlari.Yayinci,
            ValidateAudience         = true,
            ValidAudience            = jwtAyarlari.Izleyici,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(anahtarBytes),
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero,
            RoleClaimType            = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        };
        opt.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                bool sureDoldu = ctx.Exception is SecurityTokenExpiredException;
                if (sureDoldu)
                {
                    ctx.Response.Headers.Append("Token-Suresi-Doldu", "true");
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger - JWT Bearer destekli
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Destek Takip API",
        Version     = "v1",
        Description = "Yazilim Destek Takip Sistemi REST API. " +
                      "Korunan endpoint'ler icin once /api/auth/giris ile token alin."
    });

    OpenApiSecurityScheme bearer = new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "JWT token'inizi giriniz."
    };
    opt.AddSecurityDefinition("Bearer", bearer);
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
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

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("GelistirmePolitikasi", p =>
    {
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// =============================================================================
// MIDDLEWARE PIPELINE
// =============================================================================

WebApplication app = builder.Build();

// 1. Global Exception Handler — EN BASA eklenir
app.GlobalHataYoneticisiniKullan();

// 2. Swagger (yalnizca Development)
bool swaggerAktif = app.Environment.IsDevelopment();
if (swaggerAktif)
{
    app.UseSwagger();
    app.UseSwaggerUI(opt =>
    {
        opt.SwaggerEndpoint("/swagger/v1/swagger.json", "Destek Takip API v1");
        opt.RoutePrefix = string.Empty;
    });
}

// 3. HTTPS yonlendirmesi
app.UseHttpsRedirection();

// 4. CORS
app.UseCors("GelistirmePolitikasi");

// 5. Authentication (JWT token cozumlenir) — Authorization'dan ONCE olmali
app.UseAuthentication();

// 6. Authorization ([Authorize] attribute'lari isler)
app.UseAuthorization();

// 7. Controller route'lari
app.MapControllers();

app.Run();
