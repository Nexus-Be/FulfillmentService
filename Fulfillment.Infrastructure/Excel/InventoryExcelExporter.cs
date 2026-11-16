namespace Fulfillment.Infrastructure.Excel;

using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using Fulfillment.Application.Interfaces.Excel;
using Fulfillment.Application.Services.Inventory;

public class InventoryExcelExporter : IInventoryExcelExporter
{
    public byte[] ExportLedgersToExcel(
        IReadOnlyList<InventoryLedgerResponse> ledgers,
        InventoryLedgerSummaryResponse? summary = null,
        string? warehouseName = null)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sổ Cái Kho");

        // Title Block
        worksheet.Cell(1, 1).Value = "BÁO CÁO SỔ CÁI BIẾN ĐỘNG TỒN KHO";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromArgb(15, 23, 42); // slate-900

        var infoText = $"Kho hàng: {warehouseName ?? "Tất cả kho"} | Thời điểm xuất: {DateTimeOffset.UtcNow:dd/MM/yyyy HH:mm:ss} UTC";
        worksheet.Cell(2, 1).Value = infoText;
        worksheet.Cell(2, 1).Style.Font.Italic = true;
        worksheet.Cell(2, 1).Style.Font.FontSize = 10;
        worksheet.Cell(2, 1).Style.Font.FontColor = XLColor.FromArgb(100, 116, 139); // slate-500

        // Summary Cards if provided
        int currentRow = 4;
        if (summary != null)
        {
            worksheet.Cell(currentRow, 1).Value = "TỔNG NHẬP (+):";
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 2).Value = summary.TotalInboundQuantity;
            worksheet.Cell(currentRow, 2).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 2).Style.Font.FontColor = XLColor.FromArgb(16, 185, 129); // emerald-500

            worksheet.Cell(currentRow, 3).Value = "TỔNG XUẤT (-):";
            worksheet.Cell(currentRow, 3).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 4).Value = summary.TotalOutboundQuantity;
            worksheet.Cell(currentRow, 4).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 4).Style.Font.FontColor = XLColor.FromArgb(244, 63, 94); // rose-500

            worksheet.Cell(currentRow, 5).Value = "CHÊNH LỆCH RÒNG:";
            worksheet.Cell(currentRow, 5).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 6).Value = summary.NetChange;
            worksheet.Cell(currentRow, 6).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 6).Style.Font.FontColor = summary.NetChange >= 0
                ? XLColor.FromArgb(16, 185, 129)
                : XLColor.FromArgb(244, 63, 94);

            worksheet.Cell(currentRow, 7).Value = "TỔNG SỐ BÚT TOÁN:";
            worksheet.Cell(currentRow, 7).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 8).Value = summary.TotalTransactions;
            worksheet.Cell(currentRow, 8).Style.Font.Bold = true;

            currentRow += 2;
        }

        // Table Headers
        var headers = new[]
        {
            "STT",
            "Thời Gian (UTC)",
            "Mã Bút Toán (ID)",
            "Mã SKU",
            "Kho Hàng",
            "Loại Biến Động",
            "Số Lượng",
            "Loại Chứng Từ",
            "Mã Tham Chiếu",
            "Ghi Chú"
        };

        for (int c = 0; c < headers.Length; c++)
        {
            var cell = worksheet.Cell(currentRow, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(30, 41, 59); // slate-800
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
        worksheet.Row(currentRow).Height = 24;

        // Table Rows
        int startDataRow = currentRow + 1;
        for (int i = 0; i < ledgers.Count; i++)
        {
            var l = ledgers[i];
            int r = startDataRow + i;

            worksheet.Cell(r, 1).Value = i + 1;
            worksheet.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(r, 2).Value = l.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            worksheet.Cell(r, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(r, 3).Value = l.Id.ToString();
            worksheet.Cell(r, 4).Value = l.SkuId.ToString();
            worksheet.Cell(r, 5).Value = l.WarehouseName ?? warehouseName ?? "Kho Mặc Định";

            worksheet.Cell(r, 6).Value = l.MovementType;
            worksheet.Cell(r, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(r, 6).Style.Font.Bold = true;

            // Quantity with color
            var qtyCell = worksheet.Cell(r, 7);
            qtyCell.Value = l.Quantity;
            qtyCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            qtyCell.Style.Font.Bold = true;
            if (l.Quantity > 0)
            {
                qtyCell.Style.Font.FontColor = XLColor.FromArgb(5, 150, 105); // emerald-600
                qtyCell.Value = $"+{l.Quantity}";
            }
            else if (l.Quantity < 0)
            {
                qtyCell.Style.Font.FontColor = XLColor.FromArgb(225, 29, 72); // rose-600
            }

            worksheet.Cell(r, 8).Value = l.ReferenceType ?? "MANUAL";
            worksheet.Cell(r, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(r, 9).Value = l.ReferenceId ?? "-";
            worksheet.Cell(r, 10).Value = l.Note ?? "-";

            // Light zebra striping
            if (i % 2 == 1)
            {
                worksheet.Row(r).Style.Fill.BackgroundColor = XLColor.FromArgb(248, 250, 252); // slate-50
            }
        }

        // Apply Borders
        var dataRange = worksheet.Range(currentRow, 1, Math.Max(currentRow, startDataRow + ledgers.Count - 1), headers.Length);
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorderColor = XLColor.FromArgb(226, 232, 240);
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        dataRange.Style.Border.OutsideBorderColor = XLColor.FromArgb(148, 163, 184);

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
