using AudioGuide.Core.DTOs;
using AudioGuide.Core.Interfaces;
using AudioGuide.Core.Services;
using FluentAssertions;
using Moq;
using NetTopologySuite.Geometries;
using Xunit;

namespace AudioGuide.Tests.Services;

public class AudioGuideServiceTests
{
    private readonly Mock<IPoiRepository> _poiRepositoryMock;
    private readonly AudioGuideService _service;

    public AudioGuideServiceTests()
    {
        _poiRepositoryMock = new Mock<IPoiRepository>();
        _service = new AudioGuideService(_poiRepositoryMock.Object);
    }

    // 1. Kiểm thử chuẩn hóa 10 ngôn ngữ và fallback về tiếng Việt (vi)
    [Theory]
    [InlineData("vi-VN,vi;q=0.9", "vi")]
    [InlineData("en-US", "en")]
    [InlineData("fr-FR", "fr")]
    [InlineData("ja-JP", "ja")]
    [InlineData("ko-KR", "ko")]
    [InlineData("zh-CN", "zh")]
    [InlineData("de-DE", "de")]
    [InlineData("es-ES", "es")]
    [InlineData("ru-RU", "ru")]
    [InlineData("th-TH", "th")]
    [InlineData("it-IT", "vi")] // Tiếng Ý ngoài danh sách 10 nước -> fallback về "vi"
    [InlineData(null, "vi")]    // Không truyền mã -> fallback về "vi"
    public void NormalizeLanguage_ShouldReturnExpectedCode_OrFallbackToVietnamese(string? inputLang, string expectedLang)
    {
        // Act
        var result = AudioGuideService.NormalizeLanguage(inputLang);

        // Assert
        result.Should().Be(expectedLang);
    }

    // 2. Kiểm thử quét mã QR không hợp lệ
    [Fact]
    public async Task ResolveQrScanAsync_WithInvalidToken_ShouldReturnNull()
    {
        // Act
        var result = await _service.ResolveQrScanAsync("   ", "vi");

        // Assert
        result.Should().BeNull();
        _poiRepositoryMock.Verify(x => x.GetPoiByQrTokenAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // 3. Kiểm thử quét mã QR hợp lệ và ánh xạ đúng ngôn ngữ
    [Fact]
    public async Task ResolveQrScanAsync_WithValidToken_ShouldCallRepositoryWithNormalizedLang()
    {
        // Arrange
        var token = "qr-ddl-01";
        var mockPoi = new PoiNarrativeDto
        {
            PoiId = 1,
            Code = "DINH_DOC_LAP",
            Title = "Independence Palace",
            LanguageCode = "en"
        };

        _poiRepositoryMock.Setup(r => r.GetPoiByQrTokenAsync(token, "en"))
                          .ReturnsAsync(mockPoi);

        // Act
        var result = await _service.ResolveQrScanAsync(token, "en-US,en;q=0.8");

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Independence Palace");
        _poiRepositoryMock.Verify(r => r.GetPoiByQrTokenAsync(token, "en"), Times.Once);
    }

    // 4. Kiểm thử tọa độ GPS không hợp lệ (vượt giới hạn kinh độ / vĩ độ)
    [Fact]
    public async Task ResolveGpsLocationAsync_WithInvalidCoordinates_ShouldReturnNull()
    {
        // Arrange: Vĩ độ vượt quá 90 độ
        var invalidRequest = new GpsLocationRequest
        {
            Latitude = 120.0,
            Longitude = 106.6953,
            LanguageCode = "vi"
        };

        // Act
        var result = await _service.ResolveGpsLocationAsync(invalidRequest);

        // Assert
        result.Should().BeNull();
        _poiRepositoryMock.Verify(x => x.GetNearestPoiByGpsAsync(It.IsAny<Point>(), It.IsAny<string>()), Times.Never);
    }

    // 5. Kiểm thử định vị GPS hợp lệ
    [Fact]
    public async Task ResolveGpsLocationAsync_WithValidCoordinates_ShouldCallRepositoryCorrectly()
    {
        // Arrange: Tọa độ Dinh Độc Lập
        var validRequest = new GpsLocationRequest
        {
            Latitude = 10.7770,
            Longitude = 106.6953,
            LanguageCode = "fr-FR"
        };

        var expectedDto = new PoiNarrativeDto
        {
            PoiId = 1,
            Title = "Palais de la Réunification",
            LanguageCode = "fr"
        };

        _poiRepositoryMock.Setup(r => r.GetNearestPoiByGpsAsync(It.IsAny<Point>(), "fr"))
                          .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.ResolveGpsLocationAsync(validRequest);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Palais de la Réunification");
        _poiRepositoryMock.Verify(r => r.GetNearestPoiByGpsAsync(
            It.Is<Point>(p => Math.Abs(p.X - 106.6953) < 0.0001 && Math.Abs(p.Y - 10.7770) < 0.0001),
            "fr"), Times.Once);
    }
}