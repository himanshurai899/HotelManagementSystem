using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagementSystem.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyLocaleAndRoomPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Locale",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerNight",
                table: "Rooms",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "2d12eced-4667-4eba-b331-30e62a06b603", "AQAAAAIAAYagAAAAEIhp3CB0+h53lg8FlcfLbhPogEwsjeEoLzu6WmDtPGYFGVmGFVnx1bZwglba+dUYQg==" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "b247982a-9750-4231-aa61-1fc947ea004f", "AQAAAAIAAYagAAAAECtuPO9kk+e5QNnt/3R9e3guzJgAO4ZvbZgEDO8P4kB+rJN/gwLHF2BnDT9Va3DBhw==" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "b317d45e-e06c-49ef-bf7e-09279c8bb704", "AQAAAAIAAYagAAAAENMt1n5erBKT0/lbyP7Issg4lhLqwaMCJchc95gkuK8NLW24z3veHqr8JxN4yk1lvg==" });

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CurrencyCode", "Locale" },
                values: new object[] { "INR", "en-IN" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Locale",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PricePerNight",
                table: "Rooms");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "1ab12782-48f8-481f-9d81-490f0d86163f", "AQAAAAIAAYagAAAAEG2LdYcN5GtoWcYdHlc50vg9lQLJ2mARhZ/2Qmc6yq4gws4nFtYvVHcg5cBiUmb+7w==" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "5db747ca-414d-4818-bbbc-96092135d5c4", "AQAAAAIAAYagAAAAEACbothiQ44wZIYQocMoOOd1JyqEy+NzFh2ii4Nzr/jw5vCVFkcG/dcOoYu3/B22Hw==" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "80ed58fe-078c-4852-95b1-0192a9b57c1a", "AQAAAAIAAYagAAAAEDIOmvWqkDPs9HTUJGLhzMR8gZfFn2KfNYRTJLGkWWOejXZWmTW/nEjViYVTt+8qWA==" });
        }
    }
}
