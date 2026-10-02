using AudioGuide.Core.Interfaces;
using AudioGuide.Core.Services;
using AudioGuide.Infrastructure.Data;
using AudioGuide.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Database chạy trực tiếp trên RAM (loại bỏ hoàn toàn phụ thuộc vào SQLite và mod_spatialite.so)
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseInMemoryDatabase("AudioGuideDb");
});

// 2. Đăng ký Dependency Injection (IoC)
builder.Services.AddScoped<IPoiRepository, PoiRepository>();
builder.Services.AddScoped<IAudioGuideService, AudioGuideService>();

// 3. Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVercelAndLocal", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Tự động khởi tạo database và nạp dữ liệu mẫu vào bộ nhớ
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// 4. Kích hoạt Swagger UI
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowVercelAndLocal");

app.UseAuthorization();
app.MapControllers();

// Health check endpoint cho Render
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

app.Run();