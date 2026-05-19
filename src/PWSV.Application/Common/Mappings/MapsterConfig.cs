using Mapster;
using PWSV.Application.Accounts.Dto;
using PWSV.Application.Categories.Dto;
using PWSV.Application.Currencies.Dto;
using PWSV.Application.ExchangeRates.Dto;
using PWSV.Application.Transactions.Dto;
using PWSV.Domain.Entities;

namespace PWSV.Application.Common.Mappings;

public static class MapsterConfig
{
    public static void Register()
    {
        TypeAdapterConfig<Account, AccountDto>.NewConfig()
            .Map(dest => dest.AccountTypeCode, src => src.AccountType != null ? src.AccountType.Code : string.Empty)
            .Map(dest => dest.AccountTypeName, src => src.AccountType != null ? src.AccountType.DisplayName : string.Empty)
            .Map(dest => dest.CurrencyCode, src => src.Currency != null ? src.Currency.Code : string.Empty)
            .Map(dest => dest.CurrencySymbol, src => src.Currency != null ? src.Currency.Symbol : string.Empty)
            .Ignore(dest => dest.AccountNumber!);

        TypeAdapterConfig<Category, CategoryDto>.NewConfig();

        TypeAdapterConfig<Currency, CurrencyDto>.NewConfig();

        TypeAdapterConfig<ExchangeRate, ExchangeRateDto>.NewConfig()
            .Map(dest => dest.BaseCurrencyCode, src => src.BaseCurrency != null ? src.BaseCurrency.Code : string.Empty)
            .Map(dest => dest.QuoteCurrencyCode, src => src.QuoteCurrency != null ? src.QuoteCurrency.Code : string.Empty);

        TypeAdapterConfig<Transaction, TransactionDto>.NewConfig()
            .Map(dest => dest.AccountName, src => src.Account != null ? src.Account.Name : string.Empty)
            .Map(dest => dest.CategoryName, src => src.Category != null ? src.Category.Name : null)
            .Ignore(dest => dest.Description!);
    }
}
