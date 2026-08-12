using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EBayClone.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddListingViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ListingViews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastViewedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListingViews_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ListingViews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListingViews_ListingId",
                table: "ListingViews",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_ListingViews_UserId_LastViewedAt",
                table: "ListingViews",
                columns: new[] { "UserId", "LastViewedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ListingViews_UserId_ListingId",
                table: "ListingViews",
                columns: new[] { "UserId", "ListingId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListingViews");
        }
    }
}
