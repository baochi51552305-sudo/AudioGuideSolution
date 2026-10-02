using AudioGuide.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System;
using System.Linq;
using System.Net.Http;
using System.Web;

namespace AudioGuide.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QrGuideController(AppDbContext context, IHttpClientFactory httpClientFactory) : ControllerBase
{
    private readonly AppDbContext _context = context;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    /// <summary>
    /// API trả về toàn bộ dữ liệu file MP3 để trình duyệt tính đúng tổng thời lượng (0:05 / 0:18) và tua được
    /// </summary>
    [HttpGet("audio")]
    public async Task<IActionResult> GetTtsAudio([FromQuery] string text, [FromQuery] string lang = "vi")
    {
        if (string.IsNullOrWhiteSpace(text)) return BadRequest();

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            // Giới hạn câu đọc chuẩn ngữ điệu TTS
            string cleanText = text.Length > 200 ? text[..195] + "..." : text;
            string encoded = HttpUtility.UrlEncode(cleanText);
            string ttsUrl = $"https://translate.google.com/translate_tts?ie=UTF-8&tl={lang}&client=tw-ob&q={encoded}";

            // Tải trọn vẹn mảng byte để có Content-Length cố định
            byte[] audioBytes = await client.GetByteArrayAsync(ttsUrl);

            // Bật Range Processing để điện thoại kéo tua và nhận diện đúng thanh thời lượng
            Response.Headers.Append("Accept-Ranges", "bytes");
            return File(audioBytes, "audio/mpeg", enableRangeProcessing: true);
        }
        catch
        {
            return StatusCode(500);
        }
    }

    [HttpGet("scan/{token}")]
    [Produces("text/html")]
    public async Task<IActionResult> Scan(string token, [FromQuery] string? lang = "vi")
    {
        string normToken = string.IsNullOrWhiteSpace(token) ? string.Empty : token.Trim();
        string targetLang = string.IsNullOrWhiteSpace(lang) ? "vi" : lang.Trim().ToLower();

        // 1. Tìm theo mã QR Token
        var qr = await _context.QrCodes
            .Include(q => q.Poi)
            .ThenInclude(p => p.Translations)
            .FirstOrDefaultAsync(q => q.QrToken.ToLower() == normToken.ToLower());

        var poi = qr?.Poi;

        // 2. Tìm dự phòng theo ID hoặc Tên/Code
        poi ??= await _context.Pois
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p =>
                p.Id.ToString() == normToken ||
                p.Code.ToLower() == normToken.ToLower() ||
                (normToken.Contains("dinh", StringComparison.OrdinalIgnoreCase) && p.Id == 1) ||
                (normToken.Contains("duc ba", StringComparison.OrdinalIgnoreCase) && p.Id == 2) ||
                (normToken.Contains("buu dien", StringComparison.OrdinalIgnoreCase) && p.Id == 3));

        if (poi == null)
        {
            return NotFound("<h3 style='font-family: sans-serif; text-align: center; margin-top: 50px;'>Không tìm thấy địa điểm tham quan hoặc mã QR không tồn tại.</h3>");
        }

        // 3. Lấy bản dịch tương ứng
        var translations = poi.Translations ?? Enumerable.Empty<Core.Entities.PoiTranslation>();
        var trans = translations.FirstOrDefault(t => string.Equals(t.LanguageCode, targetLang, StringComparison.OrdinalIgnoreCase))
                 ?? translations.FirstOrDefault(t => string.Equals(t.LanguageCode, "vi", StringComparison.OrdinalIgnoreCase))
                 ?? translations.FirstOrDefault();

        string title = trans?.Title ?? "Điểm Tham Quan";
        string desc = trans?.Description ?? "Nội dung thuyết minh đang được cập nhật.";

        // URL stream MP3 trực tiếp từ backend
        string safeDesc = HttpUtility.UrlEncode(desc);
        string audioStreamUrl = $"{Request.Scheme}://{Request.Host}/api/QrGuide/audio?text={safeDesc}&lang={targetLang}";

        // Tọa độ GPS chính xác
        double lat = poi.Location != null ? poi.Location.Y : 10.7770;
        double lng = poi.Location != null ? poi.Location.X : 106.6953;

        string otherLang = targetLang == "vi" ? "en" : "vi";
        string otherLangLabel = targetLang == "vi" ? "🇬🇧 English" : "🇻🇳 Tiếng Việt";

        string htmlContent = $@"
