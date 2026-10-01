using AudioGuide.Core.DTOs;
using AudioGuide.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

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
    /// Quét mã QR để lấy nội dung thuyết minh tương ứng
    /// </summary>
    /// <param name="token">Mã QR in trên bảng di tích (vd: qr-ddl-01)</param>
    /// <param name="lang">Mã ngôn ngữ tùy chọn (vi, en), nếu không truyền sẽ lấy từ header Accept-Language</param>
    [HttpGet("scan/{token}")]
    public async Task<ActionResult<PoiNarrativeDto>> Scan(string token, [FromQuery] string? lang)
    {
        var targetLang = lang ?? Request.Headers.AcceptLanguage.FirstOrDefault();
        var narrative = await _guideService.ResolveQrScanAsync(token, targetLang);

        if (narrative == null)
            return NotFound(new { message = "Không tìm thấy điểm tham quan hoặc mã QR không tồn tại." });

        return Ok(narrative);
    }
}