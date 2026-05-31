# Software Requirements Specification: Expense Manager

## 1. Introduction
**1.1 Purpose**
The purpose of this document is to define the requirements for "Expense Manager," a desktop application designed to help users efficiently track, categorize, and analyze their personal income and expenses.

**1.2 Scope**
The application will be developed using C# and .NET MAUI (Multi-platform App UI), targeting desktop environments (Windows/macOS). It will serve as a standalone personal finance tool featuring user authentication, transaction management, data visualization, and import/export capabilities.

---

## 2. Overall Description
**2.1 User Roles**
* **Standard User:** An individual seeking to track their personal finances locally on their machine.

**2.2 Operating Environment**
* **Framework:** .NET MAUI (C#)
* **Platform:** Desktop (Windows 10/11, macOS)
* **Local Storage:** SQLite (recommended for local relational data storage) or local JSON/XML files.

---

## 3. Functional Requirements (FR)

### FR1: User Authentication & Security
* **FR1.1 Registration:** Users must be able to create a local account using a username/email and password.
* **FR1.2 Password Security:** Passwords **must not** be stored in plain text. The application must use a robust cryptographic hashing algorithm (e.g., PBKDF2, BCrypt, or Argon2) with a unique salt to store credentials safely in the local database.
* **FR1.3 Login/Logout:** Users must be able to securely log into their account to access their data and log out to lock the session.

### FR2: Transaction Management (Income & Expenses)
* **FR2.1 Add Transactions:** Users can add new records. Each record must capture:
    * Type (Income or Expense)
    * Amount (Numeric/Currency)
    * Date
    * Category (Selected from a predefined or custom list)
    * Optional Note/Description
* **FR2.2 Edit/Update Transactions:** Users can modify existing transaction details (amount, date, category, etc.).
* **FR2.3 Delete Transactions:** Users can remove transactions from their ledger.

### FR3: Category Management
* **FR3.1 Predefined Categories:** The app will initialize with a set of default categories (e.g., *Housing, Food, Salary, Utilities, Entertainment*).
* **FR3.2 Custom Categories:** Users can create, update, and delete (CRUD) their own custom categories to suit their specific tracking needs.
* **FR3.3 Category Assignment:** Every transaction must be linked to exactly one category.

### FR4: Dashboard & Account Status
* **FR4.1 Current Status:** The application must calculate and display the user's current net balance (Total Income - Total Expenses).
* **FR4.2 Summary Widgets:** Display quick summaries for the current month (e.g., Monthly Income, Monthly Expenses).

### FR5: Data Filtering & Search
* **FR5.1 Multi-Parameter Filtering:** Users can filter their transaction history using one or a combination of the following parameters:
    * Category (e.g., Show only "Groceries")
    * Month (e.g., Show only "October")
    * Year (e.g., Show only "2023")
    * Transaction Type (Income vs. Expense)
* **FR5.2 Sorting:** Users can sort filtered results (e.g., by date descending, amount ascending).

### FR6: Import and Export Data
* **FR6.1 Export:** Users can export their transaction data to a standard format (e.g., CSV) for use in other spreadsheet software.
* **FR6.2 Import:** Users can import transaction data from a standard format (e.g., CSV).

### FR7: Statistics & Data Visualization
* **FR7.1 Graphical Representation:** The app must include a dedicated statistics view using charts 
* **FR7.2 Expense Breakdown:** Display a pie chart or donut chart showing expenses divided by category for a selected time period.
* **FR7.3 Income vs. Expense Trend:** Display a bar chart or line graph comparing income and expenses over time (e.g., month over month).

---

## 4. Non-Functional Requirements (NFR)

* **NFR1: Performance:** The application should load the dashboard and calculate balances in under 2 seconds, even with a history of thousands of transactions.
* **NFR2: Usability (UI/UX):** The interface must be intuitive, following modern desktop design patterns (e.g., sidebar navigation, clean forms, responsive layouts that adjust to window resizing).
* **NFR3: Data Persistence:** Data must be saved automatically upon entry so that no data is lost if the application is closed unexpectedly. All data is kept local to the user's machine to ensure privacy.
* **NFR4: Error Handling:** The application must provide clear, user-friendly error messages for invalid inputs (e.g., entering letters in an amount field) and gracefully handle import failures.