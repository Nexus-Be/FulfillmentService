namespace Fulfillment.Application.Interfaces.Excel;

using System;
using System.Collections.Generic;
using Fulfillment.Application.Services.Inventory;

public interface IInventoryExcelExporter
{
    byte[] ExportLedgersToExcel(
        IReadOnlyList<InventoryLedgerResponse> ledgers,
        InventoryLedgerSummaryResponse? summary = null,
        string? warehouseName = null);
}
