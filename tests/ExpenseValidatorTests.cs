using Xunit;

public class ExpenseValidatorTests
{
    [Fact]
    public void Valid_input_has_no_validation_errors()
    {
        var errors = ExpenseValidator.Validate(new ExpenseInput("Groceries", 45.50m, "Food", null));

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(null, 10, "Food")]
    [InlineData("Coffee", 0, "Food")]
    [InlineData("Coffee", -1, "Food")]
    [InlineData("Coffee", 10, null)]
    public void Invalid_input_returns_errors(string? title, decimal amount, string? category)
    {
        var errors = ExpenseValidator.Validate(new ExpenseInput(title, amount, category, null));

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Null_input_returns_a_clear_error()
    {
        var errors = ExpenseValidator.Validate(null);

        Assert.Contains("Request body must be a JSON object", errors);
    }
}
