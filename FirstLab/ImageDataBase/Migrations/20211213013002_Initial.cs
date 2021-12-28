using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ImageDataBase.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImageData",
                columns: table => new
                {
                    ImageDataId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImageDataArray = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageData", x => x.ImageDataId);
                });

            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    ImageId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImageName = table.Column<string>(type: "TEXT", nullable: true),
                    ImageHash = table.Column<string>(type: "TEXT", nullable: true),
                    ImagePhotoImageDataId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.ImageId);
                    table.ForeignKey(
                        name: "FK_Images_ImageData_ImagePhotoImageDataId",
                        column: x => x.ImagePhotoImageDataId,
                        principalTable: "ImageData",
                        principalColumn: "ImageDataId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImageObject",
                columns: table => new
                {
                    ImageObjectId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImageObjectName = table.Column<string>(type: "TEXT", nullable: true),
                    X1 = table.Column<float>(type: "REAL", nullable: false),
                    Y1 = table.Column<float>(type: "REAL", nullable: false),
                    X2 = table.Column<float>(type: "REAL", nullable: false),
                    Y2 = table.Column<float>(type: "REAL", nullable: false),
                    ImageId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageObject", x => x.ImageObjectId);
                    table.ForeignKey(
                        name: "FK_ImageObject_Images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "Images",
                        principalColumn: "ImageId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImageObject_ImageId",
                table: "ImageObject",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Images_ImagePhotoImageDataId",
                table: "Images",
                column: "ImagePhotoImageDataId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImageObject");

            migrationBuilder.DropTable(
                name: "Images");

            migrationBuilder.DropTable(
                name: "ImageData");
        }
    }
}
