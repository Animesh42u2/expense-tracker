# AI Notes

## AI assistance

AI assistance was used to help restructure the initial Node.js prototype into the required C# / ASP.NET Core implementation, including the minimal API endpoints, JSON persistence layer, validation, OpenAPI contract, and README.

## Validation performed

- Built the application with `dotnet build --no-restore` with zero warnings and zero errors.
- Ran `dotnet test tests/ExpenseTracker.Tests.csproj`: 6 tests passed.
- Started the API locally and tested creating, updating, filtering, calculating category totals, and deleting an expense.
- Confirmed validation requires a non-empty title and category and an amount greater than zero.

## Design decisions

- The application uses JSON-file storage in `data/expenses.json`, which is allowed by the assignment and avoids requiring a database.
- A small lock protects read/write operations inside the single-process application.
- The API contract is documented in `openapi.yaml`; no additional bonus feature was added.
