using AudioGuide.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;

namespace AudioGuide.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QrGuideController(AppDbContext context, IHttpClientFactory httpClientFactory) : ControllerBase
{
    private readonly AppDbContext _context = context;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    // Danh sách 5 ngôn ngữ chuẩn được hỗ trợ toàn hệ thống
    private static readonly Dictionary<string, (string Label, string GoogleLang)> SupportedLanguages = new()
    {
        { "vi", ("🇻🇳 Tiếng Việt", "vi") },
        { "zh", ("🇨🇳 中文 (Chinese)", "zh-CN") },
        { "en", ("🇺🇸 English (US)", "en") },
        { "fr", ("🇫🇷 Français", "fr") },
        { "ru", ("🇷🇺 Русский", "ru") }
    };

    /// <summary>
    /// API chia nhỏ đoạn văn dài thành các câu rồi ghép nối các đoạn MP3 lại với nhau,
    /// đảm bảo phát trọn vẹn bài thuyết minh dài 30s - 2 phút không bị giới hạn.
    /// </summary>
    [HttpGet("audio")]
    public async Task<IActionResult> GetTtsAudio([FromQuery] string text, [FromQuery] string lang = "vi")
    {
        if (string.IsNullOrWhiteSpace(text)) return BadRequest();

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            string targetTtsLang = SupportedLanguages.TryGetValue(lang.ToLower(), out var langInfo) ? langInfo.GoogleLang : "vi";

            // Tách văn bản dài theo các dấu câu để ngắt câu tự nhiên
            var sentences = text.Split(new[] { '.', '!', '?', ';', '\n', '。', '！', '？' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .Where(s => !string.IsNullOrEmpty(s))
                                .ToList();

            if (sentences.Count == 0) sentences.Add(text.Trim());

            using var memoryStream = new MemoryStream();

            foreach (var chunk in sentences)
            {
                // Tránh chunk quá dài vượt ngưỡng của endpoint
                string safeChunk = chunk.Length > 150 ? chunk[..150] : chunk;
                string encoded = HttpUtility.UrlEncode(safeChunk);
                string ttsUrl = $"https://translate.google.com/translate_tts?ie=UTF-8&tl={targetTtsLang}&client=tw-ob&q={encoded}";

                var response = await client.GetAsync(ttsUrl);
                if (response.IsSuccessStatusCode)
                {
                    byte[] chunkBytes = await response.Content.ReadAsByteArrayAsync();
                    await memoryStream.WriteAsync(chunkBytes, 0, chunkBytes.Length);
                }
            }

            byte[] fullAudioBytes = memoryStream.ToArray();
            Response.Headers.Append("Accept-Ranges", "bytes");
            return File(fullAudioBytes, "audio/mpeg", enableRangeProcessing: true);
        }
        catch
        {
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Giao diện Web hiển thị thông tin thuyết minh khi quét QR bằng điện thoại
    /// </summary>
    [HttpGet("scan/{token}")]
    [Produces("text/html")]
    public async Task<IActionResult> Scan(string token, [FromQuery] string? lang = "vi")
    {
        string normToken = string.IsNullOrWhiteSpace(token) ? string.Empty : token.Trim();
        string targetLang = string.IsNullOrWhiteSpace(lang) ? "vi" : lang.Trim().ToLower();

        if (!SupportedLanguages.ContainsKey(targetLang))
        {
            targetLang = "vi";
        }

        // 1. Tìm theo mã QR Token
        var qr = await _context.QrCodes
            .Include(q => q.Poi)
            .ThenInclude(p => p.Translations)
            .FirstOrDefaultAsync(q => q.QrToken.ToLower() == normToken.ToLower());

        var poi = qr?.Poi;

        // 2. Tìm dự phòng theo 5 địa điểm mới nếu quét theo mã/ID
        poi ??= await _context.Pois
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p =>
                p.Id.ToString() == normToken ||
                p.Code.ToLower() == normToken.ToLower() ||
                (normToken.Contains("ben thanh", StringComparison.OrdinalIgnoreCase) && p.Id == 1) ||
                (normToken.Contains("nha hat", StringComparison.OrdinalIgnoreCase) && p.Id == 2) ||
                (normToken.Contains("chung tich", StringComparison.OrdinalIgnoreCase) && p.Id == 3) ||
                (normToken.Contains("landmark", StringComparison.OrdinalIgnoreCase) && p.Id == 4) ||
                (normToken.Contains("nha rong", StringComparison.OrdinalIgnoreCase) && p.Id == 5));

        if (poi == null)
        {
            return NotFound("<h3 style='font-family: sans-serif; text-align: center; margin-top: 50px;'>Không tìm thấy địa điểm tham quan hoặc mã QR không tồn tại.</h3>");
        }

        // 3. Lấy bản dịch theo ngôn ngữ người dùng chọn
        var translations = poi.Translations ?? Enumerable.Empty<Core.Entities.PoiTranslation>();
        var trans = translations.FirstOrDefault(t => string.Equals(t.LanguageCode, targetLang, StringComparison.OrdinalIgnoreCase))
                 ?? translations.FirstOrDefault(t => string.Equals(t.LanguageCode, "vi", StringComparison.OrdinalIgnoreCase))
                 ?? translations.FirstOrDefault();

        string title = trans?.Title ?? "Điểm Tham Quan";
        string desc = trans?.Description ?? "Nội dung thuyết minh đang được cập nhật.";

        // Link audio stream từ endpoint API
        string safeDesc = HttpUtility.UrlEncode(desc);
        string audioStreamUrl = $"{Request.Scheme}://{Request.Host}/api/QrGuide/audio?text={safeDesc}&lang={targetLang}";

        // Tọa độ GPS
        double lat = poi.Location != null ? poi.Location.Y : 10.7725;
        double lng = poi.Location != null ? poi.Location.X : 106.6983;

        // Render danh sách 5 tùy chọn ngôn ngữ cho dropdown
        var langOptionsHtml = new StringBuilder();
        foreach (var (code, info) in SupportedLanguages)
        {
            string selected = (code == targetLang) ? "selected" : "";
            langOptionsHtml.AppendLine($"<option value='{code}' {selected}>{info.Label}</option>");
        }

        string gpsLabel = targetLang switch
        {
            "zh" => "GPS坐标",
            "en" => "GPS Coordinates",
            "fr" => "Coordonnées GPS",
            "ru" => "GPS Координаты",
            _ => "Tọa độ GPS"
        };

        string mapBtnLabel = targetLang switch
        {
            "zh" => "在谷歌地图中打开",
            "en" => "Open in Google Maps",
            "fr" => "Ouvrir dans Google Maps",
            "ru" => "Открыть на Google Картах",
            _ => "Chỉ đường trên Google Maps"
        };

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
            margin-bottom: 16px; 
        }}
        .badge {{ 
            background: #e7f3ff; 
            color: #1877f2; 
            padding: 6px 14px; 
            border-radius: 20px; 
            font-size: 13px; 
            font-weight: 600; 
        }}
        .lang-select {{
            background: #f0f2f5;
            border: 1px solid #dcdfe3;
            border-radius: 12px;
            padding: 6px 10px;
            font-size: 13px;
            font-weight: 600;
            color: #4b4f56;
            outline: none;
            cursor: pointer;
        }}
        h1 {{ 
            font-size: 22px; 
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
            <select class='lang-select' onchange='window.location.search = ""?lang="" + this.value'>
                {langOptionsHtml}
            </select>
        </div>
        
        <h1>{title}</h1>
        <p>{desc}</p>

        <div class='audio-wrapper'>
            <audio controls preload='metadata' autoplay>
                <source src='{audioStreamUrl}' type='audio/mpeg'>
                Trình duyệt không hỗ trợ nghe thuyết minh.
            </audio>
        </div>

        <div class='gps-box'>
            🌐 <strong>{gpsLabel}:</strong> {lat:F5}, {lng:F5}
        </div>

        <a class='map-btn' href='https://www.google.com/maps?q={lat},{lng}' target='_blank'>
            🗺 {mapBtnLabel}
        </a>
    </div>
</body>
</html>";

        return Content(htmlContent, "text/html; charset=utf-8");
    }

    /// <summary>
    /// API tạo ảnh QR Code
    /// </summary>
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