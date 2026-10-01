using AudioGuide.Core.Interfaces;
using AudioGuide.Core.Services;
using AudioGuide.Infrastructure.Data;
using AudioGuide.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Connection String SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Server=localhost;Database=AudioGuideDb;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        // Kích hoạt hỗ trợ địa lý NetTopologySuite trên SQL Server
        sqlOptions.UseNetTopologySuite();
    }));

// 2. Đăng ký Dependency Injection (IoC)
builder.Services.AddScoped<IPoiRepository, PoiRepository>();
builder.Services.AddScoped<IAudioGuideService, AudioGuideService>();

// 3. Cấu hình CORS để Front-End trên Vercel gọi được vào Back-End trên Render
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVercelAndLocal", policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // Cho phép cả localhost và domain Vercel
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 4. Kích hoạt Swagger UI
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowVercelAndLocal");

app.UseAuthorization();
app.MapControllers();

// Endpoint kiểm tra sức khỏe hệ thống (Health check phục vụ Render)
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

app.Run();