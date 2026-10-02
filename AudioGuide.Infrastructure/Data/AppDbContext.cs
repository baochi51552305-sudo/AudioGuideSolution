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
    Description = "Chào mừng quý khách đến với Chợ Bến Thành. Đây là một trong những biểu tượng lịch sử và văn hóa đặc trưng nhất của Thành phố Hồ Chí Minh. Ngôi chợ được xây dựng từ đầu thế kỷ hai mươi với tháp đồng hồ bốn mặt nổi tiếng. Nơi đây không chỉ là trung tâm buôn bán tấp nập các mặt hàng thủ công mỹ nghệ, lụa tơ tằm và nông sản, mà còn là thiên đường ẩm thực đường phố truyền thống của phương Nam. Chúc quý khách có những trải nghiệm tham quan và mua sắm thú vị tại đây.",
    AudioUrl = "",
    DurationSeconds = 60
},
new PoiTranslation
{
    Id = 2,
    PoiId = 1,
    LanguageCode = "zh",
    Title = "滨城市场",
    Description = "欢迎光临滨城市场。这是胡志明市最具标志性的历史文化建筑之一。市场建于二十世纪初，拥有著名的四面钟楼钟塔。这里不仅是汇聚越南传统手工艺品、优质丝绸和热带特产的繁华贸易中心，更是品尝纯正越南南方风味街头美食的绝佳去处。祝您在滨城市场拥有一段难忘而愉快的探索之旅。",
    AudioUrl = "",
    DurationSeconds = 55
},
new PoiTranslation
{
    Id = 3,
    PoiId = 1,
    LanguageCode = "en",
    Title = "Ben Thanh Market",
    Description = "Welcome to Ben Thanh Market, one of the most celebrated cultural and historical landmarks in Ho Chi Minh City. Built in the early twentieth century, it is famous for its iconic four-faced clock tower. The market is not only a lively hub for Vietnamese handicrafts, textiles, and local souvenirs, but also a paradise for authentic street food lovers. Enjoy your exploration and memorable shopping experience here.",
    AudioUrl = "",
    DurationSeconds = 50
},
new PoiTranslation
{
    Id = 4,
    PoiId = 1,
    LanguageCode = "fr",
    Title = "Marché de Ben Thanh",
    Description = "Bienvenue au marché emblématique de Ben Thanh, véritable symbole historique et culturel de Hô Chi Minh-Ville. Érigé au début du vingtième siècle, ce marché est célèbre pour sa tour de l'horloge. C'est le lieu idéal pour dénicher de l'artisanat vietnamien, des soieries raffinées et déguster les meilleures spécialités gastronomiques de la rue saïgonnaise. Nous vous souhaitons une agréable visite.",
    AudioUrl = "",
    DurationSeconds = 55
},
new PoiTranslation
{
    Id = 5,
    PoiId = 1,
    LanguageCode = "ru",
    Title = "Рынок Бен Тхань",
    Description = "Добро пожаловать на исторический рынок Бен Тхань, один из главных культурных символов Хошимина. Построенный в начале двадцатого века, он знаменит своей башней с часами. Здесь можно приобрести традиционные вьетнамские сувениры, шёлк и ремесленные изделия, а также попробовать популярные блюда уличной кухни. Желаем вам приятных покупок и ярких впечатлений.",
    AudioUrl = "",
    DurationSeconds = 55
},

