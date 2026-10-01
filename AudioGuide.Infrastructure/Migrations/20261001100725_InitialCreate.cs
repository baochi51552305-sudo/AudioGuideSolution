using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AudioGuide.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pois",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Location = table.Column<Point>(type: "geography", nullable: false),
                    TriggerRadiusMeters = table.Column<double>(type: "float", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pois", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PoiTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PoiId = table.Column<int>(type: "int", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AudioUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoiTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoiTranslations_Pois_PoiId",
                        column: x => x.PoiId,
                        principalTable: "Pois",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QrCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PoiId = table.Column<int>(type: "int", nullable: false),
                    QrToken = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ScanCount = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QrCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QrCodes_Pois_PoiId",
                        column: x => x.PoiId,
                        principalTable: "Pois",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Pois",
                columns: new[] { "Id", "Code", "CreatedAt", "IsActive", "Location", "TriggerRadiusMeters" },
                values: new object[,]
                {
                    { 1, "DINH_DOC_LAP", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (106.6953 10.777)"), 20.0 },
                    { 2, "NHA_THO_DUC_BA", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (106.699 10.7798)"), 15.0 },
                    { 3, "BUU_DIEN_TRUNG_TAM", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, (NetTopologySuite.Geometries.Point)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POINT (106.6999 10.7799)"), 15.0 }
                });

            migrationBuilder.InsertData(
                table: "PoiTranslations",
                columns: new[] { "Id", "AudioUrl", "Description", "DurationSeconds", "LanguageCode", "PoiId", "Title" },
                values: new object[,]
                {
                    { 1, "https://cdn.example.com/audio/vi/dinh_doc_lap.mp3", "Di tích lịch sử quốc gia đặc biệt, biểu tượng của sự thống nhất đất nước.", 180, "vi", 1, "Dinh Độc Lập" },
                    { 2, "https://cdn.example.com/audio/en/dinh_doc_lap.mp3", "A special national historical relic and architectural landmark in District 1.", 175, "en", 1, "Independence Palace" },
                    { 3, "https://cdn.example.com/audio/vi/nha_tho_duc_ba.mp3", "Kiệt tác kiến trúc cổ kính phong cách Roman và Gothic nằm giữa trung tâm Sài Gòn.", 150, "vi", 2, "Nhà thờ Đức Bà Sài Gòn" },
                    { 4, "https://cdn.example.com/audio/en/nha_tho_duc_ba.mp3", "An iconic cathedral built during the French colonial period.", 145, "en", 2, "Notre-Dame Cathedral Basilica of Saigon" },
                    { 5, "https://cdn.example.com/audio/vi/buu_dien_trung_tam.mp3", "Công trình kiến trúc mang đậm dấu ấn phong cách Pháp được khánh thành vào cuối thế kỷ 19.", 120, "vi", 3, "Bưu điện Trung tâm Thành phố" },
                    { 6, "https://cdn.example.com/audio/en/buu_dien_trung_tam.mp3", "One of the oldest and most beautiful post offices in Southeast Asia.", 115, "en", 3, "Saigon Central Post Office" }
                });

            migrationBuilder.InsertData(
                table: "QrCodes",
                columns: new[] { "Id", "IsActive", "PoiId", "QrToken", "ScanCount" },
                values: new object[,]
                {
                    { 1, true, 1, "qr-ddl-01", 0 },
                    { 2, true, 2, "qr-ntdb-02", 0 },
                    { 3, true, 3, "qr-bdsg-03", 0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pois_Code",
                table: "Pois",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PoiTranslations_PoiId_LanguageCode",
                table: "PoiTranslations",
                columns: new[] { "PoiId", "LanguageCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QrCodes_PoiId",
                table: "QrCodes",
                column: "PoiId");

            migrationBuilder.CreateIndex(
                name: "IX_QrCodes_QrToken",
                table: "QrCodes",
                column: "QrToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PoiTranslations");

            migrationBuilder.DropTable(
                name: "QrCodes");

            migrationBuilder.DropTable(
                name: "Pois");
        }
    }
}
