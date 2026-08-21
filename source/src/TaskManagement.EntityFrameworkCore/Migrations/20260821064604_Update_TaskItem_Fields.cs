using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Migrations
{
    /// <inheritdoc />
    public partial class Update_TaskItem_Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreationTime",
                table: "TaskAttachments");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "TaskAttachments");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "TaskAttachments");

            migrationBuilder.AddColumn<int>(
                name: "ProgressPercent",
                table: "Tasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FileUrl",
                table: "TaskAttachments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "TaskItemId",
                table: "TaskAttachments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProgressPercent",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "FileUrl",
                table: "TaskAttachments");

            migrationBuilder.DropColumn(
                name: "TaskItemId",
                table: "TaskAttachments");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreationTime",
                table: "TaskAttachments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatorId",
                table: "TaskAttachments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "TaskAttachments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
