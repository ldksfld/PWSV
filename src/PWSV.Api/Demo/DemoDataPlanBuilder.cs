using System.Globalization;
using PWSV.Domain.Enums;

namespace PWSV.Api.Demo;

public sealed class DemoDataPlanBuilder
{
    public const int HistoryMonths = 3;
    public const string OpeningBalanceCategory = "Початковий залишок";

    private const int RandomSeed = 2026;

    private static readonly DemoAccount[] AccountDefinitions =
    [
        new("cash", "Готівка", "CASH", "UAH", null),
        new("salary", "Зарплатна картка", "BANK_CARD", "UAH", "UA00 0000 0000 0000 0262 0000 0000 1"),
        new("daily", "Картка для щоденних витрат", "BANK_CARD", "UAH", "UA00 0000 0000 0000 0262 0000 0000 2"),
        new("deposit", "Депозит «Накопичувальний»", "OTHER", "UAH", "UA00 0000 0000 0000 0263 0000 0000 3"),
        new("usd", "Валютний рахунок", "BANK_CARD", "USD", "UA00 0000 0000 0000 0262 0000 0000 4"),
        new("cashUsd", "Готівка (USD)", "CASH", "USD", null),
        new("crypto", "Криптогаманець", "CRYPTO", "USDT", null),
        new("oldCard", "Стара картка", "BANK_CARD", "UAH", "UA00 0000 0000 0000 0262 0000 0000 5", IsActive: false)
    ];

    private static readonly DemoCategory[] CategoryDefinitions =
    [
        new(OpeningBalanceCategory, CategoryKind.Income),
        new("Зарплата", CategoryKind.Income),
        new("Основна зарплата", CategoryKind.Income, "Зарплата"),
        new("Премія", CategoryKind.Income, "Зарплата"),
        new("Фриланс", CategoryKind.Income),
        new("Пасивний дохід", CategoryKind.Income),
        new("Відсотки за депозитом", CategoryKind.Income, "Пасивний дохід"),
        new("Кешбек", CategoryKind.Income, "Пасивний дохід"),
        new("Подарунки отримані", CategoryKind.Income),
        new("Продукти", CategoryKind.Expense),
        new("Супермаркети", CategoryKind.Expense, "Продукти"),
        new("Ринок", CategoryKind.Expense, "Продукти"),
        new("Пекарня", CategoryKind.Expense, "Продукти"),
        new("Житло", CategoryKind.Expense),
        new("Оренда", CategoryKind.Expense, "Житло"),
        new("Комунальні послуги", CategoryKind.Expense, "Житло"),
        new("Електроенергія", CategoryKind.Expense, "Комунальні послуги"),
        new("Газ", CategoryKind.Expense, "Комунальні послуги"),
        new("Вода", CategoryKind.Expense, "Комунальні послуги"),
        new("Інтернет і зв'язок", CategoryKind.Expense, "Житло"),
        new("Транспорт", CategoryKind.Expense),
        new("Пальне", CategoryKind.Expense, "Транспорт"),
        new("Таксі", CategoryKind.Expense, "Транспорт"),
        new("Громадський транспорт", CategoryKind.Expense, "Транспорт"),
        new("Здоров'я", CategoryKind.Expense),
        new("Аптека", CategoryKind.Expense, "Здоров'я"),
        new("Спортзал", CategoryKind.Expense, "Здоров'я"),
        new("Розваги", CategoryKind.Expense),
        new("Кафе та ресторани", CategoryKind.Expense, "Розваги"),
        new("Кіно і театр", CategoryKind.Expense, "Розваги"),
        new("Підписки", CategoryKind.Expense, "Розваги"),
        new("Одяг і взуття", CategoryKind.Expense),
        new("Освіта", CategoryKind.Expense),
        new("Благодійність", CategoryKind.Expense),
        new("Подарунки", CategoryKind.Expense),
        new("Хобі", CategoryKind.Expense, IsActive: false)
    ];