// 2. Nhà hát Thành phố
new PoiTranslation
{
    Id = 6,
    PoiId = 2,
    LanguageCode = "vi",
    Title = "Nhà hát Thành phố",
    Description = "Nhà hát Thành phố Hồ Chí Minh, còn được biết đến với tên gọi Nhà hát Lớn Sài Gòn, là công trình kiến trúc nghệ thuật cổ kính khánh thành vào năm 1900. Công trình mang đậm phong cách Phục hưng Pháp, với mặt tiền tinh xảo, hệ thống phù điêu nổi bật và không gian biểu diễn âm học chất lượng cao. Nơi đây thường xuyên tổ chức các buổi hòa nhạc hàn lâm, các vở múa đương đại và chương trình nghệ thuật quốc tế đỉnh cao.",
    AudioUrl = "",
    DurationSeconds = 60
},
new PoiTranslation
{
    Id = 7,
    PoiId = 2,
    LanguageCode = "zh",
    Title = "胡志明市大剧院",
    Description = "胡志明市大剧院，亦称西贡歌剧院，是一座于1900年正式落成的典雅历史建筑。该建筑具有浓郁的法国文艺复兴建筑风格，外墙雕刻精美，浮雕装饰生动典雅，内部剧场音响效果出众。如今，这里是胡志明市举办大型交响乐音乐会、现代舞剧以及国际高水准艺术表演的核心殿堂。",
    AudioUrl = "",
    DurationSeconds = 55
},
new PoiTranslation
{
    Id = 8,
    PoiId = 2,
    LanguageCode = "en",
    Title = "Saigon Opera House",
    Description = "The Saigon Opera House, also known as the Municipal Theatre, is an architectural masterpiece officially inaugurated in the year 1900. Designed in the ornate French colonial style, it features stunning facade sculptures, elegant relief carvings, and world-class acoustics. Today, the theater hosts prominent classical symphony concerts, contemporary Vietnamese ballets, and international stage performances.",
    AudioUrl = "",
    DurationSeconds = 50
},
new PoiTranslation
{
    Id = 9,
    PoiId = 2,
    LanguageCode = "fr",
    Title = "Opéra de Saïgon",
    Description = "L'Opéra de Saïgon, inauguré en 1900, est un remarquable chef-d'œuvre de l'architecture coloniale française inspiré du Petit Palais de Paris. Ses façades ornées de bas-reliefs raffinés et son acoustique exceptionnelle en font un lieu de spectacle prestigieux. Le théâtre accueille tout au long de l'année des concerts symphoniques, des ballets classiques et des créations artistiques contemporaines.",
    AudioUrl = "",
    DurationSeconds = 55
},
new PoiTranslation
{
    Id = 10,
    PoiId = 2,
    LanguageCode = "ru",
    Title = "Муниципальный театр Сайгона",
    Description = "Муниципальный оперный театр Сайгона — выдающийся памятник французской архитектуры, открытый в 1900 году. Фасад здания украшен великолепными скульптурами и барельефами, а зрительный зал обладает великолепной акустикой. Сегодня театр является главной сценой города для проведения симфонических концертов, балетных постановок и международных культурных фестивалей.",
    AudioUrl = "",
    DurationSeconds = 55
},

// 3. Bảo tàng Chứng tích Chiến tranh
new PoiTranslation
{
    Id = 11,
    PoiId = 3,
    LanguageCode = "vi",
    Title = "Bảo tàng Chứng tích Chiến tranh",
    Description = "Bảo tàng Chứng tích Chiến tranh là một trong những điểm đến lịch sử thu hút đông đảo du khách quốc tế nhất tại Việt Nam. Nơi đây hiện lưu giữ và trưng bày hàng ngàn tư liệu, hình ảnh và hiện vật chân thực về hậu quả chiến tranh tàn khốc. Không chỉ tái hiện lịch sử, bảo tàng còn truyền tải bức thông điệp mạnh mẽ về khát vọng gìn giữ hòa bình, tình hữu nghị giữa các dân tộc trên toàn thế giới.",
    AudioUrl = "",
    DurationSeconds = 60
},
new PoiTranslation
{
    Id = 12,
    PoiId = 3,
    LanguageCode = "zh",
    Title = "战争遗迹博物馆",
    Description = "战争遗迹博物馆是越南接待国际游客最多的历史博物馆之一。馆内收藏并展出了数以万计珍贵的战争历史照片、军事实物和文献档案，生动再现了战争的残酷教训。博物馆不仅让世人铭记历史，更向全世界发出了珍惜和平、增进各国人民之间友好情谊的崇高呼吁。",
    AudioUrl = "",
    DurationSeconds = 55
},
new PoiTranslation
{
    Id = 13,
    PoiId = 3,
    LanguageCode = "en",
    Title = "War Remnants Museum",
    Description = "The War Remnants Museum is one of the most visited historical institutions in Vietnam. It houses and displays thousands of historical documents, poignant photographs, and authentic wartime equipment that reflect the devastating realities of past conflicts. More than a record of the past, the museum delivers a heartfelt and enduring message advocating for global peace and cross-cultural friendship.",
    AudioUrl = "",
    DurationSeconds = 55
},
new PoiTranslation
{
    Id = 14,
    PoiId = 3,
    LanguageCode = "fr",
    Title = "Musée des vestiges de guerre",
    Description = "Le Musée des vestiges de guerre constitue un lieu de mémoire incontournable à Hô Chi Minh-Ville. À travers des expositions poignantes de photographies, d'armements et de documents historiques, le musée retrace les dures réalités des conflits armés. Il transmet à chaque visiteur un vibrant appel à la réconciliation, à la fraternité entre les peuples et à la préservation de la paix mondiale.",
    AudioUrl = "",
    DurationSeconds = 55
},
new PoiTranslation
{
    Id = 15,
    PoiId = 3,
    LanguageCode = "ru",
    Title = "Музей жертв войны",
    Description = "Музей жертв войны — одно из самых значимых и посещаемых исторических мест во Вьетнаме. В его залах представлены тысячи подлинных документов, фотографий и военной техники, свидетельствующих о последствиях военных конфликтов. Музей хранит память об истории и служит мощным призывом к взаимопониманию, дружбе между народами и защите мира во всем мире.",
    AudioUrl = "",
    DurationSeconds = 55
},

