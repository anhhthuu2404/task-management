using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Migrations
{
    /// <inheritdoc />
    public partial class Add_TaskComment_Attachments_Table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           
            migrationBuilder.DropColumn(
                name: "DeleterId",
                table: "AppTaskComments");

            migrationBuilder.DropColumn(
                name: "DeletionTime",
                table: "AppTaskComments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AppTaskComments");

            migrationBuilder.DropColumn(
                name: "LastModificationTime",
                table: "AppTaskComments");

            migrationBuilder.DropColumn(
                name: "LastModifierId",
                table: "AppTaskComments");

            // Thêm các thuộc tính ABP AggregateRoot
            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "AppTaskComments",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExtraProperties",
                table: "AppTaskComments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            // Tạo bảng mới cho danh sách tệp đính kèm của Comment
            migrationBuilder.CreateTable(
                name: "TaskCommentAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    TaskCommentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskCommentAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskCommentAttachments_AppTaskComments_TaskCommentId",
                        column: x => x.TaskCommentId,
                        principalTable: "AppTaskComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskCommentAttachments_TaskCommentId",
                table: "TaskCommentAttachments",
                column: "TaskCommentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskCommentAttachments");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "AppTaskComments");

            migrationBuilder.DropColumn(
                name: "ExtraProperties",
                table: "AppTaskComments");

            migrationBuilder.AddColumn<Guid>(
                name: "DeleterId",
                table: "AppTaskComments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletionTime",
                table: "AppTaskComments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AppTaskComments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModificationTime",
                table: "AppTaskComments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifierId",
                table: "AppTaskComments",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}