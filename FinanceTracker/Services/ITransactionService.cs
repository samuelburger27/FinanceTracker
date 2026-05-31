using FinanceTracker.Models;

namespace FinanceTracker.Services;

/// <summary>
/// Read/write access to a user's transactions, with an in-memory cache used to avoid
/// round-tripping the database for every page navigation.
/// Mutating calls invalidate the cached entries for the owning user.
/// </summary>
public interface ITransactionService
{
    /// <summary>
    /// Returns the user's transactions, newest first. Categories are eagerly loaded.
    /// Results are cached per user; call <see cref="ClearCache"/> on logout.
    /// </summary>
    Task<IReadOnlyList<Transaction>> GetTransactionsAsync(int userId);

    Task<Transaction> AddTransactionAsync(Transaction transaction);
    Task UpdateTransactionAsync(Transaction transaction);
    Task DeleteTransactionAsync(int transactionId);

    /// <summary>
    /// Drops cached transactions. Call on logout, after bulk imports, or when a referenced
    /// entity (e.g. a Category) is renamed.
    /// </summary>
    void ClearCache();
}
