using FinanceTracker.Models;
using FinanceTracker.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Services;

public class TransactionService : ITransactionService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly object _lock = new();
    private readonly Dictionary<int, List<Transaction>> _transactionCache = new();

    public TransactionService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<Transaction>> GetTransactionsAsync(int userId)
    {
        lock (_lock)
        {
            if (_transactionCache.TryGetValue(userId, out var cached))
                return cached;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        var list = await context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Include(t => t.Category)
            .OrderByDescending(t => t.Date)
            .ToListAsync();

        lock (_lock) { _transactionCache[userId] = list; }
        return list;
    }

    public async Task<Transaction> AddTransactionAsync(Transaction transaction)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();
        Invalidate(transaction.UserId);
        return transaction;
    }

    public async Task UpdateTransactionAsync(Transaction transaction)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Transactions.Update(transaction);
        await context.SaveChangesAsync();
        Invalidate(transaction.UserId);
    }

    public async Task DeleteTransactionAsync(int transactionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var transaction = await context.Transactions.FindAsync(transactionId);
        if (transaction != null)
        {
            context.Transactions.Remove(transaction);
            await context.SaveChangesAsync();
            Invalidate(transaction.UserId);
        }
    }

    public void ClearCache()
    {
        lock (_lock) { _transactionCache.Clear(); }
    }

    private void Invalidate(int userId)
    {
        lock (_lock) { _transactionCache.Remove(userId); }
    }
}
