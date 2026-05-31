using FinanceTracker.Models;

namespace FinanceTracker.Services;

/// <summary>
/// Read/write access to predefined and user-owned categories, with an in-memory cache.
/// Renaming a category invalidates the related transaction cache because cached transactions
/// hold strong references to <see cref="Category"/> instances.
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Returns predefined categories plus those owned by <paramref name="userId"/>.
    /// Pass <c>null</c> to receive predefined categories only. Results are cached;
    /// call <see cref="ClearCache"/> on logout.
    /// </summary>
    Task<IReadOnlyList<Category>> GetCategoriesAsync(int? userId);

    /// <summary>
    /// Adds a user-owned category.
    /// </summary>
    /// <returns>
    /// On success, <c>Success</c> is <c>true</c> and <c>Category</c> is the persisted entity.
    /// On failure, <c>ErrorMessage</c> is a user-facing reason (e.g. duplicate name) and
    /// <c>Category</c> is <c>null</c>.
    /// </returns>
    Task<(bool Success, string ErrorMessage, Category? Category)> AddCategoryAsync(string name, int userId);

    /// <summary>
    /// Renames a user-owned category. Predefined categories cannot be renamed.
    /// </summary>
    Task<(bool Success, string ErrorMessage)> RenameCategoryAsync(int categoryId, string newName, int userId);

    /// <summary>
    /// Deletes a user-owned category. Fails when the category is referenced by any
    /// transaction or when the category is predefined.
    /// </summary>
    Task<(bool Success, string ErrorMessage)> DeleteCategoryAsync(int categoryId, int userId);

    /// <summary>
    /// Drops the category cache. Call on logout or after bulk imports.
    /// </summary>
    void ClearCache();
}
