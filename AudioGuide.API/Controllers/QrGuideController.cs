using AudioGuide.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System.Reflection;

namespace AudioGuide.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QrGuideController : ControllerBase
{
    private readonly IAudioGuideService _guideService;

    public QrGuideController(IAudioGuideService guideService)
    {
        _guideService = guideService;
    }

    /// <summary>
    /// Quét mã QR: Trả về trang Web trực quan kèm Trình phát âm thanh và Tọa độ GPS
    /// </summary>
    [HttpGet("scan/{token}")]
    [Produces("text/html")]
    public async Task<IActionResult> Scan(string token, [FromQuery] string? lang = "vi")
    {
        var targetLang = !string.IsNullOrEmpty(lang)
            ? lang
            : (Request.Headers.AcceptLanguage.FirstOrDefault()?.Split(',')[0].Split('-')[0] ?? "vi");

        var poi = await _guideService.ResolveQrScanAsync(token, targetLang);

        if (poi == null)
        {
            return NotFound("<h3>Không tìm thấy địa điểm tham quan hoặc mã QR không hợp lệ.</h3>");
        }

        // Tự động đọc dữ liệu từ DTO bất kể tên thuộc tính
        var props = poi.GetType().GetProperties();
        string GetPropVal(string[] names) =>
            props.FirstOrDefault(p => names.Contains(p.Name, StringComparer.OrdinalIgnoreCase))?.GetValue(poi)?.ToString() ?? "";

        string title = GetPropVal(new[] { "PoiName", "Name", "Title" });
        if (string.IsNullOrEmpty(title)) title = "Địa điểm di tích";

        string desc = GetPropVal(new[] { "Description", "Content", "NarrativeText" });
        if (string.IsNullOrEmpty(desc)) desc = "Chưa có nội dung mô tả chi tiết.";

        string audioSource = GetPropVal(new[] { "AudioUrl", "AudioFileUrl", "Audio", "Url" });
        if (string.IsNullOrEmpty(audioSource))
        {
            audioSource = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3";
        }

        double.TryParse(GetPropVal(new[] { "Latitude", "Lat" }), out double lat);
        double.TryParse(GetPropVal(new[] { "Longitude", "Lng", "Long" }), out double lng);

        if (lat == 0 && lng == 0)
        {
            lat = 10.7725;
            lng = 106.6983;
        }

        string htmlContent = $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{title} - Thuyết Minh Tự Động</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 0; padding: 20px; background: #eef2f7; color: #2d3748; }}
        .card {{ background: white; border-radius: 18px; padding: 24px; box-shadow: 0 10px 25px rgba(0,0,0,0.06); max-width: 480px; margin: auto; }}
        h1 {{ font-size: 22px; margin-top: 5px; color: #1a365d; }}
        .badge {{ display: inline-block; background: #ebf8ff; color: #3182ce; padding: 5px 12px; border-radius: 20px; font-size: 13px; font-weight: 600; margin-bottom: 12px; }}
        p {{ line-height: 1.6; color: #4a5568; font-size: 15px; }}
        .audio-box {{ margin: 22px 0; background: #f7fafc; padding: 18px; border-radius: 14px; border: 1px solid #e2e8f0; text-align: center; }}
        .play-btn {{ display: inline-block; width: 100%; padding: 14px 0; font-size: 16px; font-weight: bold; color: white; background: #e53e3e; border: none; border-radius: 10px; cursor: pointer; margin-bottom: 12px; }}
        audio {{ width: 100%; margin-top: 10px; outline: none; }}
        .gps-box {{ background: #f0fff4; border-left: 4px solid #38a169; padding: 12px 14px; border-radius: 8px; margin: 20px 0; font-size: 14px; }}
        .map-btn {{ display: block; width: 100%; text-align: center; background: #3182ce; color: white; padding: 14px 0; border-radius: 12px; text-decoration: none; font-weight: 600; box-sizing: border-box; }}
    </style>
</head>
<body>
    <div class='card'>
        <span class='badge'>📍 Audio Guide</span>
        <h1>{title}</h1>
        <p id='descText'>{desc}</p>

        <div class='audio-box'>
            <button class='play-btn' id='btnAudio' onclick='togglePlayAudio()'>▶ Bấm để nghe thuyết minh</button>
            <audio id='mainAudio' controls preload='auto'>
                <source src='{audioSource}' type='audio/mpeg'>
            </audio>
        </div>

        <div class='gps-box'>
            🌐 <strong>Tọa độ GPS:</strong> {lat:F5}, {lng:F5}
        </div>

        <a class='map-btn' href='https://www.google.com/maps?q={lat},{lng}' target='_blank'>
            🗺 Chỉ đường trên Google Maps
        </a>
    </div>

    <script>
        const audio = document.getElementById('mainAudio');
        const btn = document.getElementById('btnAudio');
        let isSpeaking = false;

        function togglePlayAudio() {{
            // 1. Thử phát file audio nếu nguồn tải thành công
            if (audio.src && audio.error == null) {{
                if (audio.paused) {{
                    audio.play().then(() => {{
                        btn.innerText = '⏸ Đang phát âm thanh...';
                        btn.style.background = '#38a169';
                    }}).catch(err => {{
                        speakText(); // Nếu lỗi trình duyệt chặn, dùng giọng đọc máy
                    }});
                }} else {{
                    audio.pause();
                    btn.innerText = '▶ Bấm để tiếp tục nghe';
                    btn.style.background = '#e53e3e';
                }}
            }} else {{
                speakText();
            }}
        }}

        // 2. Tự động đọc bằng Text-To-Speech tiếng Việt của điện thoại nếu không có file âm thanh
        function speakText() {{
            if ('speechSynthesis' in window) {{
                if (isSpeaking) {{
                    window.speechSynthesis.cancel();
                    isSpeaking = false;
                    btn.innerText = '▶ Bấm để nghe thuyết minh';
                    btn.style.background = '#e53e3e';
                    return;
                }}

                const text = document.getElementById('descText').innerText;
                const utterance = new SpeechSynthesisUtterance(text);
                utterance.lang = 'vi-VN';
                utterance.rate = 0.95;

                utterance.onend = () => {{
                    isSpeaking = false;
                    btn.innerText = '▶ Nghe lại';
                    btn.style.background = '#e53e3e';
                }};

                window.speechSynthesis.speak(utterance);
                isSpeaking = true;
                btn.innerText = '⏸ Đang đọc bài thuyết minh...';
                btn.style.background = '#38a169';
            }} else {{
                alert('Trình duyệt chưa hỗ trợ phát file âm thanh này.');
            }}
        }}
    </script>
</body>
</html>";

        return Content(htmlContent, "text/html; charset=utf-8");
    }

    /// <summary>
    /// Sinh ảnh mã QR để in ra bảng di tích
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