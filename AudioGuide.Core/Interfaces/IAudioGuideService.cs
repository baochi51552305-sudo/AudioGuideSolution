using AudioGuide.Core.DTOs;

namespace AudioGuide.Core.Interfaces;

public interface IAudioGuideService
{
    Task<PoiNarrativeDto?> ResolveQrScanAsync(string qrToken, string? languageCode);
    Task<PoiNarrativeDto?> ResolveGpsLocationAsync(GpsLocationRequest request);
}