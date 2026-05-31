using System.Globalization;
using System.Text;
using FinanceTracker.Models;
using FinanceTracker.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Services;

public class CsvService : ICsvService
{
    public const string Header = "Date,Type,Category,Amount,Note";

    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ITransactionService _transactionService;
    private readonly ICategoryService _categoryService;

    public CsvService(
        IDbContextFactory<AppDbContext> contextFactory,
        ITransactionService transactionService,
        ICategoryService categoryService)
    {
        _contextFactory = contextFactory;
        _transactionService = transactionService;
        _categoryService = categoryService;
    }

    public string ExportToCsv(IEnumerable<Transaction> transactions)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Header);
        foreach (var t in transactions)
        {
            sb.Append(t.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(t.Type);
            sb.Append(',');
            sb.Append(EscapeField(t.Category?.Name ?? string.Empty));
            sb.Append(',');
            sb.Append(t.Amount.ToString(CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(EscapeField(t.Note ?? string.Empty));
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public async Task<CsvImportResult> ImportFromCsvAsync(Stream stream, int userId)
    {
        var errors = new List<string>();

        using var reader = new StreamReader(stream, Encoding.UTF8);
        var headerLine = await reader.ReadLineAsync();
        if (headerLine == null || !headerLine.Trim().Equals(Header, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"Invalid header. Expected: {Header}");
            return new CsvImportResult(0, 0, errors);
        }

        // Phase 1: parse all rows before touching the DB.
        var parsed = new List<(DateTime Date, TransactionType Type, string CategoryName, decimal Amount, string? Note)>();
        var lineNum = 1;
        var skipped = 0;
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            lineNum++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var fields = ParseCsvLine(line);
            if (fields.Count < 4)
            {
                errors.Add($"Line {lineNum}: expected at least 4 fields, got {fields.Count}");
                skipped++;
                continue;
            }

            if (!DateTime.TryParse(fields[0], CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal, out var date))
            {
                errors.Add($"Line {lineNum}: invalid date '{fields[0]}'");
                skipped++;
                continue;
            }

            if (!Enum.TryParse<TransactionType>(fields[1], true, out var type))
            {
                errors.Add($"Line {lineNum}: invalid type '{fields[1]}' (Income/Expense)");
                skipped++;
                continue;
            }

            var categoryName = fields[2].Trim();
            if (string.IsNullOrEmpty(categoryName))
            {
                errors.Add($"Line {lineNum}: empty category");
                skipped++;
                continue;
            }

            if (!decimal.TryParse(fields[3], NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
                || amount <= 0)
            {
                errors.Add($"Line {lineNum}: invalid amount '{fields[3]}'");
                skipped++;
                continue;
            }

            var note = fields.Count > 4 ? fields[4] : null;
            if (string.IsNullOrWhiteSpace(note)) note = null;

            parsed.Add((date, type, categoryName, amount, note));
        }

        if (parsed.Count == 0)
            return new CsvImportResult(0, skipped, errors);

        await using var context = await _contextFactory.CreateDbContextAsync();

        // Phase 2: build a name→id map from categories that already exist.
        var existing = await context.Categories
            .AsNoTracking()
            .Where(c => c.UserId == null || c.UserId == userId)
            .ToListAsync();
        var categoryIdMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in existing)
            categoryIdMap[c.Name] = c.Id;

        // Phase 3: create any custom categories that are missing, then save them
        // in one batch so each gets a real DB-assigned id before transactions are built.
        var missing = parsed
            .Select(r => r.CategoryName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(name => !categoryIdMap.ContainsKey(name))
            .Select(name => new Category { Name = name, UserId = userId })
            .ToList();

        if (missing.Count > 0)
        {
            context.Categories.AddRange(missing);
            await context.SaveChangesAsync();
            foreach (var c in missing)
                categoryIdMap[c.Name] = c.Id;
        }

        // Phase 4: create transactions with an explicit CategoryId (no navigation-property
        // tracking required, works for both predefined and custom categories).
        var toInsert = parsed.Select(r => new Transaction
        {
            UserId = userId,
            CategoryId = categoryIdMap[r.CategoryName],
            Type = r.Type,
            Amount = r.Amount,
            Date = r.Date,
            Note = r.Note,
        }).ToList();

        context.Transactions.AddRange(toInsert);
        await context.SaveChangesAsync();

        _transactionService.ClearCache();
        _categoryService.ClearCache();

        return new CsvImportResult(toInsert.Count, skipped, errors);
    }

    private static string EscapeField(string value)
    {
        if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
            return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                if (c == ',')
                {
                    fields.Add(sb.ToString());
                    sb.Clear();
                }
                else if (c == '"' && sb.Length == 0)
                {
                    inQuotes = true;
                }
                else
                {
                    sb.Append(c);
                }
            }
        }
        fields.Add(sb.ToString());
        return fields;
    }
}
