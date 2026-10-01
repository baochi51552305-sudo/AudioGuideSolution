using AudioGuide.Core.Entities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace AudioGuide.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Poi> Pois => Set<Poi>();
    public DbSet<PoiTranslation> PoiTranslations => Set<PoiTranslation>();
    public DbSet<QrCode> QrCodes => Set<QrCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Cấu hình bảng POI (Điểm tham quan)
        modelBuilder.Entity<Poi>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(p => p.Code).IsUnique();

            // Cột Location lưu trữ đối tượng tọa độ địa lý WGS84
            entity.Property(p => p.Location).HasColumnType("geography").IsRequired();
        });

        // 2. Cấu hình bảng POI Translations (Đa ngôn ngữ)
        modelBuilder.Entity<PoiTranslation>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.LanguageCode).HasMaxLength(10).IsRequired();
            entity.Property(t => t.Title).HasMaxLength(200).IsRequired();
            entity.Property(t => t.AudioUrl).HasMaxLength(500).IsRequired();

            // Một POI không thể có 2 bản dịch trùng cùng một ngôn ngữ
            entity.HasIndex(t => new { t.PoiId, t.LanguageCode }).IsUnique();

            entity.HasOne(t => t.Poi)
                  .WithMany(p => p.Translations)
                  .HasForeignKey(t => t.PoiId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 3. Cấu hình bảng QR Code động
        modelBuilder.Entity<QrCode>(entity =>
        {
            entity.HasKey(q => q.Id);
            entity.Property(q => q.QrToken).HasMaxLength(64).IsRequired();
            entity.HasIndex(q => q.QrToken).IsUnique();

            entity.HasOne(q => q.Poi)
                  .WithMany(p => p.QrCodes)
                  .HasForeignKey(q => q.PoiId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ==========================================
        // SEED DATA: 3 ĐỊA DANH THỰC TẾ TẠI TP.HCM
        // ==========================================
        // Khởi tạo GeometryFactory với hệ quy chiếu SRID 4326 (chuẩn tọa độ GPS toàn cầu)
        var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        // A. Dữ liệu POI (Tọa độ dạng Longitude - Kinh độ, Latitude - Vĩ độ)
        modelBuilder.Entity<Poi>().HasData(
            new Poi
            {
                Id = 1,
                Code = "DINH_DOC_LAP",
                Location = geometryFactory.CreatePoint(new Coordinate(106.6953, 10.7770)), // Dinh Độc Lập
                TriggerRadiusMeters = 20.0,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Poi
            {
                Id = 2,
                Code = "NHA_THO_DUC_BA",
                Location = geometryFactory.CreatePoint(new Coordinate(106.6990, 10.7798)), // Nhà thờ Đức Bà
                TriggerRadiusMeters = 15.0,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Poi
            {
                Id = 3,
                Code = "BUU_DIEN_TRUNG_TAM",
                Location = geometryFactory.CreatePoint(new Coordinate(106.6999, 10.7799)), // Bưu điện Thành phố
                TriggerRadiusMeters = 15.0,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // B. Dữ liệu bản dịch thuyết minh song ngữ
        modelBuilder.Entity<PoiTranslation>().HasData(
            // 1. Dinh Độc Lập
            new PoiTranslation
            {
                Id = 1,
                PoiId = 1,
                LanguageCode = "vi",
                Title = "Dinh Độc Lập",
                Description = "Di tích lịch sử quốc gia đặc biệt, biểu tượng của sự thống nhất đất nước.",
                AudioUrl = "https://cdn.example.com/audio/vi/dinh_doc_lap.mp3",
                DurationSeconds = 180
            },
            new PoiTranslation
            {
                Id = 2,
                PoiId = 1,
                LanguageCode = "en",
                Title = "Independence Palace",
                Description = "A special national historical relic and architectural landmark in District 1.",
                AudioUrl = "https://cdn.example.com/audio/en/dinh_doc_lap.mp3",
                DurationSeconds = 175
            },

            // 2. Nhà thờ Đức Bà Sài Gòn
            new PoiTranslation
            {
                Id = 3,
                PoiId = 2,
                LanguageCode = "vi",
                Title = "Nhà thờ Đức Bà Sài Gòn",
                Description = "Kiệt tác kiến trúc cổ kính phong cách Roman và Gothic nằm giữa trung tâm Sài Gòn.",
                AudioUrl = "https://cdn.example.com/audio/vi/nha_tho_duc_ba.mp3",
                DurationSeconds = 150
            },
            new PoiTranslation
            {
                Id = 4,
                PoiId = 2,
                LanguageCode = "en",
                Title = "Notre-Dame Cathedral Basilica of Saigon",
                Description = "An iconic cathedral built during the French colonial period.",
                AudioUrl = "https://cdn.example.com/audio/en/nha_tho_duc_ba.mp3",
                DurationSeconds = 145
            },

            // 3. Bưu điện Trung tâm Thành phố
            new PoiTranslation
            {
                Id = 5,
                PoiId = 3,
                LanguageCode = "vi",
                Title = "Bưu điện Trung tâm Thành phố",
                Description = "Công trình kiến trúc mang đậm dấu ấn phong cách Pháp được khánh thành vào cuối thế kỷ 19.",
                AudioUrl = "https://cdn.example.com/audio/vi/buu_dien_trung_tam.mp3",
                DurationSeconds = 120
            },
            new PoiTranslation
            {
                Id = 6,
                PoiId = 3,
                LanguageCode = "en",
                Title = "Saigon Central Post Office",
                Description = "One of the oldest and most beautiful post offices in Southeast Asia.",
                AudioUrl = "https://cdn.example.com/audio/en/buu_dien_trung_tam.mp3",
                DurationSeconds = 115
            }
        );

        // C. Dữ liệu mã QR in thực tế tại từng địa danh
        modelBuilder.Entity<QrCode>().HasData(
            new QrCode { Id = 1, PoiId = 1, QrToken = "qr-ddl-01", ScanCount = 0, IsActive = true },
            new QrCode { Id = 2, PoiId = 2, QrToken = "qr-ntdb-02", ScanCount = 0, IsActive = true },
            new QrCode { Id = 3, PoiId = 3, QrToken = "qr-bdsg-03", ScanCount = 0, IsActive = true }
        );
    }
}