using AudioGuide.Core.DTOs;
using NetTopologySuite.Geometries;

namespace AudioGuide.Core.Interfaces;

public interface IPoiRepository
{
    // Tìm POI gần nhất theo tọa độ GPS và ngôn ngữ
    Task<PoiNarrativeDto?> GetNearestPoiByGpsAsync(Point userLocation, string languageCode);

    // Lấy nội dung POI theo mã token QR và ngôn ngữ
    Task<PoiNarrativeDto?> GetPoiByQrTokenAsync(string qrToken, string languageCode);
}