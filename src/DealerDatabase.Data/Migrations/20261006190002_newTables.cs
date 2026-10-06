using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DealerDatabase.Data.Migrations
{
    /// <inheritdoc />
    public partial class newTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressLine1",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine2",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressSource",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AvgDaysInStock",
                table: "Dealers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "AvgListedPrice",
                table: "Dealers",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CompanyNumber",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyStatus",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyType",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacebookUrl",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FcaFrn",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FcaStatus",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FcaStatusEffectiveDate",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FranchiseMake",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GoogleRating",
                table: "Dealers",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "GoogleReviewCount",
                table: "Dealers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IcoExpiryDate",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IcoRegistrationNumber",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IncorporationDate",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstagramUrl",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InventoryCount",
                table: "Dealers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegalName",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Postcode",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SafExpiryDate",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SafStatus",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerType",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SoldLast30Days",
                table: "Dealers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StockFeedProvider",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Town",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TradingName",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TrustpilotScore",
                table: "Dealers",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "VatNumber",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarrantyOffered",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebsitePlatform",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DealerFcaPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DealerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Permission = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerFcaPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DealerFcaPermissions_Dealers_DealerId",
                        column: x => x.DealerId,
                        principalTable: "Dealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DealerOfficers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DealerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: true),
                    AppointedOn = table.Column<string>(type: "TEXT", nullable: true),
                    Occupation = table.Column<string>(type: "TEXT", nullable: true),
                    Nationality = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerOfficers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DealerOfficers_Dealers_DealerId",
                        column: x => x.DealerId,
                        principalTable: "Dealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DealerSicCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DealerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerSicCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DealerSicCodes_Dealers_DealerId",
                        column: x => x.DealerId,
                        principalTable: "Dealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DealerFcaPermissions_DealerId_Permission",
                table: "DealerFcaPermissions",
                columns: new[] { "DealerId", "Permission" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DealerOfficers_DealerId_Name",
                table: "DealerOfficers",
                columns: new[] { "DealerId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DealerSicCodes_DealerId_Code",
                table: "DealerSicCodes",
                columns: new[] { "DealerId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DealerFcaPermissions");

            migrationBuilder.DropTable(
                name: "DealerOfficers");

            migrationBuilder.DropTable(
                name: "DealerSicCodes");

            migrationBuilder.DropColumn(
                name: "AddressLine1",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "AddressLine2",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "AddressSource",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "AvgDaysInStock",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "AvgListedPrice",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "CompanyNumber",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "CompanyStatus",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "CompanyType",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "FacebookUrl",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "FcaFrn",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "FcaStatus",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "FcaStatusEffectiveDate",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "FranchiseMake",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "GoogleRating",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "GoogleReviewCount",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "IcoExpiryDate",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "IcoRegistrationNumber",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "IncorporationDate",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "InstagramUrl",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "InventoryCount",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "LegalName",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "Postcode",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "SafExpiryDate",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "SafStatus",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "SellerType",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "SoldLast30Days",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "StockFeedProvider",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "Town",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "TradingName",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "TrustpilotScore",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "VatNumber",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "WarrantyOffered",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "WebsitePlatform",
                table: "Dealers");
        }
    }
}
