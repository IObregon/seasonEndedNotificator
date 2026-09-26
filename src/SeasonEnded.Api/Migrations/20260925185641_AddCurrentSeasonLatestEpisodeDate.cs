using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeasonEnded.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentSeasonLatestEpisodeDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "CurrentSeasonLatestEpisodeDate",
                table: "Shows",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentSeasonLatestEpisodeDate",
                table: "Shows");
        }
    }
}
