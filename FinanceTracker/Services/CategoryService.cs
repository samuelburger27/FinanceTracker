using FinanceTracker.Models;
using FinanceTracker.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Services;

public class CategoryService : ICategoryService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ITransactionService _transactionService;
    private readonly object _lock = new();
    private List<Category>? _cache;

    public CategoryService(IDbContextFactory<AppDbContext> contextFactory, ITransactionService transactionService)
    {
        _contextFactory = contextFactory;
        _transactionService = transactionService;
    }

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(int? userId)
    {
        List<Category>? all;
        lock (_lock) { all = _cache; }

        if (all == null)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            all = await context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();
            lock (_lock) { _cache = all; }
        }

        return all.Where(c => c.UserId == null || c.UserId == userId).ToList();
    }

    public async Task<(bool Success, string ErrorMessage, Category? Category)> AddCategoryAsync(string name, int userId)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed))
            return (false, "Name is required.", null);
        if (trimmed.Length > 100)
            return (false, "Name must be 100 characters or fewer.", null);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var clash = await context.Categories
            .AnyAsync(c => (c.UserId == null || c.UserId == userId)
                && c.Name.ToLower() == trimmed.ToLower());
        if (clash)
            return (false, "A category with that name already exists.", null);

        var category = new Category { Name = trimmed, UserId = userId };
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        ClearCache();
        return (true, string.Empty, category);
    }

    public async Task<(bool Success, string ErrorMessage)> RenameCategoryAsync(int categoryId, string newName, int userId)
    {
        var trimmed = newName?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed))
            return (false, "Name is required.");
        if (trimmed.Length > 100)
            return (false, "Name must be 100 characters or fewer.");

        await using var context = await _contextFactory.CreateDbContextAsync();
        var category = await context.Categories.FindAsync(categoryId);
        if (category == null)
            return (false, "Category not found.");
        if (category.UserId != userId)
            return (false, "Predefined categories cannot be renamed.");

        var clash = await context.Categories
            .AnyAsync(c => c.Id != categoryId
                && (c.UserId == null || c.UserId == userId)
                && c.Name.ToLower() == trimmed.ToLower());
        if (clash)
            return (false, "A category with that name already exists.");

        category.Name = trimmed;
        await context.SaveChangesAsync();
        ClearCache();
        // Cached transactions hold this Category reference, so their cached projection is now stale.
        _transactionService.ClearCache();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string ErrorMessage)> DeleteCategoryAsync(int categoryId, int userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var category = await context.Categories.FindAsync(categoryId);
        if (category == null)
            return (false, "Category not found.");
        if (category.UserId != userId)
            return (false, "Predefined categories cannot be deleted.");

        var inUse = await context.Transactions.AnyAsync(t => t.CategoryId == categoryId);
        if (inUse)
            return (false, "Category is in use by one or more transactions.");

        context.Categories.Remove(category);
        await context.SaveChangesAsync();
        ClearCache();
        return (true, string.Empty);
    }

    public void ClearCache()
    {
        lock (_lock) { _cache = null; }
    }
}