// 4. Landmark 81
new PoiTranslation
{
    Id = 16,
    PoiId = 4,
    LanguageCode = "vi",
    Title = "Landmark 81",
    Description = "Tọa lạc bên bờ sông Sài Gòn, Landmark 81 là tòa nhà chọc trời cao nhất Việt Nam và nằm trong nhóm những công trình cao nhất thế giới. Với thiết kế lấy cảm hứng từ bó tre truyền thống vươn lên bầu trời xanh, tòa tháp biểu trưng cho sự đoàn kết, thịnh vượng và sức mạnh vươn tầm của đất nước. Bên trong tòa nhà tích hợp trung tâm thương mại cao cấp, đài quan sát ngắm toàn cảnh thành phố và khu nghỉ dưỡng đẳng cấp.",
    AudioUrl = "",
    DurationSeconds = 60
},
new PoiTranslation
{
    Id = 17,
    PoiId = 4,
    LanguageCode = "zh",
    Title = "地标塔 81",
    Description = "地标塔81矗立在西贡河畔，不仅是越南第一摩天高楼，更是世界著名的高层建筑之一。其外观设计灵感源自越南传统竹丛昂首挺立的形象，寓意着团结、繁荣与不断开拓的时代精神。大厦集现代化高端购物中心、全景城市观景台以及豪华酒店于一体，是俯瞰胡志明市天际线的绝佳圣地。",
    AudioUrl = "",
    DurationSeconds = 55
},
new PoiTranslation
{
    Id = 18,
    PoiId = 4,
    LanguageCode = "en",
    Title = "Landmark 81",
    Description = "Rising majestically by the Saigon River, Landmark 81 is the tallest skyscraper in Vietnam and one of the tallest towers in the world. Inspired by the traditional Vietnamese bamboo bundle, its design symbolizes national unity, prosperity, and forward-looking ambition. The tower features a premier retail mall, a panoramic skydeck observatory, and luxury hospitality facilities.",
    AudioUrl = "",
    DurationSeconds = 50
},
new PoiTranslation
{
    Id = 19,
    PoiId = 4,
    LanguageCode = "fr",
    Title = "Landmark 81",
    Description = "Dressé fièrement au bord de la rivière de Saïgon, le gratte-ciel Landmark 81 est la plus haute tour du Vietnam. Inspirée par l'image traditionnelle du faisceau de bambous, sa silhouette spectaculaire incarne la solidarité, la prospérité et l'extraordinaire dynamisme économique de la métropole. La tour abrite un complexe commercial de luxe, des hôtels raffinés et une terrasse d'observation panoramique exceptionnelle.",
    AudioUrl = "",
    DurationSeconds = 55
},
new PoiTranslation
{
    Id = 20,
    PoiId = 4,
    LanguageCode = "ru",
    Title = "Небоскреб Landmark 81",
    Description = "Возвышающийся на берегу реки Сайгон, небоскрёб Landmark 81 является самым высоким зданием во Вьетнаме. Вдохновленный традиционным образом бамбуковой связки, его архитектурный облик символизирует национальное единство и стремительный прогресс. В здании расположены современный торгово-развлекательный комплекс, смотровая площадка с круговой панорамой и роскошный отель.",
    AudioUrl = "",
    DurationSeconds = 55
},

