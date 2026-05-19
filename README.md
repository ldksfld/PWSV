# Personal Wealth & Security Vault (PWSV)

Це навчальний проєкт - локальна інформаційна система для обліку особистих фінансів. Реалізовано перший етап ("Transactional Core") - ядро системи з базою даних, серверним API та десктопним клієнтом. Все працює локально на одному ПК, без жодних звернень до інтернету. Основний фокус - чистота моделі даних (3NF), цілісність на рівні СУБД (constraints, тригери), і базова безпека (хешування паролів, шифрування чутливих полів).

## Що вміє система

Облік фінансів:

- Створення і ведення кількох рахунків (готівка, банківські картки, криптогаманці, інше)
- Підтримка різних валют: UAH, USD, EUR, PLN, GBP, BTC, ETH, USDT (з можливістю додавання своїх)
- Внесення курсів валют вручну з прив'язкою до дати
- Ієрархічний довідник категорій доходів і витрат (необмежена вкладеність)
- Транзакції трьох типів: дохід (Income), витрата (Expense), переказ між рахунками (Transfer, у тому числі між валютами)
- Автоматичне оновлення балансу рахунку через тригери СУБД при будь-яких змінах транзакцій
- Захист від видалення рахунку, по якому існують транзакції (тригер `INSTEAD OF DELETE`)
- Фільтрація і пагінація транзакцій за датою, рахунком, категорією, типом
- Деталі рахунку з історією останніх 50 транзакцій
- Початковий залишок при створенні рахунку оформлюється окремою службовою транзакцією (категорія "Початковий залишок")
- Аудит-журнал (AuditEntries) автоматично фіксує INSERT/UPDATE/DELETE для основних сутностей

Безпека:

- Реєстрація і вхід через логін та пароль (одного користувача на інсталяцію)
- Хешування паролів алгоритмом BCrypt (work factor = 12)
- Шифрування номерів рахунків і описів транзакцій (AES-256-GCM)
- Ключ шифрування виводиться з мастер-паролю користувача через PBKDF2-SHA256 (100 000 ітерацій, унікальна сіль на користувача)
- JWT-авторизація між клієнтом і API (HS256, термін дії 8 годин)
- Rate limiting на ендпоінт логіну (3 спроби / хвилину з IP)
- Серверний журнал з маскуванням чутливих полів (паролі, токени, описи)
- Кореляційний ID на кожен HTTP-запит
- Перевірка приналежності ресурсу користувачу на кожній операції
- Локалізація українською мовою (uk-UA)

Що є в проєкті:

- Серверна частина на ASP.NET Core Web API (.NET 10, C# 14)
- Клієнтська частина на WPF (MVVM, CommunityToolkit.Mvvm)
- База даних SQL Server Express LocalDB
- Entity Framework Core 10 для роботи з БД
- Тригери на рівні БД для автоматичного перерахунку балансів
- Чиста архітектура (Domain / Application / Infrastructure / Presentation)
- CQRS через MediatR з pipeline behaviors (логування, валідація, unit-of-work для транзакційних команд)
- FluentValidation на боці Application
- Mapster для маппінгу Entity → DTO
- Serilog зі структурованим логуванням у файли
- Swagger / OpenAPI для документації API
- Юніт-тести (xUnit + FluentAssertions) та інтеграційні тести (WebApplicationFactory + реальний LocalDB)

## Що потрібно встановити

- Windows 10 (x64) або новіше
- Visual Studio 2026 Community з робочими навантаженнями:
  - ".NET desktop development"
  - "ASP.NET and web development"
  - "Data storage and processing" (для LocalDB)
- .NET 10 SDK (входить у поставку VS 2026)
- SQL Server Express LocalDB (зазвичай встановлюється разом з VS)

Ніяких додаткових пакетів встановлювати руками не потрібно - усе підтягне NuGet при першому білді.

## Як запустити локально

### 1. Завантажити репозиторій з GitHub

```
git clone https://github.com/ldksfld/PWSV.git
cd PWSV
```

### 2. Переконатись, що LocalDB живий

У командному рядку:

```
sqllocaldb info
```

У списку має бути `MSSQLLocalDB`. Якщо немає - створити та запустити:

```
sqllocaldb create "MSSQLLocalDB"
sqllocaldb start "MSSQLLocalDB"
```

Перевірка статусу:

```
sqllocaldb info "MSSQLLocalDB"
```

У виводі має бути `State: Running`.

### 3. Прийняти dev-сертифікат HTTPS

Потрібно тільки якщо це перший .NET-проєкт на цій машині:

```
dotnet dev-certs https --trust
```

### 4. Відновити NuGet-пакети та зібрати рішення (Build)

З кореня репозиторію:

```
dotnet restore PWSV.slnx
dotnet build PWSV.slnx -c Debug
```

Або через Visual Studio:

1. Відкрити файл `PWSV.slnx` у Visual Studio 2026.
2. Дочекатись відновлення NuGet-пакетів (це може зайняти хвилину-дві).
3. `Build > Build Solution` (`Ctrl+Shift+B`). Збірка має пройти без помилок та попереджень (проєкт зібраний з `TreatWarningsAsErrors=true`).

### 5. Налаштувати секрет JWT

Окремо створювати нічого не треба - при першому запуску `PWSV.Api` секрет згенерується автоматично у файл `src/PWSV.Api/secrets.json` (48 випадкових байт, base64). Цей файл виключений з репозиторію через `.gitignore` і використовується тільки локально.

Якщо хочеться задати свій секрет (наприклад, щоб JWT-токени переживали перезапуск, або щоб задати конкретне значення між машинами) - створити вручну файл `src/PWSV.Api/secrets.json`:

```json
{
  "Jwt": {
    "SecretKey": "тут_довгий_випадковий_рядок_не_менше_32_байтів"
  }
}
```

Альтернативно - через User Secrets (значення з User Secrets перекриває `secrets.json`):

```
dotnet user-secrets init --project src/PWSV.Api
dotnet user-secrets set "Jwt:SecretKey" "тут_довгий_випадковий_рядок_не_менше_32_байтів" --project src/PWSV.Api
```

### 6. База даних

Окремо запускати міграції не обов'язково - при першому запуску `PWSV.Api` буде виконано:

- створення файлів БД у каталозі `%LOCALAPPDATA%\PWSV\PWSV.mdf` (та `PWSV_log.ldf`),
- застосування всіх міграцій EF Core (схема, чек-констрейнти, індекси, тригери на `Transactions` та `Accounts`),
- наповнення довідників `AccountTypes` та `Currencies` базовими записами.

Якщо все ж потрібно прогнати міграції до запуску (наприклад, з командного рядка), спочатку поставити EF Core CLI як глобальний інструмент (якщо ще не стояв), потім виконати оновлення:

```
dotnet tool install --global dotnet-ef
dotnet ef database update --project src/PWSV.Infrastructure --startup-project src/PWSV.Api
```

Або з `Package Manager Console` у Visual Studio (`Tools > NuGet Package Manager > Package Manager Console`):

```
Update-Database -Project PWSV.Infrastructure -StartupProject PWSV.Api
```

Перевірити, що тригери дійсно створились - через `View > SQL Server Object Explorer` у Visual Studio, підключитись до `(localdb)\MSSQLLocalDB`, відкрити базу `PWSV`. У таблиці `dbo.Transactions` мають бути тригери:

- `trg_Transactions_AfterInsert`
- `trg_Transactions_AfterUpdate`
- `trg_Transactions_AfterDelete`

А в таблиці `dbo.Accounts`:

- `trg_Accounts_PreventDeleteWhenHasTransactions`

Якщо потрібно скинути базу повністю: закрити Visual Studio, виконати:

```
sqllocaldb stop "MSSQLLocalDB"
```

видалити каталог `%LOCALAPPDATA%\PWSV` повністю, після чого знову запустити API - база створиться з нуля.

### 7. Запустити API та клієнт

У Visual Studio:

1. ПКМ на рішенні `PWSV` → `Configure Startup Projects`.
2. Обрати `Multiple startup projects`.
3. Поставити Action = `Start` для `PWSV.Api` та `PWSV.Client`.
4. OK, потім `F5`.

Або з командного рядка у двох окремих терміналах (важливо: для API явно вказати профіль `https`, бо профіль `http` за замовчуванням слухає тільки порт 5000, а клієнт ходить на `https://localhost:5001`):

```
dotnet run --project src/PWSV.Api --launch-profile https
dotnet run --project src/PWSV.Client
```

Спочатку запуститься API на `https://localhost:5001` (Swagger - на `https://localhost:5001/swagger`), потім відкриється вікно WPF-клієнта.

### 8. Реєстрація першого користувача

У вікні клієнта зареєструвати користувача (логін + пароль + мастер-пароль). Вимоги:

- Логін: 3-64 символи.
- Пароль: 8-128 символів, обов'язково мала літера, велика літера, цифра.
- Мастер-пароль: не коротше 8 символів. Використовується для виведення AES-ключа і запитується при кожному вході - якщо втратити, зашифровані поля (номери рахунків, описи) розшифрувати буде неможливо.

Після реєстрації автоматично відбудеться вхід, і відкриється головне вікно. Реєстрація доступна тільки один раз - система розрахована на одного користувача на інсталяцію.
