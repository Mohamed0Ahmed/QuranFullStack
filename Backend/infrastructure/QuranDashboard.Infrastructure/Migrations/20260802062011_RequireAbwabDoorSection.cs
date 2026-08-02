using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RequireAbwabDoorSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "section_id",
                table: "abwab_doors",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "section_id",
                table: "abwab_doors",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
