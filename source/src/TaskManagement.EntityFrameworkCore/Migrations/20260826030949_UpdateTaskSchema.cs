using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTaskSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "Tasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Tasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FieldName",
                table: "TaskActivityLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewValue",
                table: "TaskActivityLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OldValue",
                table: "TaskActivityLogs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Position",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "FieldName",
                table: "TaskActivityLogs");

            migrationBuilder.DropColumn(
                name: "NewValue",
                table: "TaskActivityLogs");

            migrationBuilder.DropColumn(
                name: "OldValue",
                table: "TaskActivityLogs");
        }
    }
}
