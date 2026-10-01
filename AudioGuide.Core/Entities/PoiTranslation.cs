namespace AudioGuide.Core.Entities;

public class PoiTranslation
{
    public int Id { get; set; }
    public int PoiId { get; set; }
    public string LanguageCode { get; set; } = string.Empty; // "vi", "en", ...
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AudioUrl { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }

    public Poi? Poi { get; set; }
}