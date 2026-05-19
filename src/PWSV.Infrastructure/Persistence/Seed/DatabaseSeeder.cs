using Microsoft.EntityFrameworkCore;
using PWSV.Domain.Entities;

namespace PWSV.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        SeedAccountTypes(modelBuilder);
        SeedCurrencies(modelBuilder);
    }

    private static void SeedAccountTypes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountType>().HasData(
            new AccountType
            {
                Id = 1,
                Code = "CASH",
                DisplayName = "Готівка"
            },
            new AccountType
            {
                Id = 2,
                Code = "BANK_CARD",
                DisplayName = "Банківська картка"
            },
            new AccountType
            {
                Id = 3,
                Code = "CRYPTO",
                DisplayName = "Криптогаманець"
            },
            new AccountType
            {
                Id = 4,
                Code = "OTHER",
                DisplayName = "Інше"
            });
    }

    private static void SeedCurrencies(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Currency>().HasData(
            new Currency
            {
                Id = 1,
                Code = "UAH",
                Name = "Українська гривня",
                Symbol = "₴",
                DecimalPlaces = 2
            },
            new Currency
            {
                Id = 2,
                Code = "USD",
                Name = "Долар США",
                Symbol = "$",
                DecimalPlaces = 2
            },
            new Currency
            {
                Id = 3,
                Code = "EUR",
                Name = "Євро",
                Symbol = "€",
                DecimalPlaces = 2
            },
            new Currency
            {
                Id = 4,
                Code = "PLN",
                Name = "Польський злотий",
                Symbol = "zł",
                DecimalPlaces = 2
            },
            new Currency
            {
                Id = 5,
                Code = "GBP",
                Name = "Фунт стерлінгів",
                Symbol = "£",
                DecimalPlaces = 2
            },
            new Currency
            {
                Id = 6,
                Code = "BTC",
                Name = "Bitcoin",
                Symbol = "₿",
                DecimalPlaces = 8
            },
            new Currency
            {
                Id = 7,
                Code = "ETH",
                Name = "Ethereum",
                Symbol = "Ξ",
                DecimalPlaces = 8
            },
            new Currency
            {
                Id = 8,
                Code = "USDT",
                Name = "Tether",
                Symbol = "₮",
                DecimalPlaces = 6
            });
    }
}
