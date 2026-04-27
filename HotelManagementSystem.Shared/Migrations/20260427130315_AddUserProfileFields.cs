using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagementSystem.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePhotoUrl",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ConcurrencyStamp", "FirstName", "LastName", "PasswordHash", "ProfilePhotoUrl" },
                values: new object[] { "d5880af9-461f-4008-94f9-13b42c53b109", null, null, "AQAAAAIAAYagAAAAENR5917pBnL6PyZ2XCDzO9iGSkb4DDzeqWl5LxZ+LZeSibPLW1rXUxM4hEBOoaoRHQ==", null });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ConcurrencyStamp", "FirstName", "LastName", "PasswordHash", "ProfilePhotoUrl" },
                values: new object[] { "9ec4f0a7-657c-4575-a08f-38144f8f842f", null, null, "AQAAAAIAAYagAAAAEMk3zoKqMbwUDH9MaepyUR1laRnZPs1fZQjX1MX+/hX5GDvn7GHmj/RsixkKuaiF0Q==", null });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ConcurrencyStamp", "FirstName", "LastName", "PasswordHash", "ProfilePhotoUrl" },
                values: new object[] { "5521121b-76a2-4f55-8694-d39a4145c9f3", null, null, "AQAAAAIAAYagAAAAEGsc0cn5yzW7AlRbXoj9cHvH6NlsD3pJ72UZFOOKTbQt5CUW2gABFXTEVsBj3oGwzA==", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfilePhotoUrl",
                table: "AspNetUsers");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "5d5b5d7b-e208-41d6-a56e-e5e0e5853bde", "AQAAAAIAAYagAAAAEK6A2WgZClodb8mcwJfqlpYDVtdt2kgGyjlppPWTe6Zvrsr//OFg0SSpl8LPLeo3Vg==" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "214b7a44-81ff-4742-8b4e-3d91f6657829", "AQAAAAIAAYagAAAAEOEexhL12tCUhO9PtWGMQo1bLycY8TojKunkQuD9Q8niasjNbI1bYg+PzG73Voiq8g==" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "c0b8a4af-b89b-45b7-8015-23d92533a99c", "AQAAAAIAAYagAAAAEOkrR31LQIa/wC3ZigNvYlNwDMZdIB/dqG6MEP/yZiYNuFEcrfwm02OIvrPf2wNgWw==" });
        }
    }
}
