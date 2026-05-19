using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PWSV.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    DecimalPlaces = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                    table.CheckConstraint("CK_Currencies_DecimalPlaces", "[DecimalPlaces] BETWEEN 0 AND 8");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(72)", unicode: false, maxLength: 72, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BaseCurrencyId = table.Column<int>(type: "int", nullable: false),
                    QuoteCurrencyId = table.Column<int>(type: "int", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.Id);
                    table.CheckConstraint("CK_ExchangeRates_DifferentCurrencies", "[BaseCurrencyId] <> [QuoteCurrencyId]");
                    table.CheckConstraint("CK_ExchangeRates_Rate", "[Rate] > 0");
                    table.ForeignKey(
                        name: "FK_ExchangeRates_Currencies_BaseCurrencyId",
                        column: x => x.BaseCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExchangeRates_Currencies_QuoteCurrencyId",
                        column: x => x.QuoteCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AccountTypeId = table.Column<int>(type: "int", nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    AccountNumberCipher = table.Column<byte[]>(type: "varbinary(512)", maxLength: 512, nullable: true),
                    AccountNumberNonce = table.Column<byte[]>(type: "varbinary(16)", maxLength: 16, nullable: true),
                    AccountNumberTag = table.Column<byte[]>(type: "varbinary(16)", maxLength: 16, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accounts_AccountTypes_AccountTypeId",
                        column: x => x.AccountTypeId,
                        principalTable: "AccountTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Accounts_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Accounts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    EntityName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Action = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Kind = table.Column<string>(type: "char(1)", nullable: false),
                    ParentCategoryId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.CheckConstraint("CK_Categories_Kind", "[Kind] IN ('I','E')");
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Categories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KeyDerivationConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Salt = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: false),
                    Iterations = table.Column<int>(type: "int", nullable: false, defaultValue: 100000),
                    KeyLength = table.Column<int>(type: "int", nullable: false, defaultValue: 32)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyDerivationConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KeyDerivationConfigs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    Kind = table.Column<string>(type: "char(1)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    DescriptionCipher = table.Column<byte[]>(type: "varbinary(2048)", maxLength: 2048, nullable: true),
                    DescriptionNonce = table.Column<byte[]>(type: "varbinary(16)", maxLength: 16, nullable: true),
                    DescriptionTag = table.Column<byte[]>(type: "varbinary(16)", maxLength: 16, nullable: true),
                    Counterparty = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LinkedTransactionId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.CheckConstraint("CK_Transactions_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_Transactions_Kind", "[Kind] IN ('I','E','T')");
                    table.CheckConstraint("CK_Transactions_TransferShape", "([Kind] = 'T' AND [CategoryId] IS NULL) OR ([Kind] IN ('I','E') AND [CategoryId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Transactions_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Transactions_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Transactions_Transactions_LinkedTransactionId",
                        column: x => x.LinkedTransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "AccountTypes",
                columns: new[] { "Id", "Code", "DisplayName" },
                values: new object[,]
                {
                    { 1, "CASH", "Готівка" },
                    { 2, "BANK_CARD", "Банківська картка" },
                    { 3, "CRYPTO", "Криптогаманець" },
                    { 4, "OTHER", "Інше" }
                });

            migrationBuilder.InsertData(
                table: "Currencies",
                columns: new[] { "Id", "Code", "DecimalPlaces", "Name", "Symbol" },
                values: new object[,]
                {
                    { 1, "UAH", (byte)2, "Українська гривня", "₴" },
                    { 2, "USD", (byte)2, "Долар США", "$" },
                    { 3, "EUR", (byte)2, "Євро", "€" },
                    { 4, "PLN", (byte)2, "Польський злотий", "zł" },
                    { 5, "GBP", (byte)2, "Фунт стерлінгів", "£" },
                    { 6, "BTC", (byte)8, "Bitcoin", "₿" },
                    { 7, "ETH", (byte)8, "Ethereum", "Ξ" },
                    { 8, "USDT", (byte)6, "Tether", "₮" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AccountTypeId",
                table: "Accounts",
                column: "AccountTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_CurrencyId",
                table: "Accounts",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_UserId",
                table: "Accounts",
                column: "UserId")
                .Annotation("SqlServer:Include", new[] { "Name", "Balance" });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_UserId_IsActive",
                table: "Accounts",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountTypes_Code",
                table: "AccountTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_OccurredAt",
                table: "AuditEntries",
                column: "OccurredAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_UserId",
                table: "AuditEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentCategoryId",
                table: "Categories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_UserId_Kind",
                table: "Categories",
                columns: new[] { "UserId", "Kind", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UQ_Categories",
                table: "Categories",
                columns: new[] { "UserId", "ParentCategoryId", "Name" },
                unique: true,
                filter: "[ParentCategoryId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_Code",
                table: "Currencies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_QuoteCurrencyId",
                table: "ExchangeRates",
                column: "QuoteCurrencyId");

            migrationBuilder.CreateIndex(
                name: "UQ_ExchangeRates",
                table: "ExchangeRates",
                columns: new[] { "BaseCurrencyId", "QuoteCurrencyId", "EffectiveDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KeyDerivationConfigs_UserId",
                table: "KeyDerivationConfigs",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId_OccurredAt",
                table: "Transactions",
                columns: new[] { "AccountId", "OccurredAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CategoryId",
                table: "Transactions",
                column: "CategoryId",
                filter: "[CategoryId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_LinkedTransactionId",
                table: "Transactions",
                column: "LinkedTransactionId",
                filter: "[LinkedTransactionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_OccurredAt",
                table: "Transactions",
                column: "OccurredAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEntries");

            migrationBuilder.DropTable(
                name: "ExchangeRates");

            migrationBuilder.DropTable(
                name: "KeyDerivationConfigs");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "AccountTypes");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
