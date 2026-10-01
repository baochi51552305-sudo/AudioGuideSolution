using AudioGuide.Core.DTOs;
using AudioGuide.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AudioGuide.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GeoGuideController : ControllerBase
{
    private readonly IAudioGuideService _guideService;

    public GeoGuideController(IAudioGuideService guideService)
    {
        _guideService = guideService;
    }

    /// <summary>
    /// Gửi tọa độ GPS của người dùng để kích hoạt thuyết minh tự động khi đến gần địa danh
    /// </summary>
    [HttpPost("locate")]
    public async Task<ActionResult<PoiNarrativeDto>> LocatePoi([FromBody] GpsLocationRequest request)
    {
        request.LanguageCode ??= Request.Headers.AcceptLanguage.FirstOrDefault();
        var narrative = await _guideService.ResolveGpsLocationAsync(request);

        if (narrative == null)
            return Ok(new { message = "Du khách chưa bước vào bán kính của địa danh nào.", poi = (object?)null });

        return Ok(narrative);
    }
}