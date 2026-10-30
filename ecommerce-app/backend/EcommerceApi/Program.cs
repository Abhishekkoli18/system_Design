using System.Text;
using EcommerceApi.Data;
using EcommerceApi.Helpers;
using EcommerceApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using EcommerceApi.Services.Logging;
using EcommerceApi.Middleware;
using Microsoft.AspNetCore.RateLimiting;


var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1) REGISTER SERVICES (dependency injection container)
//    Everything below is "available" to be injected into any
//    controller or service constructor later.
// ============================================================
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/app-log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// --- Database connection ---
// Reads "ConnectionStrings:DefaultConnection" from appsettings.json
// and tells EF Core to use SQL Server with that connection string.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- Our own services ---
// "Scoped" = one instance created per HTTP request.
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<JwtHelper>(); // no per-request state, safe as singleton

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 5;              // max 5 attempts
        opt.Window = TimeSpan.FromMinutes(1);  // per 1 minute
        opt.QueueLimit = 0;                // no waiting in line, just reject immediately
    });

    options.RejectionStatusCode = 429;    // "Too Many Requests" status code
});


builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

// --- Switchable logging: File or Database, based on appsettings.json ---
   // add this to your top "using" list too
var loggingProvider = builder.Configuration["LoggingSettings:Provider"];

if (loggingProvider == "Database")
{
    builder.Services.AddScoped<IApplicationLogger, DatabaseLogger>();
}
else
{
    builder.Services.AddScoped<IApplicationLogger, FileLogger>();
}

// --- Controllers ---
builder.Services.AddControllers();

// --- CORS (Cross-Origin Resource Sharing) ---
// The React app runs on a DIFFERENT port (5173) than the API (e.g. 5000/7000).
// Browsers block requests between different origins by default unless the
// server explicitly allows it. This policy says "requests from the React
// dev server are allowed."
var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(allowedOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// --- JWT Authentication ---
// This configures HOW the server validates a token that arrives on a request:
// same secret key, issuer, and audience that JwtHelper used to CREATE the token.
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})  
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

// Swagger - gives you a browser UI to test API endpoints without React
builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// --- Seed the hardcoded Admin account ---
// Runs once every time the app starts. Checks if an Admin already exists;
// if not, creates the one fixed Admin account (see Data/DbSeeder.cs for
// the hardcoded email/password). This is intentional: there is no public
// UI to create an Admin - it always exists via this seed step instead.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DbSeeder.SeedAdmin(db);
}

// ============================================================
// 2) MIDDLEWARE PIPELINE
//    Order matters here - each request passes through these
//    top to bottom.
// ============================================================

// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI(); // browse to /swagger to test endpoints manually
// }
app.UseSwagger();
app.UseSwaggerUI();

// app.UseHttpsRedirection(); // disabled - running HTTP only for local dev
//----------------
app.UseRequestLogging();
// CORS must come before Authentication/Authorization
app.UseCors("AllowReactApp");

app.UseAuthentication(); // "who are you?" - reads and validates the JWT
app.UseAuthorization();  // "are you allowed to do this?" - checks [Authorize] rules
app.UseRateLimiter();
app.MapControllers();

app.Run();
