using FluentAssertions;
using PWSV.Api.Demo;
using PWSV.Domain.Enums;

namespace PWSV.Api.IntegrationTests;

public sealed class DemoDataPlanTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeZoneInfo Kyiv = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");

    [Fact]
    public void Build_ForSameMoment_IsDeterministic()
    {
        var first = DemoDataPlanBuilder.Build(Now, Kyiv);
        var second = DemoDataPlanBuilder.Build(Now, Kyiv);

        second.Transactions.Should().Equal(first.Transactions);
        second.ExchangeRates.Should().Equal(first.ExchangeRates);
        second.ExpectedBalances.Should().Equal(first.ExpectedBalances);
    }

    [Fact]
    public void Build_PlansOnlyPastOperationsWithinHistoryWindowInChronologicalOrder()
    {
        var plan = DemoDataPlanBuilder.Build(Now, Kyiv);
        var start = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(Now, Kyiv)).AddMonths(-DemoDataPlanBuilder.HistoryMonths);

        plan.Transactions.Should().HaveCountGreaterThan(150);
        plan.Transactions.Select(t => t.OccurredAt).Should().BeInAscendingOrder()
            .And.OnlyContain(t => t.Kind == DateTimeKind.Utc && t <= Now.AddMinutes(-1));
        plan.Transactions.Min(t => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(t.OccurredAt, Kyiv))).Should().Be(start);
        plan.Transactions.OfType<DemoIncome>().Count(i => i.CategoryName == DemoDataPlanBuilder.OpeningBalanceCategory).Should().Be(7);
    }

    [Fact]
    public void Build_UsesOnlyActivePlannedAccountsAndCategoriesOfMatchingKind()
    {
        var plan = DemoDataPlanBuilder.Build(Now, Kyiv);
        var activeAccounts = plan.Accounts.Where(a => a.IsActive).Select(a => a.Key).ToHashSet();
        var categories = plan.Categories.ToDictionary(c => c.Name);

        foreach (var income in plan.Transactions.OfType<DemoIncome>())
        {
            activeAccounts.Should().Contain(income.AccountKey);
            categories[income.CategoryName].Should().Match<DemoCategory>(c => c.Kind == CategoryKind.Income && c.IsActive);
        }

        foreach (var expense in plan.Transactions.OfType<DemoExpense>())
        {
            activeAccounts.Should().Contain(expense.AccountKey);
            categories[expense.CategoryName].Should().Match<DemoCategory>(c => c.Kind == CategoryKind.Expense && c.IsActive);
        }

        foreach (var transfer in plan.Transactions.OfType<DemoTransfer>())
        {
            activeAccounts.Should().Contain([transfer.SourceAccountKey, transfer.DestinationAccountKey]);
            transfer.SourceAccountKey.Should().NotBe(transfer.DestinationAccountKey);
        }
    }

    [Fact]
    public void Build_SetsExchangeRateOnlyForCrossCurrencyTransfersUsingRateValidOnThatDay()
    {
        var plan = DemoDataPlanBuilder.Build(Now, Kyiv);
        var currencies = plan.Accounts.ToDictionary(a => a.Key, a => a.CurrencyCode);
        var transfers = plan.Transactions.OfType<DemoTransfer>().ToList();

        transfers.Should().Contain(t => t.ExchangeRate != null).And.Contain(t => t.ExchangeRate == null);
        foreach (var transfer in transfers)
        {
            var source = currencies[transfer.SourceAccountKey];
            var destination = currencies[transfer.DestinationAccountKey];
            if (source == destination)
            {
                transfer.ExchangeRate.Should().BeNull();
                continue;
            }

            var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(transfer.OccurredAt, Kyiv));
            var direct = plan.ExchangeRates.Where(r => r.BaseCurrencyCode == source && r.QuoteCurrencyCode == destination && r.EffectiveDate <= day)
                .MaxBy(r => r.EffectiveDate);
            var inverse = plan.ExchangeRates.Where(r => r.BaseCurrencyCode == destination && r.QuoteCurrencyCode == source && r.EffectiveDate <= day)
                .MaxBy(r => r.EffectiveDate);
            var expected = direct?.Rate ?? Math.Round(1m / inverse!.Rate, 8, MidpointRounding.AwayFromZero);
            transfer.ExchangeRate.Should().Be(expected);
        }
    }

    [Fact]
    public void Build_ExpectedBalancesFollowTransactionsAndNeverGoNegative()
    {
        var plan = DemoDataPlanBuilder.Build(Now, Kyiv);
        var balances = plan.Accounts.ToDictionary(a => a.Key, _ => 0m);

        foreach (var transaction in plan.Transactions)
        {
            switch (transaction)
            {
                case DemoIncome income:
                    balances[income.AccountKey] += income.Amount;
                    break;
                case DemoExpense expense:
                    balances[expense.AccountKey] -= expense.Amount;
                    break;
                case DemoTransfer transfer:
                    balances[transfer.SourceAccountKey] -= transfer.Amount;
                    balances[transfer.DestinationAccountKey] += transfer.ExchangeRate is { } rate
                        ? decimal.Round(transfer.Amount * rate, 2, MidpointRounding.AwayFromZero)
                        : transfer.Amount;
                    break;
            }

            balances.Values.Should().OnlyContain(balance => balance >= 0m);
        }

        balances.Should().Equal(plan.ExpectedBalances);
        plan.ExpectedBalances["oldCard"].Should().Be(0m);
    }

    [Fact]
    public void Build_AmountsFitStoragePrecisionAndTextsFitValidationLimits()
    {
        var plan = DemoDataPlanBuilder.Build(Now, Kyiv);

        plan.Transactions.Select(Amount).Should().OnlyContain(a => a > 0m && decimal.Round(a, 2) == a);
        plan.Transactions.Select(t => t.Description).Should().OnlyContain(d => d == null || d.Length <= 512);
        plan.Transactions.OfType<DemoExpense>().Select(e => e.Counterparty).Should().OnlyContain(c => c == null || c.Length <= 256);
        plan.Transactions.Should().Contain(t => t.Description == null).And.Contain(t => t.Description != null);
    }

    [Fact]
    public void Build_ExchangeRatesAreUniquePositiveAndNeverInFuture()
    {
        var plan = DemoDataPlanBuilder.Build(Now, Kyiv);

        plan.ExchangeRates.Should().OnlyContain(r => r.Rate > 0m && decimal.Round(r.Rate, 4) == r.Rate && r.EffectiveDate <= DateOnly.FromDateTime(Now));
        plan.ExchangeRates.Select(r => (r.BaseCurrencyCode, r.QuoteCurrencyCode, r.EffectiveDate)).Should().OnlyHaveUniqueItems();
        plan.ExchangeRates.Select(r => r.EffectiveDate).Max().Should().Be(DateOnly.FromDateTime(Now));
    }

    [Fact]
    public void Build_DeclaresParentCategoriesBeforeChildrenWithUniqueNames()
    {
        var plan = DemoDataPlanBuilder.Build(Now, Kyiv);
        var declared = new HashSet<string>();

        foreach (var category in plan.Categories)
        {
            if (category.ParentName is { } parent)
            {
                declared.Should().Contain(parent);
                plan.Categories.Single(c => c.Name == parent).Kind.Should().Be(category.Kind);
            }

            declared.Add(category.Name).Should().BeTrue();
        }

        plan.Categories.Should().Contain(c => !c.IsActive);
        plan.Accounts.Should().ContainSingle(a => !a.IsActive);
    }

    [Fact]
    public void Build_UsesOnlyFictionalAccountNumbers()
    {
        var plan = DemoDataPlanBuilder.Build(Now, Kyiv);

        plan.Accounts.Select(a => a.AccountNumber).OfType<string>()
            .Should().NotBeEmpty()
            .And.OnlyContain(number => number.StartsWith("UA00 ", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("UTC", "2026-11-02T12:00:00Z")]
    [InlineData("FLE Standard Time", "2026-03-29T09:00:00Z")]
    [InlineData("FLE Standard Time", "2026-10-25T09:00:00Z")]
    [InlineData("FLE Standard Time", "2026-11-01T22:30:00Z")]
    [InlineData("Pacific Standard Time", "2026-11-01T18:00:00Z")]
    [InlineData("Tokyo Standard Time", "2026-11-01T16:00:00Z")]
    public void Build_InAnyTimeZone_ProducesValidPastTimeline(string timeZoneId, string utcNow)
    {
        var now = DateTime.Parse(utcNow, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        var plan = DemoDataPlanBuilder.Build(now, zone);

        plan.Transactions.Should().HaveCountGreaterThan(150);
        plan.Transactions.Select(t => t.OccurredAt).Should().BeInAscendingOrder()
            .And.OnlyContain(t => t <= now.AddMinutes(-1));
        plan.ExchangeRates.Should().OnlyContain(r => r.EffectiveDate <= DateOnly.FromDateTime(now));
        plan.ExpectedBalances.Values.Should().OnlyContain(balance => balance >= 0m);
    }

    private static decimal Amount(DemoTransaction transaction) => transaction switch
    {
        DemoIncome income => income.Amount,
        DemoExpense expense => expense.Amount,
        DemoTransfer transfer => transfer.Amount,
        _ => 0m
    };
}
