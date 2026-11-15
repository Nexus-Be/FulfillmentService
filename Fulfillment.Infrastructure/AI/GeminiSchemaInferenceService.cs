namespace Fulfillment.Infrastructure.AI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Interfaces.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class GeminiSchemaInferenceService : IAiSchemaInferenceService
{
    private readonly HttpClient _httpClient;
    private readonly AiOptions _options;
    private readonly ILogger<GeminiSchemaInferenceService> _logger;

    public GeminiSchemaInferenceService(
        IOptions<AiOptions> options,
        ILogger<GeminiSchemaInferenceService> logger,
        HttpClient? httpClient = null)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? new HttpClient();

        if (_options.TimeoutSeconds > 0)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        }
    }

    public async Task<TableSchemaInferenceResult> InferSchemaAsync(
        IReadOnlyList<IReadOnlyList<string>> sampleRows,
        CancellationToken cancellationToken = default)
    {
        if (sampleRows == null || sampleRows.Count == 0)
        {
            return CreateDefaultFallbackSchema();
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogInformation("Gemini API key is not configured. Using deterministic heuristic schema inference.");
            return InferSchemaHeuristically(sampleRows);
        }

        try
        {
            var prompt = BuildPrompt(sampleRows);
            var endpointUrl = $"{_options.Endpoint.TrimEnd('/')}/models/{_options.Model}:generateContent?key={_options.ApiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    response_mime_type = "application/json"
                }
            };

            var response = await _httpClient.PostAsJsonAsync(endpointUrl, requestBody, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Gemini API returned HTTP {StatusCode}: {ErrorText}. Falling back to heuristic inference.", response.StatusCode, errorText);
                return InferSchemaHeuristically(sampleRows);
            }

            var jsonResult = await response.Content.ReadFromJsonAsync<GeminiApiResponse>(cancellationToken: cancellationToken);
            var candidateText = jsonResult?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(candidateText))
            {
                _logger.LogWarning("Gemini API returned empty candidate text. Falling back to heuristic inference.");
                return InferSchemaHeuristically(sampleRows);
            }

            var parsed = JsonSerializer.Deserialize<GeminiInferenceResponse>(candidateText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed == null || parsed.SkuColIndex < 0 || parsed.QuantityColIndex < 0)
            {
                _logger.LogWarning("Gemini API returned incomplete schema. Falling back to heuristic inference.");
                return InferSchemaHeuristically(sampleRows);
            }

            return new TableSchemaInferenceResult(
                HeaderRowIndex: Math.Max(0, parsed.HeaderRowIndex),
                SkuColIndex: parsed.SkuColIndex,
                QuantityColIndex: parsed.QuantityColIndex,
                OperationColIndex: parsed.OperationColIndex ?? -1,
                NoteColIndex: parsed.NoteColIndex ?? -1,
                Mode: !string.IsNullOrWhiteSpace(parsed.Mode) ? parsed.Mode.ToUpperInvariant() : "SINGLE_QTY_COL",
                IntakeKeywords: parsed.IntakeKeywords ?? new List<string> { "nhap", "nhập", "+", "cộng", "in" },
                AdjustKeywords: parsed.AdjustKeywords ?? new List<string> { "xuat", "xuất", "-", "trừ", "out" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to infer schema using Gemini API. Falling back to heuristic inference.");
            return InferSchemaHeuristically(sampleRows);
        }
    }

    private static string BuildPrompt(IReadOnlyList<IReadOnlyList<string>> sampleRows)
    {
        var sampleJson = JsonSerializer.Serialize(sampleRows);
        return $$"""
You are an expert spreadsheet data parser for warehouse inventory management.
Analyze the following sample 2D array of rows extracted from a seller's Excel or CSV file:

{{sampleJson}}

Identify the coordinates of the table columns:
1. "headerRowIndex": (0-based integer) which row contains column headers (e.g., "Mã SKU", "Số lượng", "Ghi chú", etc.). If no obvious header row, use 0.
2. "skuColIndex": (0-based integer) column index containing product SKU, barcode, or product code.
3. "quantityColIndex": (0-based integer) column index containing quantity or amount to adjust.
4. "operationColIndex": (0-based integer, or -1 if none) column indicating whether the row is an Intake (Nhập kho) or Adjustment/Outtake (Xuất kho / Điều chỉnh).
5. "noteColIndex": (0-based integer, or -1 if none) column containing notes, reasons, or remarks.
6. "mode": "SINGLE_QTY_COL" (if positive/negative numbers denote in/out or default intake) or "OPERATION_COL" (if a dedicated column specifies intake vs outtake).
7. "intakeKeywords": array of strings that signify intake (e.g. ["nhập", "cộng", "+", "in"]).
8. "adjustKeywords": array of strings that signify adjustment or outtake (e.g. ["xuất", "trừ", "-", "out"]).

Respond ONLY with valid JSON matching this schema:
{
  "headerRowIndex": 0,
  "skuColIndex": 0,
  "quantityColIndex": 1,
  "operationColIndex": -1,
  "noteColIndex": -1,
  "mode": "SINGLE_QTY_COL",
  "intakeKeywords": ["nhập", "+"],
  "adjustKeywords": ["xuất", "-"]
}
""";
    }

    public static TableSchemaInferenceResult InferSchemaHeuristically(IReadOnlyList<IReadOnlyList<string>> sampleRows)
    {
        if (sampleRows == null || sampleRows.Count == 0)
        {
            return CreateDefaultFallbackSchema();
        }

        int headerRow = 0;
        int skuCol = 0;
        int qtyCol = 1;
        int opCol = -1;
        int noteCol = -1;
        string mode = "SINGLE_QTY_COL";

        // Tìm dòng header tiềm năng nhất
        for (int r = 0; r < Math.Min(sampleRows.Count, 5); r++)
        {
            var row = sampleRows[r];
            for (int c = 0; c < row.Count; c++)
            {
                var text = (row[c] ?? string.Empty).Trim().ToLowerInvariant();

                if (text.Contains("sku") || text.Contains("mã") || text.Contains("code") || text.Contains("san pham") || text.Contains("sản phẩm"))
                {
                    headerRow = r;
                    skuCol = c;
                }
                else if (text.Contains("số lượng") || text.Contains("so luong") || text.Contains("qty") || text.Contains("sl") || text.Contains("tồn") || text.Contains("quantity"))
                {
                    headerRow = r;
                    qtyCol = c;
                }
                else if (text.Contains("loại") || text.Contains("loai") || text.Contains("thao tác") || text.Contains("action") || text.Contains("operation") || text.Contains("type"))
                {
                    opCol = c;
                    mode = "OPERATION_COL";
                }
                else if (text.Contains("ghi chú") || text.Contains("ghi chu") || text.Contains("note") || text.Contains("lý do") || text.Contains("ly do"))
                {
                    noteCol = c;
                }
            }
        }

        // Đảm bảo không trùng cột
        if (skuCol == qtyCol && sampleRows[headerRow].Count > 1)
        {
            qtyCol = (skuCol == 0) ? 1 : 0;
        }

        return new TableSchemaInferenceResult(
            HeaderRowIndex: headerRow,
            SkuColIndex: skuCol,
            QuantityColIndex: qtyCol,
            OperationColIndex: opCol,
            NoteColIndex: noteCol,
            Mode: mode,
            IntakeKeywords: new List<string> { "nhap", "nhập", "+", "cộng", "in", "intake" },
            AdjustKeywords: new List<string> { "xuat", "xuất", "-", "trừ", "out", "adjust" });
    }

    private static TableSchemaInferenceResult CreateDefaultFallbackSchema() =>
        new TableSchemaInferenceResult(
            HeaderRowIndex: 0,
            SkuColIndex: 0,
            QuantityColIndex: 1,
            OperationColIndex: -1,
            NoteColIndex: -1,
            Mode: "SINGLE_QTY_COL",
            IntakeKeywords: new List<string> { "nhập", "+" },
            AdjustKeywords: new List<string> { "xuất", "-" });

    private sealed class GeminiApiResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private sealed class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }
    }

    private sealed class GeminiContent
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart>? Parts { get; set; }
    }

    private sealed class GeminiPart
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private sealed class GeminiInferenceResponse
    {
        [JsonPropertyName("headerRowIndex")]
        public int HeaderRowIndex { get; set; }

        [JsonPropertyName("skuColIndex")]
        public int SkuColIndex { get; set; }

        [JsonPropertyName("quantityColIndex")]
        public int QuantityColIndex { get; set; }

        [JsonPropertyName("operationColIndex")]
        public int? OperationColIndex { get; set; }

        [JsonPropertyName("noteColIndex")]
        public int? NoteColIndex { get; set; }

        [JsonPropertyName("mode")]
        public string? Mode { get; set; }

        [JsonPropertyName("intakeKeywords")]
        public List<string>? IntakeKeywords { get; set; }

        [JsonPropertyName("adjustKeywords")]
        public List<string>? AdjustKeywords { get; set; }
    }
}

