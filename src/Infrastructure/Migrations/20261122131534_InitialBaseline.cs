
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class InitialBaseline : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Baseline: the existing database schema is already present.
            // No tables must be created.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Do not delete existing database tables.
        }
    }
}
