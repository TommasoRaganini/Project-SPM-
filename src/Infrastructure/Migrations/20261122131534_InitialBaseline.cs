
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class InitialBaseline : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Baseline: if the database is newly initialized, create the base itineraries table.
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS itineraries (
                    ""Id"" uuid NOT NULL,
                    ""Title"" character varying(200) NOT NULL,
                    ""Theme"" character varying(100) NOT NULL,
                    ""Description"" character varying(2000) NOT NULL,
                    ""GeographicalArea"" character varying(200) NOT NULL,
                    ""ResponsibleEditorId"" character varying(100) NOT NULL,
                    ""CreatedAtUtc"" timestamp with time zone NOT NULL,
                    ""Status"" character varying(50) NOT NULL,
                    CONSTRAINT ""PK_itineraries"" PRIMARY KEY (""Id"")
                );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS itineraries;");
        }
    }
}
