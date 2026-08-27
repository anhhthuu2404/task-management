using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Migrations
{
    /// <inheritdoc />
    public partial class Add_Recurring_Columns_To_TaskItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Frequency",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRecurring",
                table: "Tasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastGeneratedDate",
                table: "Tasks",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Frequency",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "IsRecurring",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "LastGeneratedDate",
                table: "Tasks");
        }
    }
}
