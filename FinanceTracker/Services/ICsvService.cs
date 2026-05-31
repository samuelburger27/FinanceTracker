using FinanceTracker.Models;

namespace FinanceTracker.Services;

/// <summary>
/// Result of a CSV import.
/// </summary>
/// <param name="Imported">Number of transactions written to the database.</param>
/// <param name="Skipped">Rows rejected because of validation errors.</param>
/// <param name="Errors">Per-row, user-facing error messages (line-numbered).</param>
public record CsvImportResult(int Imported, int Skipped, IReadOnlyList<string> Errors);

/// <summary>
/// Imports and exports transactions in CSV format. The expected header is
/// <c>Date,Type,Category,Amount,Note</c>; missing categories on import are
/// auto-created as user-owned.
/// </summary>
public interface ICsvService
{
    /// <summary>
    /// Serializes <paramref name="transactions"/> to a CSV document with the canonical header.
    /// </summary>
    string ExportToCsv(IEnumerable<Transaction> transactions);

    /// <summary>
    /// Parses a CSV stream and inserts the rows for <paramref name="userId"/>.
    /// Invalid rows are skipped and reported in the result; the import otherwise succeeds.
    /// </summary>
    Task<CsvImportResult> ImportFromCsvAsync(Stream stream, int userId);
}
