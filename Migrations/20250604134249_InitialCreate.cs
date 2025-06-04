using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GPIMSWeb.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChamberChillerData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Value = table.Column<double>(type: "float", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChamberChillerData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsOnline = table.Column<bool>(type: "bit", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Alarms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsCleared = table.Column<bool>(type: "bit", nullable: false),
                    ClearedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClearedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alarms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alarms_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuxData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    SensorId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<double>(type: "float", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuxData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuxData_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CanLinData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MinValue = table.Column<double>(type: "float", nullable: false),
                    MaxValue = table.Column<double>(type: "float", nullable: false),
                    CurrentValue = table.Column<double>(type: "float", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanLinData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CanLinData_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Channels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    ChannelNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    Voltage = table.Column<double>(type: "float", nullable: false),
                    Current = table.Column<double>(type: "float", nullable: false),
                    Capacity = table.Column<double>(type: "float", nullable: false),
                    Power = table.Column<double>(type: "float", nullable: false),
                    Energy = table.Column<double>(type: "float", nullable: false),
                    ScheduleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Channels_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Equipment",
                columns: new[] { "Id", "IsOnline", "LastUpdateTime", "Name", "Status", "Version" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1242), "GPIMS-001", 0, "v2.1.0" },
                    { 2, true, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1247), "GPIMS-002", 0, "v2.1.0" },
                    { 3, false, new DateTime(2025, 6, 4, 22, 12, 49, 747, DateTimeKind.Local).AddTicks(1249), "GPIMS-003", 2, "v2.0.5" },
                    { 4, true, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1278), "GPIMS-004", 1, "v2.1.0" }
                });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "Description", "Key", "UpdatedAt", "UpdatedBy", "Value" },
                values: new object[,]
                {
                    { 1, "Number of equipment", "EquipmentCount", new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1179), "System", "4" },
                    { 2, "Channels per equipment", "ChannelsPerEquipment", new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1181), "System", "8" },
                    { 3, "Default system language", "DefaultLanguage", new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1182), "System", "en" },
                    { 4, "Date format", "DateFormat", new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1184), "System", "yyyy-MM-dd HH:mm:ss" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Department", "IsActive", "LastLoginAt", "Level", "Name", "PasswordHash", "Username" },
                values: new object[] { 1, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(561), "IT", true, null, 3, "System Administrator", "$2a$11$khohGPLmyoeWR19lbKQw3.9z6XNzsaVUR1J14ck/.4foKCq0NgixW", "admin" });

            migrationBuilder.InsertData(
                table: "Channels",
                columns: new[] { "Id", "Capacity", "ChannelNumber", "Current", "Energy", "EquipmentId", "LastUpdateTime", "Mode", "Power", "ScheduleName", "Status", "Voltage" },
                values: new object[,]
                {
                    { 1, 55.0, 1, 1.7, 28.5, 1, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1332), 2, 6.46, "Schedule_1", 1, 3.8000000000000003 },
                    { 2, 60.0, 2, 1.8999999999999999, 31.5, 1, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1336), 1, 7.4100000000000001, "Schedule_2", 1, 3.9000000000000004 },
                    { 3, 65.0, 3, 2.1000000000000001, 34.5, 1, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1338), 2, 8.4000000000000004, "Schedule_3", 1, 4.0 },
                    { 4, 70.0, 4, 2.2999999999999998, 37.5, 1, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1340), 1, 9.4299999999999997, "Schedule_4", 1, 4.1000000000000005 },
                    { 5, 75.0, 5, 0.0, 0.0, 1, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1342), 2, 0.0, "", 0, 4.2000000000000002 },
                    { 6, 80.0, 6, 0.0, 0.0, 1, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1360), 1, 0.0, "", 0, 4.3000000000000007 },
                    { 7, 85.0, 7, 0.0, 0.0, 1, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1361), 2, 0.0, "", 0, 4.4000000000000004 },
                    { 8, 90.0, 8, 0.0, 0.0, 1, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1390), 1, 0.0, "", 0, 4.5 },
                    { 9, 55.0, 1, 1.7, 28.5, 2, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1392), 2, 6.46, "Schedule_1", 1, 3.8000000000000003 },
                    { 10, 60.0, 2, 1.8999999999999999, 31.5, 2, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1400), 1, 7.4100000000000001, "Schedule_2", 1, 3.9000000000000004 },
                    { 11, 65.0, 3, 2.1000000000000001, 34.5, 2, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1402), 2, 8.4000000000000004, "Schedule_3", 1, 4.0 },
                    { 12, 70.0, 4, 2.2999999999999998, 37.5, 2, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1404), 1, 9.4299999999999997, "Schedule_4", 1, 4.1000000000000005 },
                    { 13, 75.0, 5, 0.0, 0.0, 2, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1405), 2, 0.0, "", 0, 4.2000000000000002 },
                    { 14, 80.0, 6, 0.0, 0.0, 2, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1406), 1, 0.0, "", 0, 4.3000000000000007 },
                    { 15, 85.0, 7, 0.0, 0.0, 2, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1407), 2, 0.0, "", 0, 4.4000000000000004 },
                    { 16, 90.0, 8, 0.0, 0.0, 2, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1408), 1, 0.0, "", 0, 4.5 },
                    { 17, 55.0, 1, 1.7, 28.5, 3, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1410), 2, 6.46, "Schedule_1", 1, 3.8000000000000003 },
                    { 18, 60.0, 2, 1.8999999999999999, 31.5, 3, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1413), 1, 7.4100000000000001, "Schedule_2", 1, 3.9000000000000004 },
                    { 19, 65.0, 3, 2.1000000000000001, 34.5, 3, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1414), 2, 8.4000000000000004, "Schedule_3", 1, 4.0 },
                    { 20, 70.0, 4, 2.2999999999999998, 37.5, 3, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1416), 1, 9.4299999999999997, "Schedule_4", 1, 4.1000000000000005 },
                    { 21, 75.0, 5, 0.0, 0.0, 3, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1417), 2, 0.0, "", 0, 4.2000000000000002 },
                    { 22, 80.0, 6, 0.0, 0.0, 3, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1418), 1, 0.0, "", 0, 4.3000000000000007 },
                    { 23, 85.0, 7, 0.0, 0.0, 3, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1419), 2, 0.0, "", 0, 4.4000000000000004 },
                    { 24, 90.0, 8, 0.0, 0.0, 3, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1420), 1, 0.0, "", 0, 4.5 },
                    { 25, 55.0, 1, 1.7, 28.5, 4, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1430), 2, 6.46, "Schedule_1", 1, 3.8000000000000003 },
                    { 26, 60.0, 2, 1.8999999999999999, 31.5, 4, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1441), 1, 7.4100000000000001, "Schedule_2", 1, 3.9000000000000004 },
                    { 27, 65.0, 3, 2.1000000000000001, 34.5, 4, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1444), 2, 8.4000000000000004, "Schedule_3", 1, 4.0 },
                    { 28, 70.0, 4, 2.2999999999999998, 37.5, 4, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1445), 1, 9.4299999999999997, "Schedule_4", 1, 4.1000000000000005 },
                    { 29, 75.0, 5, 0.0, 0.0, 4, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1446), 2, 0.0, "", 0, 4.2000000000000002 },
                    { 30, 80.0, 6, 0.0, 0.0, 4, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1447), 1, 0.0, "", 0, 4.3000000000000007 },
                    { 31, 85.0, 7, 0.0, 0.0, 4, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1448), 2, 0.0, "", 0, 4.4000000000000004 },
                    { 32, 90.0, 8, 0.0, 0.0, 4, new DateTime(2025, 6, 4, 22, 42, 49, 747, DateTimeKind.Local).AddTicks(1449), 1, 0.0, "", 0, 4.5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_EquipmentId",
                table: "Alarms",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AuxData_EquipmentId",
                table: "AuxData",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CanLinData_EquipmentId",
                table: "CanLinData",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Channels_EquipmentId_ChannelNumber",
                table: "Channels",
                columns: new[] { "EquipmentId", "ChannelNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserHistories_UserId",
                table: "UserHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alarms");

            migrationBuilder.DropTable(
                name: "AuxData");

            migrationBuilder.DropTable(
                name: "CanLinData");

            migrationBuilder.DropTable(
                name: "ChamberChillerData");

            migrationBuilder.DropTable(
                name: "Channels");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "UserHistories");

            migrationBuilder.DropTable(
                name: "Equipment");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
