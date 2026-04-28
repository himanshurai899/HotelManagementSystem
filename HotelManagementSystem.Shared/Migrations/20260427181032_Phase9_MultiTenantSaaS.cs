using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HotelManagementSystem.Shared.Migrations
{
    /// <inheritdoc />
    public partial class Phase9_MultiTenantSaaS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Staff",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "RoomTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Rooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Amenities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subdomain = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Plan = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserTenants",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    TenantRole = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTenants", x => new { x.UserId, x.TenantId });
                    table.ForeignKey(
                        name: "FK_UserTenants_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserTenants_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "e46c60c4-acb8-44e0-811a-1da4affe27c8", "AQAAAAIAAYagAAAAEK/1e8EB7LOkabA0FyjbAZo1NwqnXzGL1DTu5f+xaazHQIyvLFVSGw+uHdO2+CF5jw==" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "af1c89e0-7521-40c3-b560-6ffc56800bf5", "AQAAAAIAAYagAAAAEIDdIvUqe5/G9YF6k7AErm2W4tpMFPFF3vHkLfCvnlmfzzbpDvRfine8yZ9ajpQVOQ==" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "5c5c1aed-0c60-491a-a699-7ea2600d4590", "AQAAAAIAAYagAAAAELzs1h/1stIoy4k8yvB/G9eA0uH+ATBEaXYtWOe0sN4EWX1hPPWAcbZMTDLa3E6Huw==" });

            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[] { "Id", "CreatedAt", "IsActive", "Name", "Plan", "Subdomain" },
                values: new object[] { 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Default", 2, "default" });

            migrationBuilder.InsertData(
                table: "UserTenants",
                columns: new[] { "TenantId", "UserId", "JoinedAt", "TenantRole" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Administrator" },
                    { 1, 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 1, 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Administrator" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Staff_TenantId",
                table: "Staff",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypes_TenantId",
                table: "RoomTypes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_TenantId",
                table: "Rooms",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId",
                table: "Invoices",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TenantId",
                table: "Bookings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Amenities_TenantId",
                table: "Amenities",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Subdomain",
                table: "Tenants",
                column: "Subdomain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTenants_TenantId",
                table: "UserTenants",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Amenities_Tenants_TenantId",
                table: "Amenities",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Tenants_TenantId",
                table: "Bookings",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Tenants_TenantId",
                table: "Invoices",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_Tenants_TenantId",
                table: "Rooms",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RoomTypes_Tenants_TenantId",
                table: "RoomTypes",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Staff_Tenants_TenantId",
                table: "Staff",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Amenities_Tenants_TenantId",
                table: "Amenities");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Tenants_TenantId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Tenants_TenantId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_Tenants_TenantId",
                table: "Rooms");

            migrationBuilder.DropForeignKey(
                name: "FK_RoomTypes_Tenants_TenantId",
                table: "RoomTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_Staff_Tenants_TenantId",
                table: "Staff");

            migrationBuilder.DropTable(
                name: "UserTenants");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Staff_TenantId",
                table: "Staff");

            migrationBuilder.DropIndex(
                name: "IX_RoomTypes_TenantId",
                table: "RoomTypes");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_TenantId",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_TenantId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_TenantId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Amenities_TenantId",
                table: "Amenities");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Staff");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "RoomTypes");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Amenities");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "37693ae5-b8d7-4f29-98df-74a94638899e", "AQAAAAIAAYagAAAAEInYhijSQ+EqAb/xqIy0afIqI/ITm+WAc2TckVQyktOudTsdh4a62lqXqCGwHVAMtA==" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "2c005a43-e9fb-4b50-9084-5e2746e59934", "AQAAAAIAAYagAAAAEI7Fjc91/INmdM40ULwKtqIQFju3adaTEHP4yJZ/6wr0qWau2h5jmTvkLS0jm9Lx0A==" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "351fda4c-bbb4-4f1c-9eae-a6fc7716507c", "AQAAAAIAAYagAAAAEI79s9jSfXHqIqU92aueAmlxWy5DzLOBoXf1b9Lis79RDnda6zVU/sO3jiLgbbXVrA==" });
        }
    }
}
