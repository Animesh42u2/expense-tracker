# Smart Expense Tracker API

A C# / ASP.NET Core REST API for managing personal expenses. Expenses are persisted in a local JSON file, so no database is required.

## Requirements

- .NET 8 SDK

## Install dependencies

```bash
dotnet restore
```

## Run the API

```bash
dotnet run
```

The API runs at `http://localhost:5000` by default. Its OpenAPI contract is in `openapi.yaml`.

## Run the tests

```bash
dotnet test tests/ExpenseTracker.Tests.csproj
```

## API endpoints

| Method | Endpoint | Description |
|---|---|---|
| POST | `/expenses` | Add an expense. Body: `{ "title", "amount", "category", "date"? }`; use `YYYY-MM-DD` for `date`. |
| GET | `/expenses` | List all expenses; use `?category=Food` to filter. |
| GET | `/expenses/{id}` | Get an expense by its ID. |
| PUT | `/expenses/{id}` | Update an expense. An omitted `date` keeps its current value. |
| DELETE | `/expenses/{id}` | Delete an expense. |
| GET | `/expenses/totals/summary` | Get overall and per-category totals; use `?category=Food` for one category. |

`title`, `amount`, and `category` are required. `amount` must be greater than zero. All validation errors return HTTP 400 and unknown IDs return HTTP 404.

## Example

```bash
curl -X POST http://localhost:5000/expenses -H "Content-Type: application/json" -d "{\"title\":\"Groceries\",\"amount\":45.50,\"category\":\"Food\"}"
```

## Storage

Data is written to `data/expenses.json`. This is deliberately simple file-based storage for the assignment scope; a database can later replace `ExpenseStore` without changing the endpoints.
