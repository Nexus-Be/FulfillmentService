using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TraceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UserName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    EntityName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    OldValues = table.Column<string>(type: "jsonb", nullable: true),
                    NewValues = table.Column<string>(type: "jsonb", nullable: true),
                    ChangedColumns = table.Column<string>(type: "jsonb", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsSynced = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Carriers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carriers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceType = table.Column<int>(type: "integer", nullable: false),
                    ReferenceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReleasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryReservations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Address = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalQuantity = table.Column<int>(type: "integer", nullable: false),
                    ReservedQuantity = table.Column<int>(type: "integer", nullable: false),
                    UnavailableQuantity = table.Column<int>(type: "integer", nullable: false),
                    AvailableQuantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItems_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Shipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CarrierId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceType = table.Column<int>(type: "integer", nullable: false),
                    ShippingFee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TrackingNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReceiverName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ReceiverPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReceiverAddress = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    EstimatedDeliveryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shipments_Carriers_CarrierId",
                        column: x => x.CarrierId,
                        principalTable: "Carriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Shipments_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryLedgers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    MovementType = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceType = table.Column<int>(type: "integer", nullable: true),
                    ReferenceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryLedgers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryLedgers_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryLedgers_InventoryReservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "InventoryReservations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InventoryReservationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryReservationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryReservationItems_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryReservationItems_InventoryReservations_Reservation~",
                        column: x => x.ReservationId,
                        principalTable: "InventoryReservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CarrierStatus = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ExternalEventId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RawPayload = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentEvents_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentItems_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AuditLogs",
                columns: new[] { "Id", "Action", "ChangedColumns", "CreatedAt", "EntityId", "EntityName", "IpAddress", "IsSynced", "NewValues", "OldValues", "ServiceName", "TraceId", "UserAgent", "UserId", "UserName" },
                values: new object[] { new Guid("77300000-0000-0000-0000-000000000001"), 9, "[\"Status\"]", new DateTimeOffset(new DateTime(2026, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "77000000-0000-0000-0000-000000000001", "Shipment", "127.0.0.1", true, "{\"Status\":\"Delivered\"}", "{\"Status\":\"OutForDelivery\"}", "Fulfillment", "trace-fulfillment-shipment-001", "GHNWebhookService/1.0", "aaaaaaaa-1111-1111-1111-111111111111", "admin@nexus.com" });

            migrationBuilder.InsertData(
                table: "Carriers",
                columns: new[] { "Id", "Code", "CreatedAt", "DeletedAt", "IsDeleted", "IsDisabled", "Name", "Priority", "UpdatedAt" },
                values: new object[] { new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"), "GHN", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, false, false, "Giao Hàng Nhanh", 1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                table: "InventoryReservations",
                columns: new[] { "Id", "ConfirmedAt", "CreatedAt", "DeletedAt", "ExpiresAt", "IsDeleted", "ReferenceId", "ReferenceType", "ReleasedAt", "Status", "UpdatedAt" },
                values: new object[] { new Guid("e2000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new DateTimeOffset(new DateTime(2026, 1, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), false, "aa000000-0000-0000-0000-000000000001", 1, null, 0, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                table: "Warehouses",
                columns: new[] { "Id", "Address", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "IsDeleted", "IsDisabled", "Name", "SellerId", "Type", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), "Hà Nội, Việt Nam", "WH-CENTRAL-01", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000000"), null, false, false, "Kho Trung Tâm Sàn Nexus", null, 1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000000") });

            migrationBuilder.InsertData(
                table: "InventoryItems",
                columns: new[] { "Id", "AvailableQuantity", "CreatedAt", "DeletedAt", "IsDeleted", "ReservedQuantity", "SkuId", "TotalQuantity", "UnavailableQuantity", "UpdatedAt", "WarehouseId" },
                values: new object[,]
                {
                    { new Guid("e1000000-0000-0000-0000-000000000001"), 100, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, false, 0, new Guid("11100000-0000-0000-0000-000000000001"), 100, 0, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") },
                    { new Guid("e1000000-0000-0000-0000-000000000002"), 200, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, false, 0, new Guid("22100000-0000-0000-0000-000000000002"), 200, 0, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") },
                    { new Guid("e1000000-0000-0000-0000-000000000003"), 150, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, false, 0, new Guid("33100000-0000-0000-0000-000000000003"), 150, 0, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") },
                    { new Guid("e1000000-0000-0000-0000-000000000004"), 9, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, false, 1, new Guid("44100000-0000-0000-0000-000000000004"), 10, 0, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") },
                    { new Guid("e1000000-0000-0000-0000-000000000005"), 500, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, false, 0, new Guid("55100000-0000-0000-0000-000000000005"), 500, 0, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") }
                });

            migrationBuilder.InsertData(
                table: "Shipments",
                columns: new[] { "Id", "CarrierId", "CreatedAt", "CreatedBy", "DeletedAt", "DeliveredAt", "EstimatedDeliveryAt", "IsDeleted", "OrderId", "ReceiverAddress", "ReceiverName", "ReceiverPhone", "ServiceType", "ShippingFee", "Status", "TrackingNumber", "UpdatedAt", "UpdatedBy", "WarehouseId" },
                values: new object[] { new Guid("77000000-0000-0000-0000-000000000001"), new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000000"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, false, new Guid("02000000-0000-0000-0000-000000000001"), "123 Nguyen Trai, Thanh Xuan, Ha Noi", "John Buyer", "0900000004", 0, 0m, 5, "GHN-NEXUS-20260101-0001", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000000"), new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") });

            migrationBuilder.InsertData(
                table: "InventoryLedgers",
                columns: new[] { "Id", "CreatedAt", "DeletedAt", "InventoryItemId", "IsDeleted", "MovementType", "Note", "Quantity", "ReferenceId", "ReferenceType", "ReservationId", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("e4000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("e1000000-0000-0000-0000-000000000004"), false, 0, "Khởi tạo tồn kho ban đầu cho Mũ Bảo Hiểm", 10, "STOCKIN-20260101-001", 3, null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("e4000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("e1000000-0000-0000-0000-000000000004"), false, 2, "Khóa tạm tồn kho phục vụ đấu giá", 1, "aa000000-0000-0000-0000-000000000001", 1, new Guid("e2000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "InventoryReservationItems",
                columns: new[] { "Id", "CreatedAt", "DeletedAt", "InventoryItemId", "IsDeleted", "Quantity", "ReservationId", "UpdatedAt" },
                values: new object[] { new Guid("e3000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("e1000000-0000-0000-0000-000000000004"), false, 1, new Guid("e2000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                table: "ShipmentEvents",
                columns: new[] { "Id", "CarrierStatus", "CreatedAt", "DeletedAt", "Description", "ExternalEventId", "IsDeleted", "OccurredAt", "RawPayload", "ShipmentId", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("77200000-0000-0000-0000-000000000001"), "PICKED_UP", new DateTimeOffset(new DateTime(2026, 1, 1, 2, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Bưu tá GHN đã lấy hàng tại kho trung tâm", "GHN-EVT-001", false, new DateTimeOffset(new DateTime(2026, 1, 1, 2, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "{\"carrier\":\"GHN\",\"status\":\"PICKED_UP\"}", new Guid("77000000-0000-0000-0000-000000000001"), 1, new DateTimeOffset(new DateTime(2026, 1, 1, 2, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("77200000-0000-0000-0000-000000000002"), "IN_TRANSIT", new DateTimeOffset(new DateTime(2026, 1, 1, 6, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Đang trung chuyển qua bưu cục Hà Nội", "GHN-EVT-002", false, new DateTimeOffset(new DateTime(2026, 1, 1, 6, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "{\"carrier\":\"GHN\",\"status\":\"IN_TRANSIT\"}", new Guid("77000000-0000-0000-0000-000000000001"), 2, new DateTimeOffset(new DateTime(2026, 1, 1, 6, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("77200000-0000-0000-0000-000000000003"), "DELIVERED", new DateTimeOffset(new DateTime(2026, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Giao hàng thành công tới người nhận", "GHN-EVT-003", false, new DateTimeOffset(new DateTime(2026, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "{\"carrier\":\"GHN\",\"status\":\"DELIVERED\"}", new Guid("77000000-0000-0000-0000-000000000001"), 4, new DateTimeOffset(new DateTime(2026, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "ShipmentItems",
                columns: new[] { "Id", "CreatedAt", "DeletedAt", "IsDeleted", "Quantity", "ShipmentId", "SkuId", "UpdatedAt" },
                values: new object[] { new Guid("77100000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, false, 1, new Guid("77000000-0000-0000-0000-000000000001"), new Guid("33100000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_Carriers_Code",
                table: "Carriers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_SkuId_WarehouseId",
                table: "InventoryItems",
                columns: new[] { "SkuId", "WarehouseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_WarehouseId",
                table: "InventoryItems",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLedgers_InventoryItemId",
                table: "InventoryLedgers",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLedgers_ReservationId",
                table: "InventoryLedgers",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservationItems_InventoryItemId",
                table: "InventoryReservationItems",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservationItems_ReservationId",
                table: "InventoryReservationItems",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentEvents_ShipmentId",
                table: "ShipmentEvents",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentItems_ShipmentId",
                table: "ShipmentItems",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_CarrierId",
                table: "Shipments",
                column: "CarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_WarehouseId",
                table: "Shipments",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_Code",
                table: "Warehouses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_SellerId",
                table: "Warehouses",
                column: "SellerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "InventoryLedgers");

            migrationBuilder.DropTable(
                name: "InventoryReservationItems");

            migrationBuilder.DropTable(
                name: "ShipmentEvents");

            migrationBuilder.DropTable(
                name: "ShipmentItems");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "InventoryReservations");

            migrationBuilder.DropTable(
                name: "Shipments");

            migrationBuilder.DropTable(
                name: "Carriers");

            migrationBuilder.DropTable(
                name: "Warehouses");
        }
    }
}
