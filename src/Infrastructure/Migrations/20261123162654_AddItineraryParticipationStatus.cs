using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddItineraryParticipationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ParticipationStatus",
                table: "itineraries",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Closed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParticipationStatus",
                table: "itineraries");
        }
    }
}
