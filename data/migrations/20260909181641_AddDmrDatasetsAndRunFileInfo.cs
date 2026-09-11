using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DMR.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDmrDatasetsAndRunFileInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Message",
                table: "DmrWorkerRuns",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SourceFileModifiedUtc",
                table: "DmrWorkerRuns",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceFileName",
                table: "DmrWorkerRuns",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DmrDatasets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceFileName = table.Column<string>(type: "TEXT", nullable: false),
                    SourceFileModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DatabasePath = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DmrDatasets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DmrDatasets_CreatedAtUtc",
                table: "DmrDatasets",
                column: "CreatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DmrDatasets");

            migrationBuilder.DropColumn(
                name: "Message",
                table: "DmrWorkerRuns");

            migrationBuilder.DropColumn(
                name: "SourceFileModifiedUtc",
                table: "DmrWorkerRuns");

            migrationBuilder.DropColumn(
                name: "SourceFileName",
                table: "DmrWorkerRuns");
        }
    }
}