    private static readonly (string Account, decimal Amount)[] OpeningBalances =
    [
        ("cash", 4_200m),
        ("salary", 38_500m),
        ("daily", 6_800m),
        ("deposit", 120_000m),
        ("usd", 1_850m),
        ("cashUsd", 600m),
        ("crypto", 650m)
    ];

    private static readonly (string Base, string Quote, double Start, double Volatility)[] RatePairs =
    [
        ("USD", "UAH", 41.30, 0.004),
        ("EUR", "UAH", 48.10, 0.006),
        ("USDT", "UAH", 41.25, 0.004)
    ];

    private static readonly string[] CashOrDaily = ["cash", "daily"];
    private static readonly string[] Supermarkets = ["Супермаркет «Колосок»", "Маркет «Щодня»", "Гіпермаркет «Злагода»", "Мінімаркет «Біля дому»"];
    private static readonly string[] Cafes = ["Кав'ярня «Зерно»", "Ресторан «Смак»", "Піцерія «Скибка»", "Кафе «Затишок»"];
    private static readonly string[] FuelStations = ["АЗС «Шлях»", "АЗС «Магістраль»"];
    private static readonly string[] FreelanceClients = ["Клієнт з Польщі", "Клієнт з Німеччини", "Клієнт з Канади"];
    private static readonly string?[] GroceryNotes = ["Продукти на тиждень", "Закупівля продуктів", "Продукти та побутова хімія", null];
    private static readonly string[] MarketNotes = ["Овочі та фрукти", "Сир і молочні продукти", "М'ясо"];
    private static readonly string?[] BakeryNotes = ["Хліб і випічка", "Круасани до кави", null];
    private static readonly string[] CafeNotes = ["Обід з колегами", "Вечеря з друзями", "Кава з десертом", "Піца на вечерю"];
    private static readonly string?[] TaxiNotes = ["Поїздка додому", "До офісу", "З вокзалу", null];
    private static readonly string[] PharmacyNotes = ["Вітаміни", "Ліки від застуди", "Аптечка додому"];
    private static readonly string[] ClothesNotes = ["Куртка", "Кросівки", "Сезонний одяг"];
    private static readonly decimal[] UsdSaleAmounts = [400m, 500m, 600m, 800m];
    private static readonly decimal[] UsdPurchaseAmounts = [8_000m, 10_000m];
    private static readonly decimal[] DonationAmounts = [1_000m, 1_500m, 2_000m, 2_500m];

    private readonly Random _random = new(RandomSeed);
    private readonly TimeZoneInfo _timeZone;
    private readonly DateTime _latestAllowedUtc;
    private readonly DateOnly _today;
    private readonly DateOnly _startDate;
    private readonly Dictionary<string, decimal> _balances = AccountDefinitions.ToDictionary(a => a.Key, _ => 0m);
    private readonly Dictionary<string, string> _currencies = AccountDefinitions.ToDictionary(a => a.Key, a => a.CurrencyCode);
    private readonly List<DemoExchangeRate> _rates = [];
    private readonly List<DemoTransaction> _transactions = [];
    private readonly List<(DateTime Local, Action<DateTime> Apply)> _dayEvents = [];

