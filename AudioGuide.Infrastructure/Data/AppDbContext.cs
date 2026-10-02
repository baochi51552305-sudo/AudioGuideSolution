using AudioGuide.Core.Entities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System;
using System.Net;

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

        // 2. Cấu hình bảng POI Translations
        modelBuilder.Entity<PoiTranslation>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.LanguageCode).HasMaxLength(10).IsRequired();
            entity.Property(t => t.Title).HasMaxLength(200).IsRequired();
            entity.Property(t => t.AudioUrl).HasMaxLength(1000).IsRequired();

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
        // SEED DATA: 5 ĐỊA ĐIỂM + 5 VOICE THUYẾT MINH THỰC TẾ
        // ==========================================
        var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        // Danh sách 5 địa danh
        modelBuilder.Entity<Poi>().HasData(
            new Poi
            {
                Id = 1,
                Code = "CHO_BEN_THANH",
                Location = geometryFactory.CreatePoint(new Coordinate(106.6983, 10.7725)),
                TriggerRadiusMeters = 35.0,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Poi
            {
                Id = 2,
                Code = "NHA_HAT_THANH_PHO",
                Location = geometryFactory.CreatePoint(new Coordinate(106.7032, 10.7766)),
                TriggerRadiusMeters = 30.0,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Poi
            {
                Id = 3,
                Code = "BAO_TANG_CHUNG_TICH_CHIEN_TRANH",
                Location = geometryFactory.CreatePoint(new Coordinate(106.6922, 10.7794)),
                TriggerRadiusMeters = 30.0,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Poi
            {
                Id = 4,
                Code = "LANDMARK_81",
                Location = geometryFactory.CreatePoint(new Coordinate(106.7218, 10.7950)),
                TriggerRadiusMeters = 50.0,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Poi
            {
                Id = 5,
                Code = "BEN_NHA_RONG",
                Location = geometryFactory.CreatePoint(new Coordinate(106.7068, 10.7681)),
                TriggerRadiusMeters = 35.0,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Hàm tiện ích tạo URL Voice thực tế bằng giọng đọc chuẩn bản ngữ
        static string BuildTtsUrl(string text, string lang) =>
            $"https://translate.google.com/translate_tts?ie=UTF-8&tl={lang}&client=tw-ob&q={Uri.EscapeDataString(text)}";

        // 25 Bản dịch với Voice Audio thực tế theo chuẩn từng thứ tiếng
        modelBuilder.Entity<PoiTranslation>().HasData(
            // 1. Chợ Bến Thành
            new PoiTranslation
            {
                Id = 1,
                PoiId = 1,
                LanguageCode = "vi",
                Title = "Chợ Bến Thành",
                Description = "Chào mừng quý khách đến với Chợ Bến Thành, biểu tượng giao thương lâu đời và sống động bậc nhất giữa lòng Sài Gòn.",
                AudioUrl = BuildTtsUrl("Chào mừng quý khách đến với Chợ Bến Thành, biểu tượng giao thương lâu đời và sống động bậc nhất giữa lòng Sài Gòn.", "vi"),
                DurationSeconds = 12
            },
            new PoiTranslation
            {
                Id = 2,
                PoiId = 1,
                LanguageCode = "zh",
                Title = "滨城市场",
                Description = "欢迎来到滨城市场，这是胡志明市最具代表性的历史悠久的地标与繁华集市。",
                AudioUrl = BuildTtsUrl("欢迎来到滨城市场，这是胡志明市最具代表性的历史悠久的地标与繁华集市。", "zh-CN"),
                DurationSeconds = 10
            },
            new PoiTranslation
            {
                Id = 3,
                PoiId = 1,
                LanguageCode = "en",
                Title = "Ben Thanh Market",
                Description = "Welcome to Ben Thanh Market, one of the most famous and historic commercial landmarks in central Ho Chi Minh City.",
                AudioUrl = BuildTtsUrl("Welcome to Ben Thanh Market, one of the most famous and historic commercial landmarks in central Ho Chi Minh City.", "en"),
                DurationSeconds = 10
            },
            new PoiTranslation
            {
                Id = 4,
                PoiId = 1,
                LanguageCode = "fr",
                Title = "Marché de Ben Thanh",
                Description = "Bienvenue au marché de Ben Thanh, symbole commercial et culturel incontournable au cœur de Ho Chi Minh Ville.",
                AudioUrl = BuildTtsUrl("Bienvenue au marché de Ben Thanh, symbole commercial et culturel incontournable au cœur de Ho Chi Minh Ville.", "fr"),
                DurationSeconds = 11
            },
            new PoiTranslation
            {
                Id = 5,
                PoiId = 1,
                LanguageCode = "ru",
                Title = "Рынок Бен Тхань",
                Description = "Добро пожаловать на рынок Бен Тхань, знаменитый исторический символ и центр торговли в Хошимине.",
                AudioUrl = BuildTtsUrl("Добро пожаловать на рынок Бен Тхань, знаменитый исторический символ и центр торговли в Хошимине.", "ru"),
                DurationSeconds = 11
            },

            // 2. Nhà hát Thành phố
            new PoiTranslation
            {
                Id = 6,
                PoiId = 2,
                LanguageCode = "vi",
                Title = "Nhà hát Thành phố",
                Description = "Nhà hát Thành phố Hồ Chí Minh là công trình kiến trúc nghệ thuật cổ kính theo phong cách Phục hưng Pháp khánh thành năm 1900.",
                AudioUrl = BuildTtsUrl("Nhà hát Thành phố Hồ Chí Minh là công trình kiến trúc nghệ thuật cổ kính theo phong cách Phục hưng Pháp khánh thành năm 1900.", "vi"),
                DurationSeconds = 13
            },
            new PoiTranslation
            {
                Id = 7,
                PoiId = 2,
                LanguageCode = "zh",
                Title = "胡志明市大剧院",
                Description = "胡志明市大剧院于1900年竣工，是一座华丽的法国古典主义歌剧院建筑瑰宝。",
                AudioUrl = BuildTtsUrl("胡志明市大剧院于1900年竣工，是一座华丽的法国古典主义歌剧院建筑瑰宝。", "zh-CN"),
                DurationSeconds = 11
            },
            new PoiTranslation
            {
                Id = 8,
                PoiId = 2,
                LanguageCode = "en",
                Title = "Saigon Opera House",
                Description = "The Saigon Opera House is a magnificent French colonial masterpiece officially inaugurated in 1900.",
                AudioUrl = BuildTtsUrl("The Saigon Opera House is a magnificent French colonial masterpiece officially inaugurated in 1900.", "en"),
                DurationSeconds = 11
            },
            new PoiTranslation
            {
                Id = 9,
                PoiId = 2,
                LanguageCode = "fr",
                Title = "Opéra de Saïgon",
                Description = "L'Opéra de Saïgon est un somptueux chef-d'œuvre de l'architecture coloniale française inauguré en 1900.",
                AudioUrl = BuildTtsUrl("L'Opéra de Saïgon est un somptueux chef-d'œuvre de l'architecture coloniale française inauguré en 1900.", "fr"),
                DurationSeconds = 12
            },
            new PoiTranslation
            {
                Id = 10,
                PoiId = 2,
                LanguageCode = "ru",
                Title = "Муниципальный оперный театр Сайгона",
                Description = "Оперный театр Сайгона — великолепный памятник французской колониальной архитектуры, открытый в 1900 году.",
                AudioUrl = BuildTtsUrl("Оперный театр Сайгона — великолепный памятник французской колониальной архитектуры, открытый в 1900 году.", "ru"),
                DurationSeconds = 12
            },

            // 3. Bảo tàng Chứng tích Chiến tranh
            new PoiTranslation
            {
                Id = 11,
                PoiId = 3,
                LanguageCode = "vi",
                Title = "Bảo tàng Chứng tích Chiến tranh",
                Description = "Bảo tàng Chứng tích Chiến tranh lưu giữ hàng ngàn tài liệu, hiện vật lịch sử và lan tỏa khát vọng yêu chuộng hòa bình.",
                AudioUrl = BuildTtsUrl("Bảo tàng Chứng tích Chiến tranh lưu giữ hàng ngàn tài liệu, hiện vật lịch sử và lan tỏa khát vọng yêu chuộng hòa bình.", "vi"),
                DurationSeconds = 14
            },
            new PoiTranslation
            {
                Id = 12,
                PoiId = 3,
                LanguageCode = "zh",
                Title = "战争遗迹博物馆",
                Description = "战争遗迹博物馆展示了珍贵的近代战争实物档案，向世界传递珍爱和平的崇高理念。",
                AudioUrl = BuildTtsUrl("战争遗迹博物馆展示了珍贵的近代战争实物档案，向世界传递珍爱和平的崇高理念。", "zh-CN"),
                DurationSeconds = 11
            },
            new PoiTranslation
            {
                Id = 13,
                PoiId = 3,
                LanguageCode = "en",
                Title = "War Remnants Museum",
                Description = "The War Remnants Museum preserves thousands of historic wartime artifacts, delivering a profound message of global peace.",
                AudioUrl = BuildTtsUrl("The War Remnants Museum preserves thousands of historic wartime artifacts, delivering a profound message of global peace.", "en"),
                DurationSeconds = 12
            },
            new PoiTranslation
            {
                Id = 14,
                PoiId = 3,
                LanguageCode = "fr",
                Title = "Musée des vestiges de guerre",
                Description = "Le Musée des vestiges de guerre conserve des témoignages historiques poignants et diffuse un message d'espoir pour la paix.",
                AudioUrl = BuildTtsUrl("Le Musée des vestiges de guerre conserve des témoignages historiques poignants et diffuse un message d'espoir pour la paix.", "fr"),
                DurationSeconds = 13
            },
            new PoiTranslation
            {
                Id = 15,
                PoiId = 3,
                LanguageCode = "ru",
                Title = "Музей жертв войны",
                Description = "Музей жертв войны хранит тысячи исторических свидетельств и призывает людей к миру и согласию во всем мире.",
                AudioUrl = BuildTtsUrl("Музей жертв войны хранит тысячи исторических свидетельств и призывает людей к миру и согласию во всем мире.", "ru"),
                DurationSeconds = 13
            },

            // 4. Landmark 81
            new PoiTranslation
            {
                Id = 16,
                PoiId = 4,
                LanguageCode = "vi",
                Title = "Landmark 81",
                Description = "Landmark 81 là tòa nhà chọc trời cao nhất Việt Nam, biểu tượng cho sự thịnh vượng và năng động của TP. Hồ Chí Minh hiện đại.",
                AudioUrl = BuildTtsUrl("Landmark 81 là tòa nhà chọc trời cao nhất Việt Nam, biểu tượng cho sự thịnh vượng và năng động của thành phố Hồ Chí Minh hiện đại.", "vi"),
                DurationSeconds = 13
            },
            new PoiTranslation
            {
                Id = 17,
                PoiId = 4,
                LanguageCode = "zh",
                Title = "地标塔 81",
                Description = "地标塔81是越南第一高楼，象征着胡志明市现代化蓬勃发展的崭新形象与活力。",
                AudioUrl = BuildTtsUrl("地标塔81是越南第一高楼，象征着胡志明市现代化蓬勃发展的崭新形象与活力。", "zh-CN"),
                DurationSeconds = 10
            },
            new PoiTranslation
            {
                Id = 18,
                PoiId = 4,
                LanguageCode = "en",
                Title = "Landmark 81",
                Description = "Landmark 81 is the tallest skyscraper in Vietnam, standing as a proud symbol of modern prosperity and rapid innovation.",
                AudioUrl = BuildTtsUrl("Landmark 81 is the tallest skyscraper in Vietnam, standing as a proud symbol of modern prosperity and rapid innovation.", "en"),
                DurationSeconds = 12
            },
            new PoiTranslation
            {
                Id = 19,
                PoiId = 4,
                LanguageCode = "fr",
                Title = "Landmark 81",
                Description = "Landmark 81 est le plus haut gratte-ciel du Vietnam, emblème de modernité, de dynamisme et de prospérité urbaine.",
                AudioUrl = BuildTtsUrl("Landmark 81 est le plus haut gratte-ciel du Vietnam, emblème de modernité, de dynamisme et de prospérité urbaine.", "fr"),
                DurationSeconds = 12
            },
            new PoiTranslation
            {
                Id = 20,
                PoiId = 4,
                LanguageCode = "ru",
                Title = "Небоскреб Landmark 81",
                Description = "Landmark 81 — самое высокое здание во Вьетнаме, яркий символ динамичного развития и современного мегаполиса.",
                AudioUrl = BuildTtsUrl("Landmark 81 — самое высокое здание во Вьетнаме, яркий символ динамичного развития и современного мегаполиса.", "ru"),
                DurationSeconds = 12
            },

            // 5. Bến Nhà Rồng
            new PoiTranslation
            {
                Id = 21,
                PoiId = 5,
                LanguageCode = "vi",
                Title = "Bến Nhà Rồng",
                Description = "Bến Nhà Rồng bên bờ sông Sài Gòn là di tích lịch sử đặc biệt, nơi người thanh niên Nguyễn Tất Thành ra đi tìm đường cứu nước năm 1911.",
                AudioUrl = BuildTtsUrl("Bến Nhà Rồng bên bờ sông Sài Gòn là di tích lịch sử đặc biệt, nơi người thanh niên Nguyễn Tất Thành ra đi tìm đường cứu nước năm 1911.", "vi"),
                DurationSeconds = 14
            },
            new PoiTranslation
            {
                Id = 22,
                PoiId = 5,
                LanguageCode = "zh",
                Title = "龙屋港",
                Description = "龙屋港坐落于西贡河畔，是一处重要历史遗址，1911年青年阮必成在此乘船启程追寻救国之路。",
                AudioUrl = BuildTtsUrl("龙屋港坐落于西贡河畔，是一处重要历史遗址，1911年青年阮必成在此乘船启程追寻救国之路。", "zh-CN"),
                DurationSeconds = 11
            },
            new PoiTranslation
            {
                Id = 23,
                PoiId = 5,
                LanguageCode = "en",
                Title = "Dragon Wharf (Nha Rong Wharf)",
                Description = "Nha Rong Wharf along the Saigon River is a revered historical monument from where President Ho Chi Minh departed in 1911.",
                AudioUrl = BuildTtsUrl("Nha Rong Wharf along the Saigon River is a revered historical monument from where President Ho Chi Minh departed in 1911.", "en"),
                DurationSeconds = 13
            },
            new PoiTranslation
            {
                Id = 24,
                PoiId = 5,
                LanguageCode = "fr",
                Title = "Quai Nha Rong",
                Description = "Le quai Nha Rong au bord de la rivière de Saïgon est le site mémorial d'où le président Hô Chi Minh est parti en 1911.",
                AudioUrl = BuildTtsUrl("Le quai Nha Rong au bord de la rivière de Saïgon est le site mémorial d'où le président Hô Chi Minh est parti en 1911.", "fr"),
                DurationSeconds = 13
            },
            new PoiTranslation
            {
                Id = 25,
                PoiId = 5,
                LanguageCode = "ru",
                Title = "Пристань Няронг",
                Description = "Пристань Няронг на реке Сайгон — святое историческое место, откуда в 1911 году Хо Ши Мин отправился в путь за независимость родины.",
                AudioUrl = BuildTtsUrl("Пристань Няронг на реке Сайгон — святое историческое место, откуда в 1911 году Хо Ши Мин отправился в путь за независимость родины.", "ru"),
                DurationSeconds = 14
            }
        );

        // QR Code Tokens
        modelBuilder.Entity<QrCode>().HasData(
            new QrCode { Id = 1, PoiId = 1, QrToken = "qr-cbt-01", ScanCount = 0, IsActive = true },
            new QrCode { Id = 2, PoiId = 2, QrToken = "qr-nhtp-02", ScanCount = 0, IsActive = true },
            new QrCode { Id = 3, PoiId = 3, QrToken = "qr-btct-03", ScanCount = 0, IsActive = true },
            new QrCode { Id = 4, PoiId = 4, QrToken = "qr-lm81-04", ScanCount = 0, IsActive = true },
            new QrCode { Id = 5, PoiId = 5, QrToken = "qr-bnr-05", ScanCount = 0, IsActive = true }
        );
    }
}