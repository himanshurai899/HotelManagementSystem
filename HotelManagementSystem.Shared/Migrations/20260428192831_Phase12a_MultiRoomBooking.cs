using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagementSystem.Shared.Migrations
{
    /// <inheritdoc />
    public partial class Phase12a_MultiRoomBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 12a — Multi-Room Booking is a destructive schema change.
            // The single Bookings.RoomId column is being replaced by the BookingRooms join table.
            // Per project decision (Gate 1), all existing booking-related rows are dropped
            // before the column drop so the schema migration succeeds without backfill.
            // Order matters: child rows first, then parents.
            migrationBuilder.Sql("DELETE FROM Payments;");
            migrationBuilder.Sql("DELETE FROM InvoiceItems;");
            migrationBuilder.Sql("DELETE FROM Invoices;");
            migrationBuilder.Sql("DELETE FROM Bookings;");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Rooms_RoomId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_RoomId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "Bookings");

            migrationBuilder.CreateTable(
                name: "BookingRooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    PriceAtBooking = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingRooms_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingRooms_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "5f97aa72-254a-4c90-8db6-ca69381a0c87", "AQAAAAIAAYagAAAAEMzKfj/AvhsHYNLgZ+xtxRfDevk5FAbQ9VrfPAbALqdVXbSYD8/c1jC9j+8s+yBwxQ==" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "cd82df7c-ea9b-4129-b669-803b75d42855", "AQAAAAIAAYagAAAAEP+ccE1FcLqz5RasVw4nzR05ykua4zNmQ+Q2oMqnSXDlNnVCdDL3p4kiqQjGKhoFPw==" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "53f662ab-2516-4fd6-858a-f424eaea3e8b", "AQAAAAIAAYagAAAAENIPafAeh0AeSlsbjzAA5Fdv2WholRBOdaGCgettPLrWA34yKPJ4s0uoTrYZPQxWAA==" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingRooms_BookingId_RoomId",
                table: "BookingRooms",
                columns: new[] { "BookingId", "RoomId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingRooms_RoomId",
                table: "BookingRooms",
                column: "RoomId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingRooms");

            migrationBuilder.AddColumn<int>(
                name: "RoomId",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_RoomId",
                table: "Bookings",
                column: "RoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Rooms_RoomId",
                table: "Bookings",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
