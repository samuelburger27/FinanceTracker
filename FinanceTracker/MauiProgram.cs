using CommunityToolkit.Maui;
using FinanceTracker.Services;
using FinanceTracker.Services.Database;
using FinanceTracker.ViewModels;
using FinanceTracker.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FinanceTracker;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // SQLite native bindings must be initialized before any DbContext is created.
        SQLitePCL.Batteries_V2.Init();

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Database
        builder.Services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite($"Data Source={DbConfig.DatabasePath}"));

        // Services
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<ITransactionService, TransactionService>();
        builder.Services.AddSingleton<ICategoryService, CategoryService>();
        builder.Services.AddSingleton<ICsvService, CsvService>();

        // ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<TransactionListViewModel>();
        builder.Services.AddTransient<AddEditTransactionViewModel>();
        builder.Services.AddTransient<StatisticsViewModel>();
        builder.Services.AddTransient<CategoriesViewModel>();

        // Pages
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<AddEditTransactionPage>();
        builder.Services.AddTransient<StatisticsPage>();
        builder.Services.AddTransient<CategoriesPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Create the schema once at startup rather than on every DbContext instantiation.
        using (var scope = app.Services.CreateScope())
        {
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var context = factory.CreateDbContext();
            context.Database.EnsureCreated();
        }

        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(MauiProgram));
        logger.LogInformation("Database path: {Path}", DbConfig.DatabasePath);

        return app;
    }
}