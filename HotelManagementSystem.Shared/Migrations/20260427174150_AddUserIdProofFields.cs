using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagementSystem.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdProofFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdProofNumber",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdProofType",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ConcurrencyStamp", "IdProofNumber", "IdProofType", "PasswordHash" },
                values: new object[] { "37693ae5-b8d7-4f29-98df-74a94638899e", null, null, "AQAAAAIAAYagAAAAEInYhijSQ+EqAb/xqIy0afIqI/ITm+WAc2TckVQyktOudTsdh4a62lqXqCGwHVAMtA==" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ConcurrencyStamp", "IdProofNumber", "IdProofType", "PasswordHash" },
                values: new object[] { "2c005a43-e9fb-4b50-9084-5e2746e59934", null, null, "AQAAAAIAAYagAAAAEI7Fjc91/INmdM40ULwKtqIQFju3adaTEHP4yJZ/6wr0qWau2h5jmTvkLS0jm9Lx0A==" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ConcurrencyStamp", "IdProofNumber", "IdProofType", "PasswordHash" },
                values: new object[] { "351fda4c-bbb4-4f1c-9eae-a6fc7716507c", null, null, "AQAAAAIAAYagAAAAEI79s9jSfXHqIqU92aueAmlxWy5DzLOBoXf1b9Lis79RDnda6zVU/sO3jiLgbbXVrA==" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdProofNumber",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IdProofType",
                table: "AspNetUsers");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "fdedb39c-6a8a-4571-9d63-c3bf0ce61c7e", "AQAAAAIAAYagAAAAEJOE9nED9G4JNLwk8w0P38R1Aedfk1X1jzxs9rzXntnTdBAegMV95Ga/DF+uMkRcDA==" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "47cd1535-c21d-41fc-8001-c792d50aacb8", "AQAAAAIAAYagAAAAEPfFt7vqDn6KxTAdP+61FLUWNUPqbtmBWo2e1sN3yew0pqjfSehdB7odYIbkqOEM4A==" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "4be86eab-b3cc-4e4b-b8f4-3996fa0f90f0", "AQAAAAIAAYagAAAAEDBruBJKmg9QjIdwpZ2llqH6kNWCsSQc3lPbVETJrlgd76tiascZHUmywOFoNwugyA==" });
        }
    }
}
