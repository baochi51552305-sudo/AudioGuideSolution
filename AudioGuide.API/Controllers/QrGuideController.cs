using AudioGuide.Core.DTOs;
using AudioGuide.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

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
    [HttpGet("generate/{token}")]
    public IActionResult GenerateQr(string token, [FromQuery] string? lang = "vi")
    {
        // URL thực tế của frontend hoặc endpoint scan trên Render
        // Du khách quét QR bằng camera điện thoại sẽ mở URL này
        var requestUrl = $"{Request.Scheme}://{Request.Host}/api/QrGuide/scan/{token}?lang={lang}";

        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(requestUrl, QRCodeGenerator.ECCLevel.Q);

        // Tạo ảnh PNG dạng PngByteQRCode (hoạt động tốt trên Linux/Docker của Render)
        var qrCode = new PngByteQRCode(qrCodeData);
        byte[] qrCodeBytes = qrCode.GetGraphic(20);

        return File(qrCodeBytes, "image/png");
    }
}