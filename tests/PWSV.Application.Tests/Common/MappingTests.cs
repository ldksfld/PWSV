using FluentAssertions;
using Mapster;
using PWSV.Application.Accounts.Dto;
using PWSV.Application.Transactions.Dto;
using PWSV.Domain.Entities;
using PWSV.Domain.Enums;

namespace PWSV.Application.Tests.Common;

public sealed class MappingTests
{
    public MappingTests()
    {
        MappingSetup.EnsureRegistered();
    }

    [Theory]
    [InlineData(TransactionKind.Income, null, TransactionDirection.Incoming)]
    [InlineData(TransactionKind.Expense, null, TransactionDirection.Outgoing)]
    [InlineData(TransactionKind.Transfer, 11L, TransactionDirection.Outgoing)]
    [InlineData(TransactionKind.Transfer, 9L, TransactionDirection.Incoming)]
    public void Transaction_MapsDirectionFromKindAndLink(TransactionKind kind, long? linkedId, TransactionDirection expected)
    {
        var transaction = new Transaction { Id = 10, Kind = kind, LinkedTransactionId = linkedId, Amount = 1m, Account = new Account { Name = "Готівка" } };

        var dto = transaction.Adapt<TransactionDto>();

        dto.Direction.Should().Be(expected);
        dto.AccountName.Should().Be("Готівка");
    }

    [Fact]
    public void Account_MapsCurrencyAndTypeAndNeverCopiesEncryptedNumber()
    {
        var account = new Account
        {
            Name = "BTC",
            AccountType = new AccountType { Code = "CRYPTO", DisplayName = "Криптогаманець" },
            Currency = new Currency { Code = "BTC", Symbol = "₿", DecimalPlaces = 8 },
            AccountNumberCipher = [1, 2, 3]
        };

        var dto = account.Adapt<AccountDto>();

        dto.AccountTypeName.Should().Be("Криптогаманець");
        dto.CurrencyCode.Should().Be("BTC");
        dto.CurrencyDecimalPlaces.Should().Be(8);
        dto.AccountNumber.Should().BeNull();
    }
}
