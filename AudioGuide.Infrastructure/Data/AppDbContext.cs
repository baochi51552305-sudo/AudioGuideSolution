using AudioGuide.Core.Entities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System;

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

        // 1. Cấu hình bảng POI
        modelBuilder.Entity<Poi>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(p => p.Code).IsUnique();
        });

        // 2. Cấu hình bảng POI Translations (Đa ngôn ngữ)
        modelBuilder.Entity<PoiTranslation>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.LanguageCode).HasMaxLength(10).IsRequired();
            entity.Property(t => t.Title).HasMaxLength(200).IsRequired();
            entity.Property(t => t.AudioUrl).HasMaxLength(500).IsRequired();

            entity.HasIndex(t => new { t.PoiId, t.LanguageCode }).IsUnique();

            entity.HasOne(t => t.Poi)
                  .WithMany(p => p.Translations)
                  .HasForeignKey(t => t.PoiId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 3. Cấu hình bảng QR Code
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
        // SEED DATA CHUẨN
        // ==========================================
        var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        // A. Danh sách 3 địa danh chuẩn (Kinh độ X, Vĩ độ Y)
        modelBuilder.Entity<Poi>().HasData(
            new Poi
            {
                Id = 1,
                Code = "DINH_DOC_LAP",
                Location = geometryFactory.CreatePoint(new Coordinate(106.6953, 10.7770)), // Dinh Độc Lập
                TriggerRadiusMeters = 30.0,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Poi
            {
                Id = 2,
                Code = "NHA_THO_DUC_BA",
                Location = geometryFactory.CreatePoint(new Coordinate(106.6990, 10.7798)), // Nhà thờ Đức Bà
                TriggerRadiusMeters = 25.0,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Poi
            {
                Id = 3,
                Code = "BUU_DIEN_TRUNG_TAM",
                Location = geometryFactory.CreatePoint(new Coordinate(106.6999, 10.7799)), // Bưu điện Thành phố
                TriggerRadiusMeters = 25.0,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
        // B. Dữ liệu bản dịch thuyết minh song ngữ (Sử dụng URL audio thực tế hoạt động 24/7)
        modelBuilder.Entity<PoiTranslation>().HasData(
            // 1. Dinh Độc Lập
            new PoiTranslation
            {
                Id = 1,
                PoiId = 1,
                LanguageCode = "vi",
                Title = "Dinh Độc Lập",
                Description = "Dinh Độc Lập, còn gọi là Dinh Thống Nhất, là di tích lịch sử quốc gia đặc biệt tọa lạc tại trung tâm Quận 1, Thành phố Hồ Chí Minh. Nơi đây từng chứng kiến sự kiện ngày 30 tháng 4 năm 1975 giải phóng miền Nam, thống nhất đất nước.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 180
            },
            new PoiTranslation
            {
                Id = 2,
                PoiId = 1,
                LanguageCode = "en",
                Title = "Independence Palace",
                Description = "A special national historical relic and architectural landmark in District 1, Ho Chi Minh City, marking historic reunification events.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 175
            },

            // 2. Nhà thờ Đức Bà
            new PoiTranslation
            {
                Id = 3,
                PoiId = 2,
                LanguageCode = "vi",
                Title = "Nhà thờ Đức Bà Sài Gòn",
                Description = "Kiệt tác kiến trúc cổ kính giao hòa giữa phong cách Roman và Gothic, biểu tượng gắn liền với lịch sử đô thị Sài Gòn.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 150
            },
            new PoiTranslation
            {
                Id = 4,
                PoiId = 2,
                LanguageCode = "en",
                Title = "Notre-Dame Cathedral Basilica of Saigon",
                Description = "An iconic cathedral built during the French colonial period in the heart of Saigon.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 145
            },

            // 3. Bưu điện Trung tâm
            new PoiTranslation
            {
                Id = 5,
                PoiId = 3,
                LanguageCode = "vi",
                Title = "Bưu điện Trung tâm Thành phố",
                Description = "Công trình kiến trúc Pháp đặc sắc kết hợp nét hoa văn trang trí phương Đông, được khánh thành vào cuối thế kỷ 19.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 120
            },
            new PoiTranslation
            {
                Id = 6,
                PoiId = 3,
                LanguageCode = "en",
                Title = "Saigon Central Post Office",
                Description = "One of the oldest and most architecturally preserved post offices in Southeast Asia.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 115
            }
        );

        // C. Mã token QR chuẩn liên kết trực tiếp vào từng POI
        modelBuilder.Entity<QrCode>().HasData(
            new QrCode { Id = 1, PoiId = 1, QrToken = "qr-ddl-01", ScanCount = 0, IsActive = true },
            new QrCode { Id = 2, PoiId = 2, QrToken = "qr-ntdb-02", ScanCount = 0, IsActive = true },
            new QrCode { Id = 3, PoiId = 3, QrToken = "qr-bdsg-03", ScanCount = 0, IsActive = true }
        );
    }
}