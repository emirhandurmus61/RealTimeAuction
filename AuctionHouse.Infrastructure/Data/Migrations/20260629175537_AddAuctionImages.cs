using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuctionHouse.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuctionImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuctionImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Url = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    AuctionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuctionImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuctionImages_Auctions_AuctionId",
                        column: x => x.AuctionId,
                        principalTable: "Auctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuctionImages_AuctionId",
                table: "AuctionImages",
                column: "AuctionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuctionImages");
        }
    }
}
