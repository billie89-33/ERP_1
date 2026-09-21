using System.Text;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using JamineERP.Backend.Data;
using JamineERP.Backend.Middlewares;

// Load .env file from the parent directory (root of the workspace)
Env.Load(Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"));

var builder = WebApplication.CreateBuilder(args);


builder.Services.Configure<JamineERP.Backend.Services.CloudinarySettings>(options => 
{
    options.CloudName = Environment.GetEnvironmentVariable("Cloudinary__CloudName") ?? builder.Configuration["CloudinarySettings:CloudName"] ?? "";
    options.ApiKey = Environment.GetEnvironmentVariable("Cloudinary__ApiKey") ?? builder.Configuration["CloudinarySettings:ApiKey"] ?? "";
    options.ApiSecret = Environment.GetEnvironmentVariable("Cloudinary__ApiSecret") ?? builder.Configuration["CloudinarySettings:ApiSecret"] ?? "";
});
builder.Services.AddScoped<JamineERP.Backend.Services.IPhotoService, JamineERP.Backend.Services.PhotoService>();

// 1. Database Configuration (PostgreSQL)
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. JWT Authentication Configuration
var jwtKey = Environment.GetEnvironmentVariable("Jwt__Key") ?? builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is missing");
var jwtIssuer = Environment.GetEnvironmentVariable("Jwt__Issuer") ?? builder.Configuration["Jwt:Issuer"];
var jwtAudience = Environment.GetEnvironmentVariable("Jwt__Audience") ?? builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
        
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                Console.WriteLine($"[Auth] Request Path: {context.Request.Path}");
                if (context.Request.Cookies.ContainsKey("jwt"))
                {
                    context.Token = context.Request.Cookies["jwt"];
                    Console.WriteLine("[Auth] Token found in cookies!");
                }
                else 
                {
                    Console.WriteLine("[Auth] No 'jwt' cookie found. Cookies available: " + string.Join(", ", context.Request.Cookies.Keys));
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// 3. CORS Configuration (อนุญาตให้ Angular เรียก API ได้)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Angular Dev Server
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // สำคัญมาก! สำหรับรับส่ง HttpOnly Cookie (JWT)
    });
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionMiddleware>(); // <-- เสียบปลั๊กตัวดัก Error ตรงนี้ (ด่านแรกสุด)

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c => c.SerializeAsV2 = true); // บังคับให้สร้างคู่มือเวอร์ชัน 2.0 เพื่อแก้บั๊ก Swagger UI งอแง
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");

// Add Authentication before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
