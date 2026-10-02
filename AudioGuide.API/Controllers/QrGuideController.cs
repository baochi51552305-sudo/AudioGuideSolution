using AudioGuide.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System;
using System.Linq;

namespace AudioGuide.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QrGuideController(AppDbContext context) : ControllerBase
{
    private readonly AppDbContext _context = context;

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

        // 3. Lấy bản dịch tương ứng theo ngôn ngữ
        var translations = poi.Translations ?? Enumerable.Empty<Core.Entities.PoiTranslation>();
        var trans = translations.FirstOrDefault(t => string.Equals(t.LanguageCode, targetLang, StringComparison.OrdinalIgnoreCase))
                 ?? translations.FirstOrDefault(t => string.Equals(t.LanguageCode, "vi", StringComparison.OrdinalIgnoreCase))
                 ?? translations.FirstOrDefault();

        string title = trans?.Title ?? "Điểm Tham Quan";
        string desc = trans?.Description ?? "Nội dung thuyết minh đang được cập nhật.";
        string audioUrl = trans?.AudioUrl ?? string.Empty;

        // Tọa độ GPS chính xác
        double lat = poi.Location != null ? poi.Location.Y : 10.7770;
        double lng = poi.Location != null ? poi.Location.X : 106.6953;

        string otherLang = targetLang == "vi" ? "en" : "vi";
        string otherLangLabel = targetLang == "vi" ? "🇬🇧 English Version" : "🇻🇳 Phiên bản Tiếng Việt";

        string htmlContent = $@"
<!DOCTYPE html>
<html lang='{targetLang}'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{title} - Audio Guide</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 0; padding: 20px; background: #eef2f7; color: #2d3748; }}
        .card {{ background: white; border-radius: 18px; padding: 24px; box-shadow: 0 10px 25px rgba(0,0,0,0.06); max-width: 480px; margin: auto; }}
        .top-bar {{ display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px; }}
        .badge {{ background: #ebf8ff; color: #3182ce; padding: 5px 12px; border-radius: 20px; font-size: 13px; font-weight: 600; }}
        .lang-switch {{ font-size: 13px; text-decoration: none; color: #4a5568; background: #edf2f7; padding: 5px 10px; border-radius: 8px; font-weight: 500; }}
        h1 {{ font-size: 22px; margin: 8px 0 14px 0; color: #1a365d; }}
        p {{ line-height: 1.65; color: #4a5568; font-size: 15px; text-align: justify; }}
        .audio-player-box {{ margin: 20px 0; background: #f7fafc; padding: 18px; border-radius: 14px; border: 1px solid #e2e8f0; text-align: center; }}
        .play-control-btn {{ width: 100%; padding: 14px; font-size: 16px; font-weight: 600; color: white; background: #3182ce; border: none; border-radius: 10px; cursor: pointer; transition: 0.2s; display: flex; align-items: center; justify-content: center; gap: 8px; }}
        .play-control-btn:active {{ transform: scale(0.98); }}
        .gps-box {{ background: #f0fff4; border-left: 4px solid #38a169; padding: 12px 14px; border-radius: 8px; margin: 20px 0; font-size: 14px; }}
        .map-btn {{ display: block; width: 100%; text-align: center; background: #2f855a; color: white; padding: 14px 0; border-radius: 12px; text-decoration: none; font-weight: 600; box-sizing: border-box; }}
    </style>
</head>
<body>
    <div class='card'>
        <div class='top-bar'>
            <span class='badge'>📍 Audio Guide</span>
            <a class='lang-switch' href='?lang={otherLang}'>{otherLangLabel}</a>
        </div>
        
        <h1>{title}</h1>
        <p id='descText'>{desc}</p>

        <div class='audio-player-box'>
            <button class='play-control-btn' id='mainBtn' onclick='toggleAudio()'>
                ▶ {(targetLang == "vi" ? "Nghe thuyết minh tiếng Việt" : "Listen to English Narration")}
            </button>
        </div>

        <div class='gps-box'>
            🌐 <strong>{(targetLang == "vi" ? "Tọa độ GPS" : "GPS Coordinates")}:</strong> {lat:F5}, {lng:F5}
        </div>

        <a class='map-btn' href='https://www.google.com/maps?q={lat},{lng}' target='_blank'>
            🗺 {(targetLang == "vi" ? "Chỉ đường trên Google Maps" : "Open in Google Maps")}
        </a>
    </div>

    <script>
        let isPlaying = false;
        const btn = document.getElementById('mainBtn');
        const isEnglish = '{targetLang}' === 'en';

        function toggleAudio() {{
            if (!('speechSynthesis' in window)) {{
                alert(isEnglish ? 'Your browser does not support audio playback.' : 'Trình duyệt không hỗ trợ phát âm thanh.');
                return;
            }}

            if (isPlaying) {{
                window.speechSynthesis.cancel();
                isPlaying = false;
                btn.innerHTML = isEnglish ? '▶ Listen to English Narration' : '▶ Nghe thuyết minh tiếng Việt';
                btn.style.background = '#3182ce';
                return;
            }}

            const text = document.getElementById('descText').innerText;
            const utter = new SpeechSynthesisUtterance(text);
            utter.lang = isEnglish ? 'en-US' : 'vi-VN';
            utter.rate = 0.95;

            // Tìm giọng chuẩn của thiết bị
            const voices = window.speechSynthesis.getVoices();
            const targetPrefix = isEnglish ? 'en' : 'vi';
            const voice = voices.find(v => v.lang.toLowerCase().startsWith(targetPrefix));
            if (voice) utter.voice = voice;

            utter.onend = () => {{
                isPlaying = false;
                btn.innerHTML = isEnglish ? '▶ Replay Narration' : '▶ Nghe lại thuyết minh';
                btn.style.background = '#3182ce';
            }};

            window.speechSynthesis.cancel();
            window.speechSynthesis.speak(utter);
            isPlaying = true;
            btn.innerHTML = isEnglish ? '⏸ Playing... Tap to Pause' : '⏸ Đang phát thuyết minh... Nhấn để dừng';
            btn.style.background = '#e53e3e';
        }}

        // Đảm bảo load giọng nói ngay khi trang vừa mở
        if ('speechSynthesis' in window) {{
            window.speechSynthesis.onvoiceschanged = () => window.speechSynthesis.getVoices();
        }}
    </script>
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