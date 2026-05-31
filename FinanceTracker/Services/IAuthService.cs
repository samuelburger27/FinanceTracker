using FinanceTracker.Models;

namespace FinanceTracker.Services;

/// <summary>
/// Owns the current session: registration, login, logout, and the active user.
/// Usernames are stored normalized (trimmed, lowercase); callers may pass any casing.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// The user authenticated for this session, or <c>null</c> when no one is signed in.
    /// </summary>
    User? CurrentUser { get; }

    /// <summary>
    /// Creates an account and signs the new user in on success.
    /// </summary>
    /// <returns>
    /// <c>Success</c> indicates the account was created and <see cref="CurrentUser"/> is set.
    /// <c>ErrorMessage</c> is a user-facing message when <c>Success</c> is <c>false</c>
    /// (empty otherwise).
    /// </returns>
    Task<(bool Success, string ErrorMessage)> RegisterAsync(string username, string password);

    /// <summary>
    /// Verifies credentials and, on success, sets <see cref="CurrentUser"/>.
    /// </summary>
    /// <returns>
    /// <c>Success</c> indicates the user is now signed in.
    /// <c>ErrorMessage</c> is a user-facing message when <c>Success</c> is <c>false</c>;
    /// it deliberately does not distinguish "no such user" from "wrong password".
    /// </returns>
    Task<(bool Success, string ErrorMessage)> LoginAsync(string username, string password);

    /// <summary>
    /// Clears <see cref="CurrentUser"/>. Safe to call when no one is signed in.
    /// </summary>
    void Logout();
}
