namespace Fulfillment.Infrastructure.Excel;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Fulfillment.Application.Interfaces.AI;

public class InventoryExcelParser : IInventoryExcelParser
{
    public Task<IReadOnlyList<IReadOnlyList<string>>> ExtractSampleRowsAsync(
        Stream stream,
        string fileName,
        int maxRows = 10,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var isCsv = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);

        if (isCsv)
        {
            return Task.FromResult(ExtractSampleFromCsv(stream, maxRows));
        }

        return Task.FromResult(ExtractSampleFromExcel(stream, maxRows));
    }

    public Task<IReadOnlyList<RawInventoryImportRow>> ParseAllRowsAsync(
        Stream stream,
        string fileName,
        TableSchemaInferenceResult schema,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(schema);

        var isCsv = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);

        if (isCsv)
        {
            return Task.FromResult(ParseAllFromCsv(stream, schema));
        }

        return Task.FromResult(ParseAllFromExcel(stream, schema));
    }

    private static IReadOnlyList<IReadOnlyList<string>> ExtractSampleFromExcel(Stream stream, int maxRows)
    {
        var sample = new List<IReadOnlyList<string>>();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null) return sample;

        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var lastColUsed = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        for (int r = 1; r <= lastRowUsed && sample.Count < maxRows; r++)
        {
            var row = worksheet.Row(r);
            if (row.IsEmpty()) continue;

            var rowValues = new List<string>();
            for (int c = 1; c <= Math.Min(lastColUsed, 20); c++)
            {
                rowValues.Add(row.Cell(c).GetString().Trim());
            }

            if (rowValues.Any(v => !string.IsNullOrWhiteSpace(v)))
            {
                sample.Add(rowValues);
            }
        }

        return sample;
    }

    private static IReadOnlyList<IReadOnlyList<string>> ExtractSampleFromCsv(Stream stream, int maxRows)
    {
        var sample = new List<IReadOnlyList<string>>();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

        string? line;
        while ((line = reader.ReadLine()) != null && sample.Count < maxRows)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = SplitCsvLine(line);
            sample.Add(parts);
        }

        return sample;
    }

    private static IReadOnlyList<RawInventoryImportRow> ParseAllFromExcel(Stream stream, TableSchemaInferenceResult schema)
    {
        var result = new List<RawInventoryImportRow>();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null) return result;

        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        int 엑celStartRow = schema.HeaderRowIndex + 2; // +1 for 0-index -> 1-index, +1 for row after header

        for (int r = 엑celStartRow; r <= lastRowUsed; r++)
        {
            var row = worksheet.Row(r);
            if (row.IsEmpty()) continue;

            var skuCell = row.Cell(schema.SkuColIndex + 1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(skuCell)) continue;

            var qtyStr = row.Cell(schema.QuantityColIndex + 1).GetString().Trim();
            int.TryParse(qtyStr, NumberStyles.Integer | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var rawQty);

            string? note = schema.NoteColIndex >= 0
                ? row.Cell(schema.NoteColIndex + 1).GetString().Trim()
                : null;

            string rawOp = schema.OperationColIndex >= 0
                ? row.Cell(schema.OperationColIndex + 1).GetString().Trim()
                : string.Empty;

            var effectiveQty = CalculateEffectiveQuantity(rawQty, rawOp, schema);

            result.Add(new RawInventoryImportRow(
                RowNumber: r,
                RawSkuCode: skuCell,
                Quantity: effectiveQty,
                Note: note,
                RawOperation: rawOp));
        }

        return result;
    }

    private static IReadOnlyList<RawInventoryImportRow> ParseAllFromCsv(Stream stream, TableSchemaInferenceResult schema)
    {
        var result = new List<RawInventoryImportRow>();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

        int currentLine = 0;
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            currentLine++;
            if (currentLine <= schema.HeaderRowIndex + 1) continue;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = SplitCsvLine(line);
            if (schema.SkuColIndex >= parts.Count) continue;

            var skuCell = parts[schema.SkuColIndex].Trim();
            if (string.IsNullOrWhiteSpace(skuCell)) continue;

            int rawQty = 0;
            if (schema.QuantityColIndex < parts.Count)
            {
                int.TryParse(parts[schema.QuantityColIndex].Trim(), NumberStyles.Integer | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out rawQty);
            }

            string? note = schema.NoteColIndex >= 0 && schema.NoteColIndex < parts.Count
                ? parts[schema.NoteColIndex].Trim()
                : null;

            string rawOp = schema.OperationColIndex >= 0 && schema.OperationColIndex < parts.Count
                ? parts[schema.OperationColIndex].Trim()
                : string.Empty;

            var effectiveQty = CalculateEffectiveQuantity(rawQty, rawOp, schema);

            result.Add(new RawInventoryImportRow(
                RowNumber: currentLine,
                RawSkuCode: skuCell,
                Quantity: effectiveQty,
                Note: note,
                RawOperation: rawOp));
        }

        return result;
    }

    private static int CalculateEffectiveQuantity(int rawQty, string rawOp, TableSchemaInferenceResult schema)
    {
        if (schema.Mode.Equals("OPERATION_COL", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(rawOp))
        {
            var opLower = rawOp.ToLowerInvariant();
            var isIntake = schema.IntakeKeywords.Any(k => opLower.Contains(k.ToLowerInvariant()));
            var isAdjust = schema.AdjustKeywords.Any(k => opLower.Contains(k.ToLowerInvariant()));

            if (isAdjust && rawQty > 0)
            {
                return -rawQty;
            }
            if (isIntake && rawQty < 0)
            {
                return Math.Abs(rawQty);
            }
        }

        return rawQty;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString().Trim());
        return result;
    }
}

