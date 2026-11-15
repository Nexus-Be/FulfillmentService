namespace Fulfillment.Application.Services.Inventory;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fulfillment.Application.Exceptions;
using Fulfillment.Domain.Exceptions;

public sealed partial class InventoryAppService
{
    public async Task<InventoryImportPreviewResponse> PreviewImportAsync(
        Stream fileStream,
        string fileName,
        Guid? warehouseId,
        Guid currentUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        if (_excelParser == null)
        {
            throw new InvalidOperationException("Excel parser service is not configured.");
        }

        var effectiveWarehouseId = await ResolveEffectiveWarehouseIdAsync(warehouseId, currentUserId, isAdmin, cancellationToken);

        // 1. Trích xuất 10 dòng đầu tiên có dữ liệu
        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        var sampleRows = await _excelParser.ExtractSampleRowsAsync(fileStream, fileName, maxRows: 10, cancellationToken);

        // 2. Dùng AI Schema Inference (hoặc fallback heuristic) để xác định tọa độ cột
        var schema = _aiSchemaInferenceService != null
            ? await _aiSchemaInferenceService.InferSchemaAsync(sampleRows, cancellationToken)
            : new Fulfillment.Application.Interfaces.AI.TableSchemaInferenceResult(
                HeaderRowIndex: 0,
                SkuColIndex: 0,
                QuantityColIndex: 1,
                OperationColIndex: -1,
                NoteColIndex: -1,
                Mode: "SINGLE_QTY_COL",
                IntakeKeywords: new List<string> { "nhập", "+" },
                AdjustKeywords: new List<string> { "xuất", "-" });

        // 3. Đọc lại từ đầu file để C# duyệt deterministic toàn bộ dòng dữ liệu
        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        var parsedRows = await _excelParser.ParseAllRowsAsync(fileStream, fileName, schema, cancellationToken);

        // 4. Đối chiếu danh sách SKU với Database của Warehouse
        var existingItems = await _dbContext.InventoryItems
            .AsNoTracking()
            .Where(i => i.WarehouseId == effectiveWarehouseId)
            .ToListAsync(cancellationToken);

        var existingSkuMap = existingItems.ToDictionary(i => i.SkuId, i => i);

        var readyItems = new List<InventoryImportPreviewItem>();
        var unmatchedItems = new List<InventoryImportUnmatchedItem>();

        foreach (var row in parsedRows)
        {
            if (string.IsNullOrWhiteSpace(row.RawSkuCode))
            {
                continue;
            }

            // Kiểm tra xem RawSkuCode có phải là Guid SKU hợp lệ không
            if (Guid.TryParse(row.RawSkuCode, out var skuGuid))
            {
                if (existingSkuMap.TryGetValue(skuGuid, out var existingItem))
                {
                    // Đã tồn tại SKU trong kho
                    var newTotal = existingItem.TotalQuantity + row.Quantity;
                    if (newTotal < 0 || newTotal < existingItem.ReservedQuantity)
                    {
                        readyItems.Add(new InventoryImportPreviewItem(
                            RowNumber: row.RowNumber,
                            SkuId: skuGuid,
                            SkuCode: row.RawSkuCode,
                            Quantity: row.Quantity,
                            Note: row.Note,
                            IsValid: false,
                            ErrorMessage: $"Resulting stock ({newTotal}) cannot be less than reserved quantity ({existingItem.ReservedQuantity})."));
                    }
                    else
                    {
                        readyItems.Add(new InventoryImportPreviewItem(
                            RowNumber: row.RowNumber,
                            SkuId: skuGuid,
                            SkuCode: row.RawSkuCode,
                            Quantity: row.Quantity,
                            Note: row.Note,
                            IsValid: true,
                            ErrorMessage: null));
                    }
                }
                else
                {
                    // SKU dạng Guid nhưng chưa có trong kho này
                    if (row.Quantity < 0)
                    {
                        unmatchedItems.Add(new InventoryImportUnmatchedItem(
                            RowNumber: row.RowNumber,
                            RawSkuCode: row.RawSkuCode,
                            Quantity: row.Quantity,
                            Note: row.Note,
                            Reason: "CANNOT_DEDUCT_UNINITIALIZED_SKU"));
                    }
                    else
                    {
                        readyItems.Add(new InventoryImportPreviewItem(
                            RowNumber: row.RowNumber,
                            SkuId: skuGuid,
                            SkuCode: row.RawSkuCode,
                            Quantity: row.Quantity,
                            Note: row.Note,
                            IsValid: true,
                            ErrorMessage: null));
                    }
                }
            }
            else
            {
                // RawSkuCode là mã chuỗi tùy chỉnh của Seller (chưa map sang Guid SKU trong catalog)
                unmatchedItems.Add(new InventoryImportUnmatchedItem(
                    RowNumber: row.RowNumber,
                    RawSkuCode: row.RawSkuCode,
                    Quantity: row.Quantity,
                    Note: row.Note,
                    Reason: "SKU_NOT_FOUND"));
            }
        }

        return new InventoryImportPreviewResponse(
            WarehouseId: effectiveWarehouseId,
            TotalRowsRead: parsedRows.Count,
            ReadyToImportItems: readyItems,
            UnmatchedNewItems: unmatchedItems);
    }
}
