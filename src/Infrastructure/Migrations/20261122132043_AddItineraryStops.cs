
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddItineraryStops : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "itinerary_stops",
                columns: table => new
                {
                    Id = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),

                    ItineraryId = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),

                    Name = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false),

                    Description = table.Column<string>(
                        type: "character varying(2000)",
                        maxLength: 2000,
                        nullable: false),

                    Latitude = table.Column<double>(
                        type: "double precision",
                        nullable: false),

                    Longitude = table.Column<double>(
                        type: "double precision",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_itinerary_stops",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_itinerary_stops_itineraries_ItineraryId",
                        column: x => x.ItineraryId,
                        principalTable: "itineraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_itinerary_stops_ItineraryId",
                table: "itinerary_stops",
                column: "ItineraryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "itinerary_stops");
        }
    }
}
