using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagementSystem.Shared.Migrations
{
    /// <inheritdoc />
    public partial class Phase12d_RoomBlocking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoomBlocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BlockType = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomBlocks_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "12e7fe1b-e4f3-44e6-8071-94c840268d0e", "AQAAAAIAAYagAAAAELg0aL4RE0t/YLRVKt3+hjJLX3mvnkPCxKL8HpdQzT076PiWW7M5IRwqGrbuCQ7dng==" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "5969b63d-49c2-4dc0-8074-5966052a2089", "AQAAAAIAAYagAAAAEH6/VIhcyjJ19oJtxJdvvH0aQkYsFC7UWWk7S9Kdi1OinI4S2RNbGEISNmVia9snCg==" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "a562afc3-4ff4-4f45-bc51-b50892451466", "AQAAAAIAAYagAAAAEPp6hiEmbBETSkct00jH4nGC1SRA8h49l+EqSPRhNdIwuC4j0GvIRQW2iJSQ0IjflA==" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomBlocks_RoomId",
                table: "RoomBlocks",
                column: "RoomId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoomBlocks");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "480ec638-70f9-4421-8203-ea32caa0dfa7", "AQAAAAIAAYagAAAAEN1jJZKR0N0kkX0uEEMFWwldWfqEzIRKnkwsojb+Cqbe4qLv5SszisDGpslT9PJA0A==" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "91aa0dbd-fb2b-496c-8e0b-4ab441820ca5", "AQAAAAIAAYagAAAAEAinLcyOic6hJTujQHIfsS4sOt1a2VIBP03D+XEQSKKYyNFc+w/gwhJ/MzrqiVOj7Q==" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "873681e1-2c8e-447e-b2b4-82e3318a2515", "AQAAAAIAAYagAAAAEKYX74rJ4GTXsWPfWkrd34PCW88OVXqBCA8SNE+VxCjH+OUuMPwPXNEjWHhr8ZG8iA==" });
        }
    }
}
