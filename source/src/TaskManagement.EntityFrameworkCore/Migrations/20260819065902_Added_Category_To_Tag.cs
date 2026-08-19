using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Migrations
{
    /// <inheritdoc />
    public partial class Added_Category_To_Tag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Tags",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_CategoryId",
                table: "Tags",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_Categories_CategoryId",
                table: "Tags",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tags_Categories_CategoryId",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Tags_CategoryId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Tags");
        }
    }
}
