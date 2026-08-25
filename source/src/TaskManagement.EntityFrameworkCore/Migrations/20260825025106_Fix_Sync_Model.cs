using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Sync_Model : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Thêm cột UserId vào bảng TaskActivityLogs (nếu chưa có)
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "TaskActivityLogs",
                type: "uniqueidentifier",
                nullable: true);

            // Thêm cột SubmissionFilesJson vào bảng Tags (nếu chưa có)
            migrationBuilder.AddColumn<string>(
                name: "SubmissionFilesJson",
                table: "Tags",
                type: "nvarchar(max)",
                nullable: true);

            // Thêm cột SubmissionNote vào bảng Tags (nếu chưa có)
            migrationBuilder.AddColumn<string>(
                name: "SubmissionNote",
                table: "Tags",
                type: "nvarchar(max)",
                nullable: true);

            // Đã loại bỏ lệnh CREATE TABLE AppTaskComments vì bảng đã tồn tại trong DB
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TaskActivityLogs");

            migrationBuilder.DropColumn(
                name: "SubmissionFilesJson",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "SubmissionNote",
                table: "Tags");
        }
    }
}