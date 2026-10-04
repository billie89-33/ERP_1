using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace JamineERP.Backend.Middlewares
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context); // ปล่อยให้ API ทำงานปกติ
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดขึ้นในระบบ (Unhandled Exception)");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            // ดึง InnerException ออกมาถ้ามี เพื่อให้รู้สาเหตุลึกๆ
            var message = exception.InnerException?.Message ?? exception.Message;

            var result = JsonSerializer.Serialize(new
            {
                message = "เกิดข้อผิดพลาดที่เซิร์ฟเวอร์",
                error = message, // ส่ง error จริงกลับไป (ในการใช้งานจริงอาจซ่อนไว้ถ้าเป็น Production แต่โปรเจกต์นี้เปิดไว้ดูบั๊ก)
                stackTrace = exception.StackTrace
            });

            return context.Response.WriteAsync(result);
        }
    }
}
