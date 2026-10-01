using AudioGuide.Core.DTOs;
using AudioGuide.Core.Interfaces;
using AudioGuide.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace AudioGuide.Infrastructure.Repositories;

public class PoiRepository : IPoiRepository
{
    private readonly AppDbContext _context;

    public PoiRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PoiNarrativeDto?> GetNearestPoiByGpsAsync(Point userLocation, string languageCode)
    {
        // Tìm các POI đang hoạt động có khoảng cách nhỏ hơn bán kính kích hoạt
        var candidate = await _context.Pois
            .AsNoTracking()
            .Include(p => p.Translations)
            .Where(p => p.IsActive && p.Location.Distance(userLocation) <= p.TriggerRadiusMeters)
            .Select(p => new
            {
                Poi = p,
                Distance = p.Location.Distance(userLocation),
                // Lấy đúng ngôn ngữ yêu cầu, nếu không có thì mặc định lấy tiếng Việt "vi"
                SelectedTranslation = p.Translations.FirstOrDefault(t => t.LanguageCode == languageCode)
                                      ?? p.Translations.FirstOrDefault(t => t.LanguageCode == "vi")
            })
            .OrderBy(x => x.Distance)
            .FirstOrDefaultAsync();

        if (candidate == null || candidate.SelectedTranslation == null)
            return null;

        return new PoiNarrativeDto
        {
            PoiId = candidate.Poi.Id,
            Code = candidate.Poi.Code,
            Title = candidate.SelectedTranslation.Title,
            Description = candidate.SelectedTranslation.Description,
            AudioUrl = candidate.SelectedTranslation.AudioUrl,
            DurationSeconds = candidate.SelectedTranslation.DurationSeconds,
            LanguageCode = candidate.SelectedTranslation.LanguageCode,
            DistanceMeters = Math.Round(candidate.Distance, 2)
        };
    }

    public async Task<PoiNarrativeDto?> GetPoiByQrTokenAsync(string qrToken, string languageCode)
    {
        // Tra cứu mã QR và POI liên kết
        var qr = await _context.QrCodes
            .Include(q => q.Poi)
                .ThenInclude(p => p!.Translations)
            .FirstOrDefaultAsync(q => q.QrToken == qrToken && q.IsActive && q.Poi!.IsActive);

        if (qr == null || qr.Poi == null)
            return null;

        // Tăng lượt quét mã QR
        qr.ScanCount++;
        await _context.SaveChangesAsync();

        var translation = qr.Poi.Translations.FirstOrDefault(t => t.LanguageCode == languageCode)
                          ?? qr.Poi.Translations.FirstOrDefault(t => t.LanguageCode == "vi");

        if (translation == null)
            return null;

        return new PoiNarrativeDto
        {
            PoiId = qr.Poi.Id,
            Code = qr.Poi.Code,
            Title = translation.Title,
            Description = translation.Description,
            AudioUrl = translation.AudioUrl,
            DurationSeconds = translation.DurationSeconds,
            LanguageCode = translation.LanguageCode,
            DistanceMeters = 0
        };
    }
}