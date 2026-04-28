using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagementSystem.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowHourlyStayToRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowHourlyStay",
                table: "Rooms",
                type: "bit",
                nullable: false,
                defaultValue: false);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowHourlyStay",
                table: "Rooms");

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
        }
    }
}
