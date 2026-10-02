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
        // SEED DATA: 5 ĐỊA ĐIỂM TIÊU BIỂU TP.HCM
        // ==========================================
        var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        // A. Danh sách 5 địa danh (Kinh độ X, Vĩ độ Y)
        modelBuilder.Entity<Poi>().HasData(
            new Poi
            {
                Id = 1,
                Code = "CHO_BEN_THANH",
                Location = geometryFactory.CreatePoint(new Coordinate(106.6983, 10.7725)), // Chợ Bến Thành
                TriggerRadiusMeters = 35.0,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Poi
            {
                Id = 2,
                Code = "NHA_HAT_THANH_PHO",
                Location = geometryFactory.CreatePoint(new Coordinate(106.7032, 10.7766)), // Nhà hát Thành phố
                TriggerRadiusMeters = 30.0,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Poi
            {
                Id = 3,
                Code = "BAO_TANG_CHUNG_TICH_CHIEN_TRANH",
                Location = geometryFactory.CreatePoint(new Coordinate(106.6922, 10.7794)), // Bảo tàng Chứng tích Chiến tranh
                TriggerRadiusMeters = 30.0,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Poi
            {
                Id = 4,
                Code = "LANDMARK_81",
                Location = geometryFactory.CreatePoint(new Coordinate(106.7218, 10.7950)), // Landmark 81
                TriggerRadiusMeters = 50.0,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Poi
            {
                Id = 5,
                Code = "BEN_NHA_RONG",
                Location = geometryFactory.CreatePoint(new Coordinate(106.7068, 10.7681)), // Bến Nhà Rồng
                TriggerRadiusMeters = 35.0,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // B. Bản dịch thuyết minh 5 ngôn ngữ (vi, zh, en, fr, ru)
        modelBuilder.Entity<PoiTranslation>().HasData(
            // ----------------------------------------------------
            // 1. Chợ Bến Thành (Id: 1 -> 5)
            // ----------------------------------------------------
            new PoiTranslation
            {
                Id = 1,
                PoiId = 1,
                LanguageCode = "vi",
                Title = "Chợ Bến Thành",
                Description = "Biểu tượng giao thương lâu đời và sống động bậc nhất giữa lòng trung tâm Thành phố Hồ Chí Minh.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 150
            },
            new PoiTranslation
            {
                Id = 2,
                PoiId = 1,
                LanguageCode = "zh",
                Title = "滨城市场",
                Description = "胡志明市最具代表性的历史贸易集市，汇聚丰富多元的越南地道特色商品与美食文化。",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 145
            },
            new PoiTranslation
            {
                Id = 3,
                PoiId = 1,
                LanguageCode = "en",
                Title = "Ben Thanh Market",
                Description = "One of the most famous and historic commercial landmarks in central Ho Chi Minh City.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 140
            },
            new PoiTranslation
            {
                Id = 4,
                PoiId = 1,
                LanguageCode = "fr",
                Title = "Marché de Ben Thanh",
                Description = "Symbole commercial et culturel emblématique au cœur de Hô Chi Minh-Ville.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 148
            },
            new PoiTranslation
            {
                Id = 5,
                PoiId = 1,
                LanguageCode = "ru",
                Title = "Рынок Бен Тхань",
                Description = "Один из старейших и известнейших торговых символов в центре Хошимина.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 152
            },

            // ----------------------------------------------------
            // 2. Nhà hát Thành phố (Id: 6 -> 10)
            // ----------------------------------------------------
            new PoiTranslation
            {
                Id = 6,
                PoiId = 2,
                LanguageCode = "vi",
                Title = "Nhà hát Thành phố",
                Description = "Công trình nghệ thuật kiến trúc Gothic - Phục Hưng Pháp tráng lệ khánh thành năm 1900.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 160
            },
            new PoiTranslation
            {
                Id = 7,
                PoiId = 2,
                LanguageCode = "zh",
                Title = "胡志明市大剧院",
                Description = "于1900年竣工的典雅法国殖民时期哥特式与文艺复兴风格艺术建筑瑰宝。",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 155
            },
            new PoiTranslation
            {
                Id = 8,
                PoiId = 2,
                LanguageCode = "en",
                Title = "Saigon Opera House",
                Description = "A magnificent French colonial architectural opera house completed in 1900.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 150
            },
            new PoiTranslation
            {
                Id = 9,
                PoiId = 2,
                LanguageCode = "fr",
                Title = "Opéra de Saïgon",
                Description = "Magnifique chef-d'œuvre de l'architecture coloniale française inauguré en 1900.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 158
            },
            new PoiTranslation
            {
                Id = 10,
                PoiId = 2,
                LanguageCode = "ru",
                Title = "Муниципальный театр Сайгона",
                Description = "Величественное здание оперного театра в колониальном французском стиле, открытое в 1900 году.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 162
            },

            // ----------------------------------------------------
            // 3. Bảo tàng Chứng tích Chiến tranh (Id: 11 -> 15)
            // ----------------------------------------------------
            new PoiTranslation
            {
                Id = 11,
                PoiId = 3,
                LanguageCode = "vi",
                Title = "Bảo tàng Chứng tích Chiến tranh",
                Description = "Nơi lưu giữ những tài liệu, hiện vật lịch sử chân thực về các cuộc chiến tranh và thông điệp hòa bình.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 190
            },
            new PoiTranslation
            {
                Id = 12,
                PoiId = 3,
                LanguageCode = "zh",
                Title = "战争遗迹博物馆",
                Description = "展示有关越南近代战争历史照片、军事实物并传递珍爱和平理念的纪念博物馆。",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 185
            },
            new PoiTranslation
            {
                Id = 13,
                PoiId = 3,
                LanguageCode = "en",
                Title = "War Remnants Museum",
                Description = "A poignant museum preserving wartime exhibits, artifacts, and powerful calls for world peace.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 180
            },
            new PoiTranslation
            {
                Id = 14,
                PoiId = 3,
                LanguageCode = "fr",
                Title = "Musée des vestiges de guerre",
                Description = "Musée mémorial conservant des preuves historiques émouvantes et un puissant message de paix.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 188
            },
            new PoiTranslation
            {
                Id = 15,
                PoiId = 3,
                LanguageCode = "ru",
                Title = "Музей жертв войны",
                Description = "Мемориальный комплекс, хранящий свидетельства военных событий и призыв к глобальному миру.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 195
            },

            // ----------------------------------------------------
            // 4. Landmark 81 (Id: 16 -> 20)
            // ----------------------------------------------------
            new PoiTranslation
            {
                Id = 16,
                PoiId = 4,
                LanguageCode = "vi",
                Title = "Landmark 81",
                Description = "Tòa nhà cao nhất Việt Nam, biểu tượng cho khát vọng phát triển năng động và hiện đại.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 130
            },
            new PoiTranslation
            {
                Id = 17,
                PoiId = 4,
                LanguageCode = "zh",
                Title = "地标塔 81",
                Description = "越南第一高楼，象征着胡志明市现代化蓬勃发展的新地标建筑。",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 125
            },
            new PoiTranslation
            {
                Id = 18,
                PoiId = 4,
                LanguageCode = "en",
                Title = "Landmark 81",
                Description = "The tallest skyscraper in Vietnam, symbolizing modern prosperity and dynamism.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 120
            },
            new PoiTranslation
            {
                Id = 19,
                PoiId = 4,
                LanguageCode = "fr",
                Title = "Landmark 81",
                Description = "Le plus haut gratte-ciel du Vietnam, symbole de modernité et d'élan économique.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 128
            },
            new PoiTranslation
            {
                Id = 20,
                PoiId = 4,
                LanguageCode = "ru",
                Title = "Небоскреб Landmark 81",
                Description = "Самое высокое здание во Вьетнаме, символ стремительного современного развития страны.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 132
            },

            // ----------------------------------------------------
            // 5. Bến Nhà Rồng (Id: 21 -> 25)
            // ----------------------------------------------------
            new PoiTranslation
            {
                Id = 21,
                PoiId = 5,
                LanguageCode = "vi",
                Title = "Bến Nhà Rồng",
                Description = "Di tích lịch sử quan trọng bên sông Sài Gòn, nơi Bác Hồ ra đi tìm đường cứu nước năm 1911.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 170
            },
            new PoiTranslation
            {
                Id = 22,
                PoiId = 5,
                LanguageCode = "zh",
                Title = "龙屋港（胡志明博物馆）",
                Description = "坐落于西贡河畔的重要历史遗址，1911年胡志明主席在此登船踏上寻求救国之路。",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 165
            },
            new PoiTranslation
            {
                Id = 23,
                PoiId = 5,
                LanguageCode = "en",
                Title = "Dragon Wharf (Nha Rong Wharf)",
                Description = "A historic riverfront monument where President Ho Chi Minh departed to seek national salvation in 1911.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 160
            },
            new PoiTranslation
            {
                Id = 24,
                PoiId = 5,
                LanguageCode = "fr",
                Title = "Quai Nha Rong",
                Description = "Site historique majeur au bord du fleuve Saïgon d'où le président Hô Chi Minh est parti en 1911.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 168
            },
            new PoiTranslation
            {
                Id = 25,
                PoiId = 5,
                LanguageCode = "ru",
                Title = "Пристань Няронг",
                Description = "Знаковое историческое место на реке Сайгон, откуда в 1911 году Хо Ши Мин отправился в путь за освобождение родины.",
                AudioUrl = "https://actions.google.com/sounds/v1/ambiences/daytime_forest_bonfire.ogg",
                DurationSeconds = 172
            }
        );

        // C. Mã token QR liên kết trực tiếp vào 5 POI
        modelBuilder.Entity<QrCode>().HasData(
            new QrCode { Id = 1, PoiId = 1, QrToken = "qr-cbt-01", ScanCount = 0, IsActive = true },
            new QrCode { Id = 2, PoiId = 2, QrToken = "qr-nhtp-02", ScanCount = 0, IsActive = true },
            new QrCode { Id = 3, PoiId = 3, QrToken = "qr-btct-03", ScanCount = 0, IsActive = true },
            new QrCode { Id = 4, PoiId = 4, QrToken = "qr-lm81-04", ScanCount = 0, IsActive = true },
            new QrCode { Id = 5, PoiId = 5, QrToken = "qr-bnr-05", ScanCount = 0, IsActive = true }
        );
    }
}