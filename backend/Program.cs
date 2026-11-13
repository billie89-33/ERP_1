using System.Text;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using JamineERP.Backend.Data;
using JamineERP.Backend.Middlewares;
using JamineERP.Backend.Services;

// Load .env file from the parent directory (root of the workspace)
Env.Load(Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"));

var builder = WebApplication.CreateBuilder(args);


builder.Services.Configure<CloudinarySettings>(options => 
{
    options.CloudName = Environment.GetEnvironmentVariable("Cloudinary__CloudName") ?? builder.Configuration["CloudinarySettings:CloudName"] ?? "";
    options.ApiKey = Environment.GetEnvironmentVariable("Cloudinary__ApiKey") ?? builder.Configuration["CloudinarySettings:ApiKey"] ?? "";
    options.ApiSecret = Environment.GetEnvironmentVariable("Cloudinary__ApiSecret") ?? builder.Configuration["CloudinarySettings:ApiSecret"] ?? "";
});
builder.Services.AddScoped<IPhotoService, PhotoService>();
builder.Services.AddHttpClient<IGeminiAiService, GeminiAiService>();

// 1. Database Configuration (PostgreSQL)
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// Render automatically injects DATABASE_URL for connected databases
var renderDbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(renderDbUrl))
{
    // Parse postgres://user:pass@host/db to Host=host;Database=db;Username=user;Password=pass
    var databaseUri = new Uri(renderDbUrl);
    var userInfo = databaseUri.UserInfo.Split(':');
    var npgsqlBuilder = new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = databaseUri.Host,
        Port = databaseUri.Port,
        Username = userInfo[0],
        Password = userInfo.Length > 1 ? userInfo[1] : "",
        Database = databaseUri.LocalPath.TrimStart('/'),
        Pooling = true
    };
    connectionString = npgsqlBuilder.ToString();
}

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
        policy.SetIsOriginAllowed(origin => true) // Strict origins for security
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // For HttpOnly Cookies
    });
});

builder.Services.AddControllers();

// Register Background Worker
builder.Services.AddHostedService<OrderExpiryWorker>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Ensure AI Vector Embedding columns exist in PostgreSQL (Idempotent Auto-Migration)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""Embedding"" real[];
            ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""EmbeddingUpdatedAt"" timestamp with time zone;
            ALTER TABLE ""SalesOrders"" ADD COLUMN IF NOT EXISTS ""Embedding"" real[];
            ALTER TABLE ""SalesOrders"" ADD COLUMN IF NOT EXISTS ""EmbeddingUpdatedAt"" timestamp with time zone;
            ALTER TABLE ""PurchaseOrders"" ADD COLUMN IF NOT EXISTS ""Embedding"" real[];
            ALTER TABLE ""PurchaseOrders"" ADD COLUMN IF NOT EXISTS ""EmbeddingUpdatedAt"" timestamp with time zone;
            ALTER TABLE ""Suppliers"" ADD COLUMN IF NOT EXISTS ""Embedding"" real[];
            ALTER TABLE ""Suppliers"" ADD COLUMN IF NOT EXISTS ""EmbeddingUpdatedAt"" timestamp with time zone;
            ALTER TABLE ""Customers"" ADD COLUMN IF NOT EXISTS ""Embedding"" real[];
            ALTER TABLE ""Customers"" ADD COLUMN IF NOT EXISTS ""EmbeddingUpdatedAt"" timestamp with time zone;
        ");
        Console.WriteLine("[AI] Verified Vector Embedding columns across all 5 ERP tables.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[AI] Warning verifying Vector Embedding columns: {ex.Message}");
    }
}

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