    private DemoDataPlanBuilder(DateTime utcNow, TimeZoneInfo timeZone)
    {
        var now = utcNow.Kind switch
        {
            DateTimeKind.Utc => utcNow,
            DateTimeKind.Local => utcNow.ToUniversalTime(),
            _ => DateTime.SpecifyKind(utcNow, DateTimeKind.Utc)
        };

        _timeZone = timeZone;
        _latestAllowedUtc = now.AddMinutes(-1);
        _today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(_latestAllowedUtc, timeZone));
        _startDate = _today.AddMonths(-HistoryMonths);
    }

    public static DemoDataPlan Build(DateTime utcNow, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);

        var builder = new DemoDataPlanBuilder(utcNow, timeZone);
        builder.PlanExchangeRates();
        builder.PlanOpeningBalances();
        for (var day = builder._startDate.AddDays(1); day <= builder._today; day = day.AddDays(1))
        {
            builder.PlanDay(day);
        }

        return new DemoDataPlan(
            CategoryDefinitions,
            AccountDefinitions,
            builder._rates,
            builder._transactions,
            builder._balances);
    }

    private void PlanExchangeRates()
    {
        var latestDate = DateOnly.FromDateTime(_latestAllowedUtc);
        var dates = new List<DateOnly>();
        for (var date = _startDate; date <= latestDate; date = date.AddDays(7))
        {
            dates.Add(date);
        }

        if (dates.Count == 0 || dates[^1] != latestDate)
        {
            dates.Add(latestDate);
        }

        var levels = RatePairs.Select(pair => pair.Start).ToArray();
        foreach (var date in dates)
        {
            for (var i = 0; i < RatePairs.Length; i++)
            {
                levels[i] *= 1 + (((_random.NextDouble() * 2) - 1) * RatePairs[i].Volatility);
                var rate = Math.Round((decimal)levels[i], 4, MidpointRounding.AwayFromZero);
                _rates.Add(new DemoExchangeRate(RatePairs[i].Base, RatePairs[i].Quote, rate, date));
            }
        }
    }

    private void PlanOpeningBalances()
    {
        for (var i = 0; i < OpeningBalances.Length; i++)
        {
            var (account, amount) = OpeningBalances[i];
            _dayEvents.Add((_startDate.ToDateTime(new TimeOnly(9, i)), at =>
                Income(at, account, amount, OpeningBalanceCategory, "Залишок на початок обліку", null)));
        }

        RunDayEvents();
    }

    private void PlanDay(DateOnly day)
    {
        var weekday = day.DayOfWeek;
        var heatingSeason = day.Month is >= 11 or <= 3;

        if (weekday is DayOfWeek.Tuesday or DayOfWeek.Friday)
        {
            At(day, 7, 10, at => Expense(at, Pick(CashOrDaily), Between(60, 190), "Пекарня", Pick(BakeryNotes), "Пекарня «Хлібна хата»"));
        }

        if (day.Day == 1)
        {
            At(day, 9, 12, at => Expense(at, "salary", 14_000m, "Оренда", $"Оренда квартири за {FormatMonth(day)}", "Орендодавець"));
        }

        if (day.Day == 3)
        {
            At(day, 18, 20, at => Expense(at, "daily", 950m, "Спортзал", "Абонемент у спортзал", "Фітнес-клуб «Енергія»"));
        }

        if (day.Day == 5)
        {
            At(day, 10, 12, at => Income(at, "salary", 64_500m, "Основна зарплата", $"Заробітна плата за {FormatMonth(day.AddMonths(-1))}", "ТОВ «Демо Софт»"));
        }

        if (day.Day == 6)
        {
            At(day, 8, 10, at => Expense(at, "daily", 299m, "Підписки", "Стрімінговий сервіс", "Онлайн-кінотеатр"));
            At(day, 8, 10, at => Expense(at, "daily", 129m, "Підписки", "Музичний сервіс", null));
        }

        if (day.Day is 7 or 21)
        {
            At(day, 12, 14, at => Transfer(at, "salary", "daily", 15_000m, "Поповнення картки для витрат"));
        }

        if (day.Day is 8 or 23)
        {
            At(day, 13, 15, at => Transfer(at, "salary", "cash", 4_000m, "Зняття готівки в банкоматі"));
        }

        if (day.Day == 10)
        {
            At(day, 8, 9, at => Expense(at, "daily", 200m, "Громадський транспорт", "Поповнення транспортної картки", null));
            At(day, 19, 21, at => Expense(at, "daily", heatingSeason ? Between(1_200, 1_650) : Between(650, 900), "Електроенергія", "Оплата електроенергії", "Енергопостачальна компанія"));
            At(day, 19, 21, at => Expense(at, "daily", heatingSeason ? Between(1_900, 2_700) : Between(240, 420), "Газ", "Оплата газопостачання", "Газопостачальна компанія"));
            At(day, 19, 21, at => Expense(at, "daily", Between(280, 430), "Вода", "Водопостачання та водовідведення", "Водоканал"));
            At(day, 19, 21, at => Expense(at, "daily", 330m, "Інтернет і зв'язок", "Домашній інтернет", "Інтернет-провайдер"));
            At(day, 19, 21, at => Expense(at, "daily", 250m, "Інтернет і зв'язок", "Мобільний зв'язок", "Мобільний оператор"));
        }

        if (day.Day == 12)
        {
            At(day, 9, 10, at => Income(at, "deposit", _balances["deposit"] * 0.012m, "Відсотки за депозитом", "Нарахування відсотків", "Банк"));
        }

        if (day.Day == 15)
        {
            At(day, 15, 18, at => Income(at, "usd", Between(700, 1_500), "Фриланс", "Оплата за розробку вебзастосунку", Pick(FreelanceClients)));
        }

        if (day.Day == 16)
        {
            At(day, 11, 13, at => Transfer(at, "usd", "salary", Pick(UsdSaleAmounts), "Продаж валюти"));
        }

        if (day.Day == 18)
        {
            At(day, 14, 16, at => Transfer(at, "salary", "cashUsd", Pick(UsdPurchaseAmounts), "Купівля доларів"));
        }

        if (day.Day == 20)
        {
            At(day, 20, 22, at => Expense(at, "daily", Pick(DonationAmounts), "Благодійність", "Щомісячний донат", "Благодійний фонд «Разом»"));
            if (day.Month % 3 == 0)
            {
                At(day, 11, 12, at => Income(at, "salary", 15_000m, "Премія", "Квартальна премія", "ТОВ «Демо Софт»"));
            }
        }

        if (day.Day == 22)
        {
            At(day, 21, 22, at => Transfer(at, "salary", "crypto", 4_000m, "Купівля USDT"));
        }

        if (day.Day == 25)
        {
            At(day, 10, 11, at => Income(at, "daily", Between(150, 460), "Кешбек", "Кешбек за покупки", "Банк"));
        }

        if (day.Day == 26)
        {
            At(day, 12, 13, at => Transfer(at, "salary", "deposit", 10_000m, "Поповнення депозиту"));
        }

        if (day == _startDate.AddDays(30))
        {
            At(day, 19, 21, at => Expense(at, "daily", 3_200m, "Освіта", "Онлайн-курс англійської мови", "Мовна школа «Лінгва»"));
        }

        if (day == _startDate.AddDays(45))
        {
            At(day, 17, 19, at => Expense(at, "daily", 1_500m, "Подарунки", "Подарунок другові на день народження", null));
        }

        if (day == _startDate.AddDays(60))
        {
            At(day, 15, 17, at => Income(at, "cash", 2_000m, "Подарунки отримані", "Подарунок від батьків", null));
        }

        if (weekday is DayOfWeek.Monday or DayOfWeek.Thursday or DayOfWeek.Saturday)
        {
            At(day, 17, 21, at => Expense(at, Chance(0.7) ? "daily" : "cash", Between(350, 1_900), "Супермаркети", Pick(GroceryNotes), Pick(Supermarkets)));
        }

        if (weekday == DayOfWeek.Saturday)
        {
            At(day, 9, 12, at => Expense(at, "cash", Between(200, 650), "Ринок", Pick(MarketNotes), "Центральний ринок"));
        }

        if (day.Day is 4 or 14 or 24)
        {
            At(day, 8, 20, at => Expense(at, "daily", Between(1_600, 2_300), "Пальне", "Заправка", Pick(FuelStations)));
        }

        if (Chance(0.06))
        {
            At(day, 10, 20, at => Expense(at, "daily", Between(120, 900), "Аптека", Pick(PharmacyNotes), "Аптека «Здоров'я»"));
        }

        if (Chance(0.13))
        {
            At(day, 12, 22, at => Expense(at, "daily", Between(450, 2_200), "Кафе та ресторани", Pick(CafeNotes), Pick(Cafes)));
        }

        if (Chance(0.03))
        {
            At(day, 13, 19, at => Expense(at, "daily", Between(1_500, 4_500), "Одяг і взуття", Pick(ClothesNotes), "Магазин «Стиль»"));
        }

        if (Chance(0.03))
        {
            At(day, 18, 21, at => Expense(at, "daily", Between(300, 700), "Кіно і театр", "Квитки", "Кінотеатр «Зоря»"));
        }

        if (Chance(0.16))
        {
            At(day, 8, 22, at => Expense(at, "daily", Between(120, 380), "Таксі", Pick(TaxiNotes), "Служба таксі «Швидко»"));
        }

        RunDayEvents();
    }

    private void At(DateOnly day, int fromHour, int toHour, Action<DateTime> apply)
    {
        var minute = _random.Next(fromHour * 60, toHour * 60);
        _dayEvents.Add((day.ToDateTime(TimeOnly.MinValue).AddMinutes(minute), apply));
    }

    private void RunDayEvents()
    {
        foreach (var (local, apply) in _dayEvents.OrderBy(e => e.Local))
        {
            var valid = _timeZone.IsInvalidTime(local) ? local.AddHours(1) : local;
            var utc = TimeZoneInfo.ConvertTimeToUtc(valid, _timeZone);
            if (utc <= _latestAllowedUtc)
            {
                apply(utc);
            }
        }

        _dayEvents.Clear();
    }

    private void Income(DateTime occurredAt, string account, decimal amount, string category, string? description, string? counterparty)
    {
        var value = Money(amount);
        if (value <= 0m)
        {
            return;
        }

        _transactions.Add(new DemoIncome(account, category, value, occurredAt, description, counterparty));
        _balances[account] += value;
    }

    private void Expense(DateTime occurredAt, string account, decimal amount, string category, string? description, string? counterparty)
    {
        var value = Money(amount);
        if (value <= 0m || _balances[account] < value)
        {
            return;
        }

        _transactions.Add(new DemoExpense(account, category, value, occurredAt, description, counterparty));
        _balances[account] -= value;
    }

    private void Transfer(DateTime occurredAt, string source, string destination, decimal amount, string? description)
    {
        var value = Money(amount);
        if (value <= 0m || _balances[source] < value)
        {
            return;
        }

        var sourceCurrency = _currencies[source];
        var destinationCurrency = _currencies[destination];
        decimal? rate = sourceCurrency == destinationCurrency
            ? null
            : RateOn(sourceCurrency, destinationCurrency, LocalDate(occurredAt));
        var credited = rate is { } conversion ? Money(value * conversion) : value;
        if (credited <= 0m)
        {
            return;
        }

        _transactions.Add(new DemoTransfer(source, destination, value, rate, occurredAt, description));
        _balances[source] -= value;
        _balances[destination] += credited;
    }

    private decimal RateOn(string fromCurrency, string toCurrency, DateOnly date)
    {
        if (LatestRate(fromCurrency, toCurrency, date) is { } direct)
        {
            return direct;
        }

        var inverse = LatestRate(toCurrency, fromCurrency, date)
            ?? throw new InvalidOperationException($"Demo exchange rate {fromCurrency}/{toCurrency} is not planned for {date:O}.");
        return Math.Round(1m / inverse, 8, MidpointRounding.AwayFromZero);
    }

    private decimal? LatestRate(string baseCurrency, string quoteCurrency, DateOnly date) =>
        _rates
            .Where(r => r.BaseCurrencyCode == baseCurrency && r.QuoteCurrencyCode == quoteCurrency && r.EffectiveDate <= date)
            .OrderByDescending(r => r.EffectiveDate)
            .Select(r => (decimal?)r.Rate)
            .FirstOrDefault();

    private DateOnly LocalDate(DateTime utc) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, _timeZone));

    private decimal Between(double min, double max) => Money((decimal)(min + (_random.NextDouble() * (max - min))));

    private T Pick<T>(IReadOnlyList<T> items) => items[_random.Next(items.Count)];

    private bool Chance(double probability) => _random.NextDouble() < probability;

    private static decimal Money(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    private static string FormatMonth(DateOnly day) => day.ToString("MM.yyyy", CultureInfo.InvariantCulture);
}
