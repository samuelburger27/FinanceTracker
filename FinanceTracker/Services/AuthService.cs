using FinanceTracker.Models;
using FinanceTracker.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Services;

public class AuthService : IAuthService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public User? CurrentUser { get; private set; }

    public AuthService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<(bool Success, string ErrorMessage)> RegisterAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
            return (false, "Username is required.");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return (false, "Password must be at least 6 characters.");

        var normalized = NormalizeUsername(username);

        await using var context = await _contextFactory.CreateDbContextAsync();

        var exists = await context.Users.AnyAsync(u => u.Username == normalized);
        if (exists)
            return (false, "Username already taken.");

        var user = new User
        {
            Username = normalized,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        CurrentUser = user;
        return (true, string.Empty);
    }

    public async Task<(bool Success, string ErrorMessage)> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (false, "Username and password are required.");

        var normalized = NormalizeUsername(username);

        await using var context = await _contextFactory.CreateDbContextAsync();

        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == normalized);
        if (user == null)
            return (false, "Invalid username or password.");

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return (false, "Invalid username or password.");

        CurrentUser = user;
        return (true, string.Empty);
    }

    public void Logout()
    {
        CurrentUser = null;
    }

    // Usernames are stored lowercase so registration and login agree on identity
    // regardless of input casing, without relying on SQLite collation.
    private static string NormalizeUsername(string username) =>
        username.Trim().ToLowerInvariant();
}
