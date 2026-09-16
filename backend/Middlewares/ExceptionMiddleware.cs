using System.Net;
using System.Text.Json;

namespace JamineERP.Backend.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // ปล่อยให้ API ทำงานไปตามปกติ
            await _next(context); 
        }
        catch (Exception ex)
        {
            // ถ้ามี Error โผล่ขึ้นมา ให้จับมันไว้ตรงนี้!
            _logger.LogError(ex, ex.Message); // พิมพ์ลง Terminal สีแดงๆ
            
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError; // ส่ง Status 500 กลับไป

            // สร้างก้อน JSON สวยๆ ส่งกลับไปให้ Angular
            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = _env.IsDevelopment() ? ex.Message : "เกิดข้อผิดพลาดที่เซิร์ฟเวอร์",
                Details = _env.IsDevelopment() ? ex.StackTrace?.ToString() : null
            };

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(response, options);

            await context.Response.WriteAsync(json);
        }
    }
}
