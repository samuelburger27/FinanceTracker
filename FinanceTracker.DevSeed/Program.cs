using FinanceTracker.Models;
using FinanceTracker.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.DevSeed;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var options = ParseArgs(args);
        if (options is null) return 1;

        // Match AuthService normalization so seeded users can log in regardless of casing.
        options = options with { Username = options.Username.Trim().ToLowerInvariant() };

        Console.WriteLine($"Seeding FinanceTracker database");
        Console.WriteLine($"   DB:       {options.DbPath}");
        Console.WriteLine($"   User:     {options.Username}");
        Console.WriteLine($"   Password: {options.Password}");
        Console.WriteLine($"   Reset:    {options.Reset}");
        Console.WriteLine();

        Directory.CreateDirectory(Path.GetDirectoryName(options.DbPath)
            ?? throw new InvalidOperationException("DB path has no directory."));

        SQLitePCL.Batteries_V2.Init();

        var contextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={options.DbPath}")
            .Options;

        await using var db = new AppDbContext(contextOptions);
        await db.Database.EnsureCreatedAsync();

        if (options.Reset)
        {
            var existing = await db.Users.FirstOrDefaultAsync(u => u.Username == options.Username);
            if (existing != null)
            {
                var oldTx = db.Transactions.Where(t => t.UserId == existing.Id);
                db.Transactions.RemoveRange(oldTx);
                db.Users.Remove(existing);
                await db.SaveChangesAsync();
                Console.WriteLine($"   Removed existing user '{options.Username}' and their transactions.");
            }
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == options.Username);
        if (user is null)
        {
            user = new User
            {
                Username = options.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(options.Password),
                CreatedAt = DateTime.UtcNow,
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            Console.WriteLine($"   Created user '{user.Username}' (id={user.Id}).");
        }
        else
        {
            Console.WriteLine($"   Reusing existing user '{user.Username}' (id={user.Id}). " +
                              "Pass --reset to wipe their data first.");
        }

        var existingCount = await db.Transactions.CountAsync(t => t.UserId == user.Id);
        if (existingCount > 0 && !options.Reset)
        {
            Console.WriteLine($"   User already has {existingCount} transactions. " +
                              "Skipping seed. Pass --reset to overwrite.");
            return 0;
        }

        var categories = await db.Categories.ToListAsync();
        var transactions = BuildTransactions(user.Id, categories, options.Seed, options.Count);
        db.Transactions.AddRange(transactions);
        await db.SaveChangesAsync();

        Console.WriteLine($"   Inserted {transactions.Count} transactions.");
        Console.WriteLine();
        Console.WriteLine("✓ Done. Launch the app and log in with the credentials above.");
        return 0;
    }

    private sealed record SeedOptions(
        string DbPath,
        string Username,
        string Password,
        bool Reset,
        int Seed,
        int Count);

    private static SeedOptions? ParseArgs(string[] args)
    {
        var dbPath = DefaultDatabasePath();
        var username = "demo";
        var password = "demo123";
        var reset = false;
        var seed = 42;
        var count = 90;

        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "--db":
                    dbPath = RequireValue(args, ref i, a); break;
                case "--user":
                    username = RequireValue(args, ref i, a); break;
                case "--password":
                    password = RequireValue(args, ref i, a); break;
                case "--reset":
                    reset = true; break;
                case "--seed":
                    seed = int.Parse(RequireValue(args, ref i, a)); break;
                case "--count":
                    count = int.Parse(RequireValue(args, ref i, a)); break;
                case "-h":
                case "--help":
                    PrintUsage(); return null;
                default:
                    Console.Error.WriteLine($"Unknown argument: {a}");
                    PrintUsage();
                    return null;
            }
        }

        return new SeedOptions(dbPath, username, password, reset, seed, count);
    }

    private static string RequireValue(string[] args, ref int i, string flag)
    {
        if (i + 1 >= args.Length)
            throw new ArgumentException($"Missing value for {flag}");
        return args[++i];
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""
            FinanceTracker.DevSeed — seeds the local SQLite database with demo data.

            Usage:
              dotnet run -- [options]

            Options:
              --db <path>         SQLite database file (default: platform app-data path)
              --user <name>       Username (default: demo)
              --password <pw>     Password (default: demo123)
              --count <n>         How many transactions to create (default: 90)
              --seed <int>        RNG seed for reproducibility (default: 42)
              --reset             Wipe the user's existing data before seeding
              -h, --help          Show this message
            """);
    }

    private static string DefaultDatabasePath()
    {
        // Mirror what FileSystem.AppDataDirectory resolves to on each desktop target.
        // On macOS Catalyst (unsandboxed debug), the container lives here.
        if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Containers",
                "com.companyname.financetracker", "Data", "Library",
                "finance_tracker_v1.db");
        }
        if (OperatingSystem.IsWindows())
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(local, "Packages",
                "com.companyname.financetracker_<hash>", "LocalState",
                "finance_tracker_v1.db");
        }
        var app = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(app, "FinanceTracker", "finance_tracker_v1.db");
    }

    private record CategoryTemplate(
        string Name,
        TransactionType Type,
        decimal Min,
        decimal Max,
        int Weight,
        string[] Notes);

    private static readonly CategoryTemplate[] Templates =
    {
        new("Salary",        TransactionType.Income,  2800, 3400, 2,
            new[] { "Monthly paycheck", "Company payroll", "Salary deposit" }),
        new("Food",          TransactionType.Expense,   8,   65, 14,
            new[] { "Lunch out", "Coffee run", "Groceries - corner store", "Dinner with friends", "Breakfast" }),
        new("Groceries",     TransactionType.Expense,  25,  140, 10,
            new[] { "Weekly groceries", "Supermarket", "Farmer's market" }),
        new("Housing",       TransactionType.Expense, 850, 1200, 2,
            new[] { "Rent", "Maintenance fee", "HOA payment" }),
        new("Utilities",     TransactionType.Expense,  40,  160, 4,
            new[] { "Electricity", "Internet", "Water bill", "Gas bill" }),
        new("Transport",     TransactionType.Expense,   3,   90, 9,
            new[] { "Metro card top-up", "Taxi", "Fuel", "Bike service", "Train ticket" }),
        new("Entertainment", TransactionType.Expense,  10,  110, 6,
            new[] { "Cinema", "Concert", "Streaming subscription", "Games", "Museum" }),
        new("Healthcare",    TransactionType.Expense,  15,  220, 3,
            new[] { "Pharmacy", "Dentist", "Doctor visit", "Gym membership" }),
        new("Other",         TransactionType.Expense,   5,   80, 4,
            new[] { "Gift", "Donation", "Misc", "Household supplies" }),
    };

    private static List<Transaction> BuildTransactions(int userId, List<Category> categories, int seed, int count)
    {
        var rng = new Random(seed);
        var now = DateTime.Today;
        var earliest = now.AddMonths(-5).AddDays(-now.Day + 1);

        var totalWeight = Templates.Sum(t => t.Weight);
        var result = new List<Transaction>(count);

        // Ensure a monthly salary on the 1st of each of the last 6 months.
        var salary = Templates.First(t => t.Name == "Salary");
        var salaryCategory = ResolveCategory(categories, salary.Name);
        if (salaryCategory != null)
        {
            for (int i = 5; i >= 0; i--)
            {
                var date = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                if (date > now) continue;
                result.Add(new Transaction
                {
                    UserId = userId,
                    CategoryId = salaryCategory.Id,
                    Type = salary.Type,
                    Amount = RandomAmount(rng, salary.Min, salary.Max),
                    Date = date,
                    Note = salary.Notes[rng.Next(salary.Notes.Length)],
                });
            }
        }

        while (result.Count < count)
        {
            var pick = rng.Next(totalWeight);
            var running = 0;
            CategoryTemplate template = Templates[0];
            foreach (var t in Templates)
            {
                running += t.Weight;
                if (pick < running) { template = t; break; }
            }

            var category = ResolveCategory(categories, template.Name);
            if (category is null) continue;

            var days = (now - earliest).Days;
            var date = earliest.AddDays(rng.Next(days + 1));

            result.Add(new Transaction
            {
                UserId = userId,
                CategoryId = category.Id,
                Type = template.Type,
                Amount = RandomAmount(rng, template.Min, template.Max),
                Date = date,
                Note = rng.NextDouble() < 0.7
                    ? template.Notes[rng.Next(template.Notes.Length)]
                    : null,
            });
        }

        return result.OrderByDescending(t => t.Date).ToList();
    }

    private static Category? ResolveCategory(List<Category> categories, string name) =>
        categories.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

    private static decimal RandomAmount(Random rng, decimal min, decimal max)
    {
        var range = (double)(max - min);
        var raw = (decimal)(rng.NextDouble() * range) + min;
        return Math.Round(raw, 2);
    }
}