<!DOCTYPE html>
<html lang='{targetLang}'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{title} - Audio Guide</title>
    <style>
        body {{ 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; 
            margin: 0; 
            padding: 16px; 
            background: #f0f2f5; 
            color: #1c1e21; 
        }}
        .card {{ 
            background: #ffffff; 
            border-radius: 20px; 
            padding: 24px; 
            box-shadow: 0 4px 16px rgba(0,0,0,0.08); 
            max-width: 480px; 
            margin: 0 auto; 
        }}
        .header-row {{ 
            display: flex; 
            justify-content: space-between; 
            align-items: center; 
            margin-bottom: 12px; 
        }}
        .badge {{ 
            background: #e7f3ff; 
            color: #1877f2; 
            padding: 6px 14px; 
            border-radius: 20px; 
            font-size: 13px; 
            font-weight: 600; 
        }}
        .lang-switch {{ 
            text-decoration: none; 
            color: #4b4f56; 
            background: #f0f2f5; 
            padding: 6px 12px; 
            border-radius: 12px; 
            font-size: 13px; 
            font-weight: 600; 
        }}
        h1 {{ 
            font-size: 24px; 
            font-weight: 700; 
            margin: 8px 0 14px 0; 
            color: #050505; 
        }}
        p {{ 
            line-height: 1.6; 
            color: #4b4f56; 
            font-size: 15px; 
            text-align: justify; 
            margin-bottom: 20px; 
        }}
        .audio-wrapper {{
            background: #f1f3f4;
            border-radius: 30px;
            padding: 6px 10px;
            margin: 20px 0;
            display: flex;
            align-items: center;
            justify-content: center;
            box-shadow: inset 0 1px 2px rgba(0,0,0,0.05);
        }}
        audio {{ 
            width: 100%; 
            height: 48px;
            outline: none;
            border-radius: 30px;
        }}
        .gps-box {{ 
            background: #e8f5e9; 
            border-left: 4px solid #2e7d32; 
            padding: 12px 14px; 
            border-radius: 8px; 
            margin: 18px 0; 
            font-size: 14px; 
            color: #1b5e20;
        }}
        .map-btn {{ 
            display: block; 
            width: 100%; 
            text-align: center; 
            background: #1877f2; 
            color: #ffffff; 
            padding: 14px 0; 
            border-radius: 12px; 
            text-decoration: none; 
            font-weight: 600; 
            box-sizing: border-box; 
        }}
    </style>
</head>
<body>
    <div class='card'>
        <div class='header-row'>
            <span class='badge'>📍 Audio Guide</span>
            <a class='lang-switch' href='?lang={otherLang}'>{otherLangLabel}</a>
        </div>
        
        <h1>{title}</h1>
        <p>{desc}</p>

        <div class='audio-wrapper'>
            <audio controls preload='metadata'>
                <source src='{audioStreamUrl}' type='audio/mpeg'>
                Trình duyệt của bạn không hỗ trợ trình phát âm thanh.
            </audio>
        </div>

        <div class='gps-box'>
            🌐 <strong>{(targetLang == "vi" ? "Tọa độ GPS" : "GPS Coordinates")}:</strong> {lat:F5}, {lng:F5}
        </div>

        <a class='map-btn' href='https://www.google.com/maps?q={lat},{lng}' target='_blank'>
            🗺 {(targetLang == "vi" ? "Chỉ đường trên Google Maps" : "Open in Google Maps")}
        </a>
    </div>
</body>
</html>";

        return Content(htmlContent, "text/html; charset=utf-8");
    }

    [HttpGet("generate/{token}")]
    public IActionResult GenerateQr(string token, [FromQuery] string? lang = "vi")
    {
        var requestUrl = $"{Request.Scheme}://{Request.Host}/api/QrGuide/scan/{token}?lang={lang}";

        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(requestUrl, QRCodeGenerator.ECCLevel.Q);

        var qrCode = new PngByteQRCode(qrCodeData);
        byte[] qrCodeBytes = qrCode.GetGraphic(20);

        return File(qrCodeBytes, "image/png");
    }
}