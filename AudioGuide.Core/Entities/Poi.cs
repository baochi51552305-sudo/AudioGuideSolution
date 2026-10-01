using NetTopologySuite.Geometries;

namespace AudioGuide.Core.Entities;

public class Poi
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Point Location { get; set; } = default!;
    public double TriggerRadiusMeters { get; set; } = 15.0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PoiTranslation> Translations { get; set; } = new List<PoiTranslation>();
    public ICollection<QrCode> QrCodes { get; set; } = new List<QrCode>();
}