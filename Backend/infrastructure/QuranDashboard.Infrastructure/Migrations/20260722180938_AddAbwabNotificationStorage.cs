using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAbwabNotificationStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "abwab_notification_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_subject = table.Column<string>(type: "text", nullable: false),
                    source_identity = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abwab_notification_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "abwab_notification_read_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_subject = table.Column<string>(type: "text", nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abwab_notification_read_states", x => x.id);
                    table.ForeignKey(
                        name: "FK_abwab_notification_read_states_abwab_notification_records_n~",
                        column: x => x.notification_id,
                        principalTable: "abwab_notification_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_abwab_notification_read_states_notification_id_recipient_su~",
                table: "abwab_notification_read_states",
                columns: new[] { "notification_id", "recipient_subject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_abwab_notification_records_recipient_subject",
                table: "abwab_notification_records",
                column: "recipient_subject");

            migrationBuilder.CreateIndex(
                name: "IX_abwab_notification_records_source_identity",
                table: "abwab_notification_records",
                column: "source_identity",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "abwab_notification_read_states");

            migrationBuilder.DropTable(
                name: "abwab_notification_records");
        }
    }
}
