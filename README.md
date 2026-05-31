# Finance Tracker

A personal finance management desktop application built with .NET MAUI and C#. Track your income and expenses, categorize transactions, and visualize your spending with charts.

## Features

- **User Authentication** — Register and log in with securely hashed passwords (BCrypt)
- **Transaction Management** — Add, edit, and delete income/expense records
- **Category Management** — Predefined categories (Housing, Food, Salary, etc.) plus custom ones
- **Dashboard** — View net balance, monthly income, and monthly expenses at a glance
- **Filtering & Sorting** — Filter transactions by category, month, year, or type
- **Statistics** — Donut chart for expense breakdown, bar chart for income vs. expense trends
- **Import/Export** — Export and import transactions via CSV

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (or later)
- macOS or Windows

## How to Run

```bash
# Build the solution
dotnet build FinanceTracker.sln

# Run on macOS
dotnet run --project FinanceTracker/FinanceTracker.csproj -f net10.0-maccatalyst

# Run on Windows
dotnet run --project FinanceTracker/FinanceTracker.csproj -f net10.0-windows10.0.19041.0
```

## Seed Demo Data (Optional)

To populate the app with sample transactions for testing:

```bash
./seed-demo.sh                              # creates user demo/demo123 with 90 transactions
./seed-demo.sh --reset                      # wipe and re-seed
./seed-demo.sh --user alice --password pw   # custom credentials
```

## Usage

1. **Register** a new account or **log in** with existing credentials.
2. On the **main page**, view your transaction list and current balance.
3. Tap **Add** to create a new income or expense entry — select a category, amount, date, and optional note.
4. Use the **filter bar** to narrow transactions by category, type, or date range.
5. Visit the **Statistics** page to see visual breakdowns of your spending and income trends.
6. Use **Export** to save your data as CSV, or **Import** to load transactions from a CSV file.