// 5. Bến Nhà Rồng
new PoiTranslation
{
    Id = 21,
    PoiId = 5,
    LanguageCode = "vi",
    Title = "Bến Nhà Rồng",
    Description = "Bến Nhà Rồng, hiện nay là Bảo tàng Hồ Chí Minh chi nhánh Thành phố Hồ Chí Minh, là một chứng tích lịch sử thiêng liêng. Vào ngày mùng 5 tháng 6 năm 1911, người thanh niên Nguyễn Tất Thành đã từ bến cảng này bước lên con tàu Amiral Latouche-Tréville ra đi tìm đường cứu nước. Tòa nhà có lối kiến trúc phương Tây kết hợp đôi rồng gắn trên nóc đặc trưng. Nơi đây hiện lưu giữ nhiều hiện vật quý báu gắn liền với cuộc đời hoạt động cách mạng của Bác Hồ.",
    AudioUrl = "",
    DurationSeconds = 65
},
new PoiTranslation
{
    Id = 22,
    PoiId = 5,
    LanguageCode = "zh",
    Title = "龙屋港",
    Description = "龙屋港如今是胡志明博物馆胡志明市分馆所在地，具有极其重要的历史意义。1911年6月5日，青年阮必成正是从这一港口登上拉图什-特雷维尔号商船，踏上了探寻国家独立解放的漫长道路。建筑巧妙融合了西方建筑风格与中国传统双龙戏珠的屋顶雕塑，馆内陈列着大量关于胡志明主席革命生涯的珍贵历史文物。",
    AudioUrl = "",
    DurationSeconds = 60
},
new PoiTranslation
{
    Id = 23,
    PoiId = 5,
    LanguageCode = "en",
    Title = "Dragon Wharf (Nha Rong Wharf)",
    Description = "Nha Rong Wharf, now known as the Ho Chi Minh Museum of Ho Chi Minh City, holds profound historical significance. On June 5th, 1911, the young patriot Nguyen Tat Thanh departed from here aboard the ship Amiral Latouche-Treville to seek salvation and independence for his nation. Characterized by European architecture crowned with two iconic ceramic dragons, the museum preserves invaluable relics of President Ho Chi Minh's lifelong revolutionary journey.",
    AudioUrl = "",
    DurationSeconds = 60
},
new PoiTranslation
{
    Id = 24,
    PoiId = 5,
    LanguageCode = "fr",
    Title = "Quai Nha Rong",
    Description = "Le quai Nha Rong, abritant aujourd'hui le musée Hô Chi Minh, est un lieu d'une importance historique capitale. C'est ici même, le 5 juin 1911, que le jeune patriote Nguyen Tat Thanh embarqua à bord d'un navire marchand pour trouver la voie de la libération nationale. Le bâtiment combine une élégante architecture française et une toiture ornée de deux dragons emblématiques, abritant de précieuses collections retraçant le parcours du président Hô Chi Minh.",
    AudioUrl = "",
    DurationSeconds = 60
},
new PoiTranslation
{
    Id = 25,
    PoiId = 5,
    LanguageCode = "ru",
    Title = "Пристань Няронг",
    Description = "Пристань Няронг, где сегодня расположен Музей Хо Ши Мина, является священным памятником вьетнамской истории. Именно отсюда 5 июня 1911 года молодой патриот Нгуен Тат Тхань отправился в дальний путь на поиски независимости для своего народа. Здание отличается гармоничным сочетанием французского колониального стиля и украшено двумя драконами на крыше, сохраняя бесценные реликвии революционной эпохи.",
    AudioUrl = "",
    DurationSeconds = 60
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