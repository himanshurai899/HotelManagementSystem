using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagementSystem.Shared.Migrations
{
    /// <inheritdoc />
    public partial class Phase12c_HourlyStay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DailyRate",
                table: "Rooms",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRate",
                table: "Rooms",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyRate",
                table: "Rooms",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "YearlyRate",
                table: "Rooms",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BookingType",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "CheckInTime",
                table: "Bookings",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "CheckOutTime",
                table: "Bookings",
                type: "time",
                nullable: true);

            // Phase 12c — Backfill: existing bookings that span more than 1 night were created
            // before BookingType existed and behave as NightStay (value = 1). FullDay (value = 0)
            // is exactly 1 night, so single-night rows stay as 0 (FullDay default).
            migrationBuilder.Sql(
                "UPDATE Bookings SET BookingType = 1 " +
                "WHERE DATEDIFF(day, CheckInDate, CheckOutDate) > 1;");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyRate",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "MonthlyRate",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "YearlyRate",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "BookingType",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CheckInTime",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CheckOutTime",
                table: "Bookings");

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
        }
    }
}
