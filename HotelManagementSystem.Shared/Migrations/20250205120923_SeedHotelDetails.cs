using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagementSystem.Shared.Migrations
{
    public partial class SeedHotelDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Amenities",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[] { 1, "Air Conditioner", "AC" });

            migrationBuilder.InsertData(
                table: "RoomTypes",
                columns: new[] { "Id", "BasePrice", "Capacity", "Name" },
                values: new object[,]
                {
                    { 1, 1000m, 2, "Classic" },
                    { 2, 2000m, 3, "Club" },
                    { 3, 5000m, 3, "Suite" }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "Password",
                value: "$2a$11$qAP93g9tp3W5cewrIok4n.b6xkkPgHqwhla18k.E3CxTGMfD2QQgK");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "Password",
                value: "$2a$11$DOS2Nb2sfFtS2k9i90fsFOSc1/UJT1ir/gfh8TBJ2UChbZOR/7oHG");

            migrationBuilder.InsertData(
                table: "Rooms",
                columns: new[] { "Id", "IsAvailable", "RoomNumber", "RoomTypeId" },
                values: new object[] { 1, true, "101", 1 });

            migrationBuilder.InsertData(
                table: "Rooms",
                columns: new[] { "Id", "IsAvailable", "RoomNumber", "RoomTypeId" },
                values: new object[] { 2, true, "102", 2 });

            migrationBuilder.InsertData(
                table: "Rooms",
                columns: new[] { "Id", "IsAvailable", "RoomNumber", "RoomTypeId" },
                values: new object[] { 3, true, "103", 3 });

            migrationBuilder.InsertData(
                table: "RoomAmenity",
                columns: new[] { "AmenityId", "RoomId" },
                values: new object[] { 1, 1 });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RoomAmenity",
                keyColumns: new[] { "AmenityId", "RoomId" },
                keyValues: new object[] { 1, 1 });

            migrationBuilder.DeleteData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Amenities",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "RoomTypes",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "RoomTypes",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "RoomTypes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "Password",
                value: "$2a$11$66n68GORO6pAm7qzQZw4POArqu41exDoY.wlb8QVqYjaX3Ho1lQVm");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "Password",
                value: "$2a$11$XQFpVGwbeSinfJ1NQRPGAeplfjjbCHfwFKJ9HxJy5yiSdqIj9V5BG");
        }
    }
}
