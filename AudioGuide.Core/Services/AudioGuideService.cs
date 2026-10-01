using AudioGuide.Core.DTOs;
using AudioGuide.Core.Interfaces;
using NetTopologySuite.Geometries;

namespace AudioGuide.Core.Services;

public class AudioGuideService : IAudioGuideService
{
    private readonly IPoiRepository _poiRepository;
    private static readonly GeometryFactory _geometryFactory = new(new PrecisionModel(), 4326);

    // Danh sách 10 mã ngôn ngữ tiêu chuẩn được hệ thống hỗ trợ
    private static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "vi", // Tiếng Việt (Default)
        "en", // Tiếng Anh
        "fr", // Tiếng Pháp
        "ja", // Tiếng Nhật
        "ko", // Tiếng Hàn
        "zh", // Tiếng Trung
        "de", // Tiếng Đức
        "es", // Tiếng Tây Ban Nha
        "ru", // Tiếng Nga
        "th"  // Tiếng Thái
    };

    public AudioGuideService(IPoiRepository poiRepository)
    {
        _poiRepository = poiRepository;
    }

    public async Task<PoiNarrativeDto?> ResolveQrScanAsync(string qrToken, string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(qrToken))
            return null;

        var normalizedLang = NormalizeLanguage(languageCode);
        return await _poiRepository.GetPoiByQrTokenAsync(qrToken.Trim(), normalizedLang);
    }

    public async Task<PoiNarrativeDto?> ResolveGpsLocationAsync(GpsLocationRequest request)
    {
        // Kiểm tra phạm vi hợp lệ của tọa độ GPS
        if (request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180)
            return null;

        var normalizedLang = NormalizeLanguage(request.LanguageCode);

        // Chuẩn WGS84: Point(Longitude, Latitude)
        var userPoint = _geometryFactory.CreatePoint(new Coordinate(request.Longitude, request.Latitude));

        return await _poiRepository.GetNearestPoiByGpsAsync(userPoint, normalizedLang);
    }

    /// <summary>
    /// Xử lý và chuẩn hóa chuỗi ngôn ngữ gửi từ Client (Accept-Language header hoặc query param)
    /// Ví dụ: "en-US,en;q=0.9" -> "en", "zh-CN" -> "zh", mã không hỗ trợ -> fallback về "vi"
    /// </summary>
    public static string NormalizeLanguage(string? lang)
    {
        if (string.IsNullOrWhiteSpace(lang))
            return "vi";

        // Tách lấy tag ngôn ngữ đầu tiên nếu Client gửi chuỗi phức hợp (vd: en-US,en;q=0.9)
        var primaryTag = lang.Split(',', ';')[0].Trim();

        // Lấy mã ngôn ngữ 2 ký tự (vd: "vi-VN" -> "vi", "zh-CN" -> "zh")
        var subCode = primaryTag.Split('-')[0].Trim().ToLowerInvariant();

        return SupportedLanguages.Contains(subCode) ? subCode : "vi";
    }
}