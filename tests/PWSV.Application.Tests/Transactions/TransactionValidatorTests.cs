using FluentAssertions;
using NSubstitute;
using PWSV.Application.Common.Interfaces;
using PWSV.Application.Transactions.Commands.CreateExpenseTransaction;
using PWSV.Application.Transactions.Commands.CreateTransferTransaction;

namespace PWSV.Application.Tests.Transactions;

public sealed class TransactionValidatorTests
{
    private static readonly DateTime Now = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0.01, true)]
    [InlineData(999_999_999_999.99, true)]
    [InlineData(0, false)]
    [InlineData(-5, false)]
    [InlineData(0.001, false)]
    [InlineData(1_000_000_000_000, false)]
    public void Expense_ValidatesAmountRangeAndScale(decimal amount, bool expectedValid)
    {
        var validator = new CreateExpenseTransactionCommandValidator(CreateClock());

        var result = validator.Validate(new CreateExpenseTransactionCommand(1, 1, amount, Now, null, null));

        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void Expense_RejectsFutureAndMissingDates()
    {
        var validator = new CreateExpenseTransactionCommandValidator(CreateClock());

        validator.Validate(new CreateExpenseTransactionCommand(1, 1, 10m, Now.AddDays(1), null, null)).IsValid.Should().BeFalse();
        validator.Validate(new CreateExpenseTransactionCommand(1, 1, 10m, default, null, null)).IsValid.Should().BeFalse();
        validator.Validate(new CreateExpenseTransactionCommand(1, 1, 10m, Now.AddSeconds(30), null, null)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(41.25, true)]
    [InlineData(0.00000001, true)]
    [InlineData(0d, false)]
    [InlineData(-1d, false)]
    [InlineData(0.000000001, false)]
    public void Transfer_ValidatesOptionalExchangeRate(double? rate, bool expectedValid)
    {
        var validator = new CreateTransferTransactionCommandValidator(CreateClock());
        var command = new CreateTransferTransactionCommand(1, 2, 100m, (decimal?)rate, Now, null);

        var result = validator.Validate(command);

        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTransferTransactionCommand.ExchangeRate));
        }
    }

    [Fact]
    public void Transfer_RejectsSameSourceAndDestination()
    {
        var validator = new CreateTransferTransactionCommandValidator(CreateClock());

        var result = validator.Validate(new CreateTransferTransactionCommand(3, 3, 100m, null, Now, null));

        result.IsValid.Should().BeFalse();
    }

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);
        return clock;
    }
}
