using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTrackerApp.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFieldsToTimeEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add CreatedBy column
            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "TimeEntries",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1); // Default to admin user ID

            // Add CreatedAt column if it doesn't exist
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TimeEntries",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            // Create foreign key for CreatedBy
            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_CreatedBy",
                table: "TimeEntries",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeEntries_Users_CreatedBy",
                table: "TimeEntries",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeEntries_Users_CreatedBy",
                table: "TimeEntries");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_CreatedBy",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TimeEntries");
        }
    }
}
