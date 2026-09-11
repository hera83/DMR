using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DMR.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexOnDmrDatasetDatabasePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DmrDatasets_DatabasePath",
                table: "DmrDatasets",
                column: "DatabasePath",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DmrDatasets_DatabasePath",
                table: "DmrDatasets");
        }
    }
}
