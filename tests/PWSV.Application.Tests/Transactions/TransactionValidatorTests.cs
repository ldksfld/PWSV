using System.Globalization;
using FluentAssertions;
using NSubstitute;
using PWSV.Application.Common.Interfaces;
using PWSV.Application.Transactions.Commands.CreateExpenseTransaction;
using PWSV.Application.Transactions.Commands.CreateTransferTransaction;
using PWSV.Application.Transactions.Commands.UpdateTransaction;
using PWSV.Application.Transactions.Queries.GetTransactionsList;
using PWSV.Domain.Enums;

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

    [Theory]
    [InlineData(1L, null, null, 10.5, true)]
    [InlineData(1L, 3, 7, 0.01, true)]
    [InlineData(0L, null, null, 10.5, false)]
    [InlineData(1L, 0, null, 10.5, false)]
    [InlineData(1L, null, 0, 10.5, false)]
    [InlineData(1L, null, null, 0, false)]
    [InlineData(1L, null, null, 10.555, false)]
    public void Update_ValidatesIdentifiersAndAmount(long id, int? accountId, int? categoryId, decimal amount, bool expectedValid)
    {
        var validator = new UpdateTransactionCommandValidator(CreateClock());

        var result = validator.Validate(new UpdateTransactionCommand(id, accountId, categoryId, amount, Now, null, null));

        result.IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData(512, 256, true)]
    [InlineData(513, 256, false)]
    [InlineData(512, 257, false)]
    public void Update_LimitsDescriptionAndCounterpartyLength(int descriptionLength, int counterpartyLength, bool expectedValid)
    {
        var validator = new UpdateTransactionCommandValidator(CreateClock());
        var command = new UpdateTransactionCommand(1, null, null, 10m, Now, new string('о', descriptionLength), new string('к', counterpartyLength));

        var result = validator.Validate(command);

        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void Expense_WithDateBefore1900_IsInvalid()
    {
        var validator = new CreateExpenseTransactionCommandValidator(CreateClock());

        var result = validator.Validate(new CreateExpenseTransactionCommand(1, 1, 10m, new DateTime(1899, 12, 31, 23, 0, 0, DateTimeKind.Utc), null, null));

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateExpenseTransactionCommand.OccurredAt));
    }

    [Fact]
    public void Expense_WithLocalTimeWithinTolerance_IsValid()
    {
        var validator = new CreateExpenseTransactionCommandValidator(CreateClock());

        var result = validator.Validate(new CreateExpenseTransactionCommand(1, 1, 10m, Now.AddSeconds(30).ToLocalTime(), null, null));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("2026-10-01", "2026-10-31", true)]
    [InlineData("2026-10-01", "2026-10-01", true)]
    [InlineData("2026-10-31", "2026-10-01", false)]
    public void ListQuery_RequiresToNotBeforeFrom(string from, string to, bool expectedValid)
    {
        var validator = new GetTransactionsListQueryValidator();
        var query = new GetTransactionsListQuery(null, null, null, DateTime.Parse(from, CultureInfo.InvariantCulture), DateTime.Parse(to, CultureInfo.InvariantCulture), 1, 10);

        var result = validator.Validate(query);

        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void ListQuery_WithUndefinedEnumValues_IsInvalid()
    {
        var validator = new GetTransactionsListQueryValidator();
        var query = new GetTransactionsListQuery(null, null, (TransactionKind)'Z', null, null, 1, 10, (TransactionSortField)9, (SortDirection)9);

        var result = validator.Validate(query);

        result.Errors.Select(e => e.PropertyName).Should().BeEquivalentTo(
            [nameof(GetTransactionsListQuery.Kind), nameof(GetTransactionsListQuery.SortBy), nameof(GetTransactionsListQuery.SortDirection)]);
    }

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);
        return clock;
    }
}
