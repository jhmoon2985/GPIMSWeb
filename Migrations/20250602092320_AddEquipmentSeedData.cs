using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GPIMSWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Equipment",
                columns: new[] { "Id", "IsOnline", "LastUpdateTime", "Name", "Status", "Version" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1277), "GPIMS-001", 0, "v2.1.0" },
                    { 2, true, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1282), "GPIMS-002", 0, "v2.1.0" },
                    { 3, false, new DateTime(2025, 6, 2, 17, 53, 19, 705, DateTimeKind.Local).AddTicks(1287), "GPIMS-003", 2, "v2.0.5" },
                    { 4, true, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1325), "GPIMS-004", 1, "v2.1.0" }
                });

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1056));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "UpdatedAt",
                value: new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1064));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 3,
                column: "UpdatedAt",
                value: new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1067));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 4,
                column: "UpdatedAt",
                value: new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1072));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 6, 2, 18, 23, 19, 704, DateTimeKind.Local).AddTicks(9549), "$2a$11$QlHZHcrPptsR.pOBzlmB7OSt.bPzAPqBX5RSVcAd9AzMECaZAwLgu" });

            migrationBuilder.InsertData(
                table: "Channels",
                columns: new[] { "Id", "Capacity", "ChannelNumber", "Current", "Energy", "EquipmentId", "LastUpdateTime", "Mode", "Power", "ScheduleName", "Status", "Voltage" },
                values: new object[,]
                {
                    { 1, 55.0, 1, 1.7, 28.5, 1, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1510), 2, 6.46, "Schedule_1", 1, 3.8000000000000003 },
                    { 2, 60.0, 2, 1.8999999999999999, 31.5, 1, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1544), 1, 7.4100000000000001, "Schedule_2", 1, 3.9000000000000004 },
                    { 3, 65.0, 3, 2.1000000000000001, 34.5, 1, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1567), 2, 8.4000000000000004, "Schedule_3", 1, 4.0 },
                    { 4, 70.0, 4, 2.2999999999999998, 37.5, 1, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1572), 1, 9.4299999999999997, "Schedule_4", 1, 4.1000000000000005 },
                    { 5, 75.0, 5, 0.0, 0.0, 1, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1575), 2, 0.0, "", 0, 4.2000000000000002 },
                    { 6, 80.0, 6, 0.0, 0.0, 1, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1595), 1, 0.0, "", 0, 4.3000000000000007 },
                    { 7, 85.0, 7, 0.0, 0.0, 1, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1598), 2, 0.0, "", 0, 4.4000000000000004 },
                    { 8, 90.0, 8, 0.0, 0.0, 1, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1613), 1, 0.0, "", 0, 4.5 },
                    { 9, 55.0, 1, 1.7, 28.5, 2, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1620), 2, 6.46, "Schedule_1", 1, 3.8000000000000003 },
                    { 10, 60.0, 2, 1.8999999999999999, 31.5, 2, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1650), 1, 7.4100000000000001, "Schedule_2", 1, 3.9000000000000004 },
                    { 11, 65.0, 3, 2.1000000000000001, 34.5, 2, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1656), 2, 8.4000000000000004, "Schedule_3", 1, 4.0 },
                    { 12, 70.0, 4, 2.2999999999999998, 37.5, 2, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1660), 1, 9.4299999999999997, "Schedule_4", 1, 4.1000000000000005 },
                    { 13, 75.0, 5, 0.0, 0.0, 2, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1663), 2, 0.0, "", 0, 4.2000000000000002 },
                    { 14, 80.0, 6, 0.0, 0.0, 2, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1665), 1, 0.0, "", 0, 4.3000000000000007 },
                    { 15, 85.0, 7, 0.0, 0.0, 2, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1667), 2, 0.0, "", 0, 4.4000000000000004 },
                    { 16, 90.0, 8, 0.0, 0.0, 2, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1670), 1, 0.0, "", 0, 4.5 },
                    { 17, 55.0, 1, 1.7, 28.5, 3, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1675), 2, 6.46, "Schedule_1", 1, 3.8000000000000003 },
                    { 18, 60.0, 2, 1.8999999999999999, 31.5, 3, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1689), 1, 7.4100000000000001, "Schedule_2", 1, 3.9000000000000004 },
                    { 19, 65.0, 3, 2.1000000000000001, 34.5, 3, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1694), 2, 8.4000000000000004, "Schedule_3", 1, 4.0 },
                    { 20, 70.0, 4, 2.2999999999999998, 37.5, 3, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1698), 1, 9.4299999999999997, "Schedule_4", 1, 4.1000000000000005 },
                    { 21, 75.0, 5, 0.0, 0.0, 3, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1712), 2, 0.0, "", 0, 4.2000000000000002 },
                    { 22, 80.0, 6, 0.0, 0.0, 3, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1742), 1, 0.0, "", 0, 4.3000000000000007 },
                    { 23, 85.0, 7, 0.0, 0.0, 3, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1745), 2, 0.0, "", 0, 4.4000000000000004 },
                    { 24, 90.0, 8, 0.0, 0.0, 3, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1748), 1, 0.0, "", 0, 4.5 },
                    { 25, 55.0, 1, 1.7, 28.5, 4, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1762), 2, 6.46, "Schedule_1", 1, 3.8000000000000003 },
                    { 26, 60.0, 2, 1.8999999999999999, 31.5, 4, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1799), 1, 7.4100000000000001, "Schedule_2", 1, 3.9000000000000004 },
                    { 27, 65.0, 3, 2.1000000000000001, 34.5, 4, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1823), 2, 8.4000000000000004, "Schedule_3", 1, 4.0 },
                    { 28, 70.0, 4, 2.2999999999999998, 37.5, 4, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1828), 1, 9.4299999999999997, "Schedule_4", 1, 4.1000000000000005 },
                    { 29, 75.0, 5, 0.0, 0.0, 4, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1831), 2, 0.0, "", 0, 4.2000000000000002 },
                    { 30, 80.0, 6, 0.0, 0.0, 4, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1833), 1, 0.0, "", 0, 4.3000000000000007 },
                    { 31, 85.0, 7, 0.0, 0.0, 4, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1835), 2, 0.0, "", 0, 4.4000000000000004 },
                    { 32, 90.0, 8, 0.0, 0.0, 4, new DateTime(2025, 6, 2, 18, 23, 19, 705, DateTimeKind.Local).AddTicks(1837), 1, 0.0, "", 0, 4.5 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "Channels",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "Equipment",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Equipment",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Equipment",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Equipment",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2025, 6, 2, 17, 38, 24, 269, DateTimeKind.Local).AddTicks(6817));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "UpdatedAt",
                value: new DateTime(2025, 6, 2, 17, 38, 24, 269, DateTimeKind.Local).AddTicks(6824));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 3,
                column: "UpdatedAt",
                value: new DateTime(2025, 6, 2, 17, 38, 24, 269, DateTimeKind.Local).AddTicks(6827));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 4,
                column: "UpdatedAt",
                value: new DateTime(2025, 6, 2, 17, 38, 24, 269, DateTimeKind.Local).AddTicks(6829));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 6, 2, 17, 38, 24, 269, DateTimeKind.Local).AddTicks(5431), "$2a$11$V1X9YmA8z.yjaU.WFX7N6.U3AoDDvQfcWv88W1oIsrevlUkGBuuaa" });
        }
    }
}
