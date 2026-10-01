namespace AudioGuide.Core.DTOs;

public class GpsLocationRequest
{
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public string? LanguageCode { get; set; }
}