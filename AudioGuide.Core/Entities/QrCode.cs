namespace AudioGuide.Core.Entities;

public class QrCode
{
    public int Id { get; set; }
    public int PoiId { get; set; }
    public string QrToken { get; set; } = string.Empty;
    public int ScanCount { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public Poi? Poi { get; set; }
}