namespace Fulfillment.Application.Interfaces.AI;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public sealed record TableSchemaInferenceResult(
    int HeaderRowIndex,
    int SkuColIndex,
    int QuantityColIndex,
    int OperationColIndex,
    int NoteColIndex,
    string Mode, // "SINGLE_QTY_COL" or "OPERATION_COL"
    IReadOnlyList<string> IntakeKeywords,
    IReadOnlyList<string> AdjustKeywords);

public interface IAiSchemaInferenceService
{
    Task<TableSchemaInferenceResult> InferSchemaAsync(
        IReadOnlyList<IReadOnlyList<string>> sampleRows,
        CancellationToken cancellationToken = default);
}

