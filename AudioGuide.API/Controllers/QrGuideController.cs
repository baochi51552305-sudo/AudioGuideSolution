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
        string targetLang = string.IsNullOrWhiteSpace(lang) ? "vi" : lang.Trim();

        // 1. Tìm theo mã QR Token
        var qr = await _context.QrCodes
            .Include(q => q.Poi)
            .ThenInclude(p => p.Translations)
            .FirstOrDefaultAsync(q => q.QrToken.ToLower() == normToken.ToLower());

        var poi = qr?.Poi;

        // 2. Dự phòng tìm theo ID hoặc Tên/Code
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

        // 3. Lấy thông tin bản dịch
        var translations = poi.Translations ?? Enumerable.Empty<Core.Entities.PoiTranslation>();
        var trans = translations.FirstOrDefault(t => string.Equals(t.LanguageCode, targetLang, StringComparison.OrdinalIgnoreCase))
                 ?? translations.FirstOrDefault(t => string.Equals(t.LanguageCode, "vi", StringComparison.OrdinalIgnoreCase))
                 ?? translations.FirstOrDefault();

        string title = trans?.Title ?? "Điểm Tham Quan";
        string desc = trans?.Description ?? "Nội dung thuyết minh đang được cập nhật.";

        // Tọa độ GPS an toàn
        double lat = poi.Location != null ? poi.Location.Y : 10.7770;
        double lng = poi.Location != null ? poi.Location.X : 106.6953;

        string htmlContent = $@"
<!DOCTYPE html>
<html lang='{targetLang}'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{title} - Thuyết Minh Tự Động</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 0; padding: 20px; background: #eef2f7; color: #2d3748; }}
        .card {{ background: white; border-radius: 18px; padding: 24px; box-shadow: 0 10px 25px rgba(0,0,0,0.06); max-width: 480px; margin: auto; }}
        h1 {{ font-size: 22px; margin-top: 5px; color: #1a365d; }}
        .badge {{ display: inline-block; background: #ebf8ff; color: #3182ce; padding: 5px 12px; border-radius: 20px; font-size: 13px; font-weight: 600; margin-bottom: 12px; }}
        p {{ line-height: 1.6; color: #4a5568; font-size: 15px; text-align: justify; }}
        .audio-box {{ margin: 22px 0; background: #f7fafc; padding: 18px; border-radius: 14px; border: 1px solid #e2e8f0; text-align: center; }}
        .play-btn {{ width: 100%; padding: 14px; font-size: 16px; font-weight: 600; color: white; background: #3182ce; border: none; border-radius: 10px; cursor: pointer; transition: 0.2s; }}
        .play-btn:active {{ transform: scale(0.98); }}
        .gps-box {{ background: #f0fff4; border-left: 4px solid #38a169; padding: 12px 14px; border-radius: 8px; margin: 20px 0; font-size: 14px; }}
        .map-btn {{ display: block; width: 100%; text-align: center; background: #2f855a; color: white; padding: 14px 0; border-radius: 12px; text-decoration: none; font-weight: 600; box-sizing: border-box; }}
    </style>
</head>
<body>
    <div class='card'>
        <span class='badge'>📍 Audio Guide</span>
        <h1>{title}</h1>
        <p id='descText'>{desc}</p>

        <div class='audio-box'>
            <button class='play-btn' id='speakBtn' onclick='toggleSpeech()'>🔊 Bấm để nghe thuyết minh</button>
        </div>

        <div class='gps-box'>
            🌐 <strong>Tọa độ GPS:</strong> {lat:F5}, {lng:F5}
        </div>

        <a class='map-btn' href='https://www.google.com/maps?q={lat},{lng}' target='_blank'>
            🗺 Chỉ đường trên Google Maps
        </a>
    </div>

    <script>
        let isSpeaking = false;
        const btn = document.getElementById('speakBtn');

        function toggleSpeech() {{
            if (!('speechSynthesis' in window)) {{
                alert('Trình duyệt không hỗ trợ phát âm thanh trực tiếp.');
                return;
            }}

            if (isSpeaking) {{
                window.speechSynthesis.cancel();
                isSpeaking = false;
                btn.innerText = '🔊 Bấm để nghe thuyết minh';
                btn.style.background = '#3182ce';
                return;
            }}

            const text = document.getElementById('descText').innerText;
            const utter = new SpeechSynthesisUtterance(text);
            utter.lang = '{targetLang}'.toLowerCase() === 'en' ? 'en-US' : 'vi-VN';
            utter.rate = 0.95;

            const voices = window.speechSynthesis.getVoices();
            const matchingVoice = voices.find(v => v.lang.toLowerCase().startsWith(utter.lang.toLowerCase().substring(0, 2)));
            if (matchingVoice) utter.voice = matchingVoice;

            utter.onend = () => {{
                isSpeaking = false;
                btn.innerText = '🔊 Nghe lại thuyết minh';
                btn.style.background = '#3182ce';
            }};

            window.speechSynthesis.cancel();
            window.speechSynthesis.speak(utter);
            isSpeaking = true;
            btn.innerText = '⏸ Đang phát... Nhấn để dừng';
            btn.style.background = '#e53e3e';
        }}

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