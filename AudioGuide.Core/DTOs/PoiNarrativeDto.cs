namespace AudioGuide.Core.DTOs;

public class PoiNarrativeDto
{
    public int PoiId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AudioUrl { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public double DistanceMeters { get; set; }
}