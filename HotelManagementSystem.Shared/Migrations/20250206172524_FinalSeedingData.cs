using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagementSystem.Shared.Migrations
{
    public partial class FinalSeedingData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "Password",
                value: "$2a$11$TvZOnzqzS11buDP9Va/5JejwvuISK5kh2wXfEtB8ePsQ3h0aa2n2u");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "Password",
                value: "$2a$11$nOYIgNYDIb6MYHplYX7dvO3QFIARAL1VoqzhkT/ZFBDB0B3wSzC3G");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
