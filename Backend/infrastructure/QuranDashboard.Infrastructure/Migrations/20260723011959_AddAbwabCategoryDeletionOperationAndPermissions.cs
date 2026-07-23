using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAbwabCategoryDeletionOperationAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "deletion_operation_id",
                table: "abwab_categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.InsertData(
                table: "permission_codes",
                columns: new[] { "code", "dashboard_admin_baseline", "system_owner_only" },
                values: new object[,]
                {
                    { "category.add", false, false },
                    { "category.delete", false, false },
                    { "category.edit", false, false },
                    { "category.move", false, false },
                    { "category.reorder", false, false },
                    { "category.view", false, false },
                    { "protection.apply", false, false },
                    { "protection.lift", false, false },
                    { "protection.view", false, false },
                    { "section.add", false, false },
                    { "section.delete", false, false },
                    { "section.edit", false, false },
                    { "section.reorder", false, false },
                    { "section.view", false, false }
                });

            migrationBuilder.CreateIndex(
                name: "ix_abwab_categories_deletion_operation_id",
                table: "abwab_categories",
                column: "deletion_operation_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_abwab_categories_deletion_operation_id",
                table: "abwab_categories");

            migrationBuilder.DeleteData(
                table: "permission_codes",
                keyColumn: "code",
                keyValue: "category.add");

            migrationBuilder.DeleteData(
                table: "permission_codes",
                keyColumn: "code",
                keyValue: "category.delete");

            migrationBuilder.DeleteData(
                table: "permission_codes",
                keyColumn: "code",
                keyValue: "category.edit");

            migrationBuilder.DeleteData(
                table: "permission_codes",
                keyColumn: "code",
                keyValue: "category.move");

            migrationBuilder.DeleteData(
                table: "permission_codes",
                keyColumn: "code",
                keyValue: "category.reorder");

            migrationBuilder.DeleteData(
                table: "permission_codes",
                keyColumn: "code",
                keyValue: "category.view");

            migrationBuilder.DeleteData(
                table: "permission_codes",
                keyColumn: "code",
                keyValue: "protection.apply");

            migrationBuilder.DeleteData(
                table: "permission_codes",
                keyColumn: "code",
                keyValue: "protection.lift");

            migrationBuilder.DeleteData(
                table: "permission_codes",
                keyColumn: "code",
                keyValue: "protection.view");

            migrationBuilder.DeleteData(
                table: "permission_codes",
                keyColumn: "code",
                keyValue: "section.add");

            migrationBuilder.DeleteData(
                table: "permission_codes",
                keyColumn: "code",
                keyValue: "section.delete");

            migrationBuilder.DeleteData(
                table: "permission_codes",
                keyColumn: "code",
                keyValue: "section.edit");

            migrationBuilder.DeleteData(
                table: "permission_codes",
                keyColumn: "code",
                keyValue: "section.reorder");

            migrationBuilder.DeleteData(
                table: "permission_codes",
                keyColumn: "code",
                keyValue: "section.view");

            migrationBuilder.DropColumn(
                name: "deletion_operation_id",
                table: "abwab_categories");
        }
    }
}
