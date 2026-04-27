using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HotelManagementSystem.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddSuperAdminAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { 4, null, "SuperAdmin", "SUPERADMIN" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ConcurrencyStamp", "EmailConfirmed", "NormalizedEmail", "PasswordHash", "SecurityStamp" },
                values: new object[] { "5d5b5d7b-e208-41d6-a56e-e5e0e5853bde", true, "ADMIN@EXAMPLE.COM", "AQAAAAIAAYagAAAAEK6A2WgZClodb8mcwJfqlpYDVtdt2kgGyjlppPWTe6Zvrsr//OFg0SSpl8LPLeo3Vg==", "ADMIN-STATIC-STAMP-0001" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ConcurrencyStamp", "EmailConfirmed", "NormalizedEmail", "PasswordHash", "SecurityStamp" },
                values: new object[] { "214b7a44-81ff-4742-8b4e-3d91f6657829", true, "GUEST@EXAMPLE.COM", "AQAAAAIAAYagAAAAEOEexhL12tCUhO9PtWGMQo1bLycY8TojKunkQuD9Q8niasjNbI1bYg+PzG73Voiq8g==", "GUEST-STATIC-STAMP-0001" });

            migrationBuilder.InsertData(
                table: "AspNetRoleClaims",
                columns: new[] { "Id", "ClaimType", "ClaimValue", "RoleId" },
                values: new object[,]
                {
                    { 6, "Permission", "ManageUsers", 4 },
                    { 7, "Permission", "ManageRoles", 4 },
                    { 8, "Permission", "ManagePermissions", 4 },
                    { 9, "Permission", "ManageRooms", 4 },
                    { 10, "Permission", "ManageRoomTypes", 4 },
                    { 11, "Permission", "ManageAmenities", 4 },
                    { 12, "Permission", "ManageStaff", 4 },
                    { 13, "Permission", "ManageBookings", 4 },
                    { 14, "Permission", "ViewReports", 4 },
                    { 15, "Permission", "ViewDashboard", 4 },
                    { 16, "Permission", "MakeBooking", 4 }
                });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "Email", "EmailConfirmed", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "RoleId", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[] { 3, 0, "c0b8a4af-b89b-45b7-8015-23d92533a99c", "superadmin@example.com", true, false, null, "SUPERADMIN@EXAMPLE.COM", "SUPERADMIN", "AQAAAAIAAYagAAAAEOkrR31LQIa/wC3ZigNvYlNwDMZdIB/dqG6MEP/yZiYNuFEcrfwm02OIvrPf2wNgWw==", "1112223333", false, 4, "SUPERADMIN-STATIC-STAMP-0001", false, "superadmin" });

            migrationBuilder.InsertData(
                table: "AspNetUserClaims",
                columns: new[] { "Id", "ClaimType", "ClaimValue", "UserId" },
                values: new object[,]
                {
                    { 5, "Permission", "FullAccess", 3 },
                    { 6, "Department", "Management", 3 }
                });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { 4, 3 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "AspNetUserClaims",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "AspNetUserClaims",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 4, 3 });

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ConcurrencyStamp", "EmailConfirmed", "NormalizedEmail", "PasswordHash", "SecurityStamp" },
                values: new object[] { "91c89de0-dd56-43d8-b663-a361f4bb70e1", false, null, "AQAAAAIAAYagAAAAEE1v9jn+YSdJz1asa6XAaWlGMH6ELZtjfwUKlRcfsmq4AeogMzefvL4UuWaehiUcKg==", null });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ConcurrencyStamp", "EmailConfirmed", "NormalizedEmail", "PasswordHash", "SecurityStamp" },
                values: new object[] { "4822fc11-f50f-4789-b252-76f1465d312a", false, null, "AQAAAAIAAYagAAAAEBcSfsMifuShfqNjRd2K7nI2oFzosZoZqQc0NNMYpPI/nfq7ZIh+uiiMgPqunKd16Q==", null });
        }
    }
}
