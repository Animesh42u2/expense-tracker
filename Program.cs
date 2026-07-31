using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ExpenseStore>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    message = "Smart Expense Tracker API",
    documentation = "See README.md and openapi.yaml.",
    endpoints = new[]
    {
        "GET /expenses?category={category}",
        "GET /expenses/{id}",
        "POST /expenses",
        "PUT /expenses/{id}",
        "DELETE /expenses/{id}",
        "GET /expenses/totals/summary?category={category}"
    }
}));

app.MapGet("/expenses", (string? category, ExpenseStore store) =>
{
    var expenses = store.ReadAll();
    if (!string.IsNullOrWhiteSpace(category))
    {
        expenses = expenses
            .Where(expense => string.Equals(expense.Category, category, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    return Results.Ok(expenses);
});

app.MapGet("/expenses/totals/summary", (string? category, ExpenseStore store) =>
{
    var expenses = store.ReadAll();
    if (!string.IsNullOrWhiteSpace(category))
    {
        var matching = expenses
            .Where(expense => string.Equals(expense.Category, category, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Results.Ok(new { category, total = matching.Sum(expense => expense.Amount), count = matching.Count });
    }

    var byCategory = expenses
        .GroupBy(expense => expense.Category)
        .ToDictionary(group => group.Key, group => group.Sum(expense => expense.Amount));
    return Results.Ok(new { overallTotal = expenses.Sum(expense => expense.Amount), byCategory });
});

app.MapGet("/expenses/{id:guid}", (Guid id, ExpenseStore store) =>
{
    var expense = store.ReadAll().FirstOrDefault(item => item.Id == id);
    return expense is null ? Results.NotFound(new { error = "Expense not found" }) : Results.Ok(expense);
});

app.MapPost("/expenses", (ExpenseInput? input, ExpenseStore store) =>
{
    var errors = ExpenseValidator.Validate(input);
    if (errors.Count > 0) return Results.BadRequest(new { errors });

    var validInput = input!;
    var expense = new Expense(Guid.NewGuid(), validInput.Title!.Trim(), validInput.Amount, validInput.Category!.Trim(), validInput.Date ?? DateOnly.FromDateTime(DateTime.UtcNow));
    var expenses = store.ReadAll();
    expenses.Add(expense);
    store.WriteAll(expenses);
    return Results.Created($"/expenses/{expense.Id}", expense);
});

app.MapPut("/expenses/{id:guid}", (Guid id, ExpenseInput? input, ExpenseStore store) =>
{
    var errors = ExpenseValidator.Validate(input);
    if (errors.Count > 0) return Results.BadRequest(new { errors });

    var expenses = store.ReadAll();
    var index = expenses.FindIndex(item => item.Id == id);
    if (index < 0) return Results.NotFound(new { error = "Expense not found" });

    var existing = expenses[index];
    var validInput = input!;
    var updated = new Expense(existing.Id, validInput.Title!.Trim(), validInput.Amount, validInput.Category!.Trim(), validInput.Date ?? existing.Date);
    expenses[index] = updated;
    store.WriteAll(expenses);
    return Results.Ok(updated);
});

app.MapDelete("/expenses/{id:guid}", (Guid id, ExpenseStore store) =>
{
    var expenses = store.ReadAll();
    var index = expenses.FindIndex(item => item.Id == id);
    if (index < 0) return Results.NotFound(new { error = "Expense not found" });

    var removed = expenses[index];
    expenses.RemoveAt(index);
    store.WriteAll(expenses);
    return Results.Ok(new { message = "Expense deleted", expense = removed });
});

app.Run();

public record Expense(Guid Id, string Title, decimal Amount, string Category, DateOnly Date);
public record ExpenseInput(string? Title, decimal Amount, string? Category, DateOnly? Date);
public record LegacyExpense(Guid Id, string Title, decimal Amount, string Category, DateTimeOffset Date);

public static class ExpenseValidator
{
    public static List<string> Validate(ExpenseInput? input)
    {
        var errors = new List<string>();
        if (input is null) return new List<string> { "Request body must be a JSON object" };
        if (string.IsNullOrWhiteSpace(input.Title)) errors.Add("title is required and must be a non-empty string");
        if (input.Amount <= 0) errors.Add("amount must be greater than 0");
        if (string.IsNullOrWhiteSpace(input.Category)) errors.Add("category is required and must be a non-empty string");
        return errors;
    }
}

public sealed class ExpenseStore
{
    private readonly string _filePath = Path.Combine(Directory.GetCurrentDirectory(), "data", "expenses.json");
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly object _lock = new();

    public List<Expense> ReadAll()
    {
        lock (_lock)
        {
            EnsureFile();
            var json = File.ReadAllText(_filePath);
            try
            {
                return JsonSerializer.Deserialize<List<Expense>>(json, _jsonOptions) ?? [];
            }
            catch (JsonException)
            {
                // Accept timestamp entries written by the earlier version, including mixed old/new files.
                var entries = JsonSerializer.Deserialize<List<JsonElement>>(json, _jsonOptions) ?? [];
                return entries.Select(entry =>
                {
                    try
                    {
                        return entry.Deserialize<Expense>(_jsonOptions)!;
                    }
                    catch (JsonException)
                    {
                        var legacy = entry.Deserialize<LegacyExpense>(_jsonOptions)!;
                        return new Expense(
                            legacy.Id,
                            legacy.Title,
                            legacy.Amount,
                            legacy.Category,
                            DateOnly.FromDateTime(legacy.Date.UtcDateTime));
                    }
                }).ToList();
            }
        }
    }

    public void WriteAll(List<Expense> expenses)
    {
        lock (_lock)
        {
            EnsureFile();
            File.WriteAllText(_filePath, JsonSerializer.Serialize(expenses, _jsonOptions));
        }
    }

    private void EnsureFile()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        if (!File.Exists(_filePath)) File.WriteAllText(_filePath, "[]");
    }
}
