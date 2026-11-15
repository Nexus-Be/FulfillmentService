namespace Fulfillment.Application.Interfaces.AI;

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public sealed record RawInventoryImportRow(
    int RowNumber,
    string RawSkuCode,
    int Quantity,
    string? Note,
    string RawOperation);

public interface IInventoryExcelParser
{
    Task<IReadOnlyList<IReadOnlyList<string>>> ExtractSampleRowsAsync(
        Stream stream,
        string fileName,
        int maxRows = 10,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RawInventoryImportRow>> ParseAllRowsAsync(
        Stream stream,
        string fileName,
        TableSchemaInferenceResult schema,
        CancellationToken cancellationToken = default);
}

