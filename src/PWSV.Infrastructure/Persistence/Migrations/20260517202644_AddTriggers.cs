using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PWSV.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTriggers : Migration
    {
        private const string CreateAfterInsert = """
            CREATE TRIGGER dbo.trg_Transactions_AfterInsert
            ON dbo.Transactions
            AFTER INSERT
            AS
            BEGIN
                SET NOCOUNT ON;
                SET XACT_ABORT ON;

                IF NOT EXISTS (SELECT 1 FROM inserted)
                    RETURN;

                UPDATE a
                SET a.Balance = a.Balance +
                        CASE i.Kind
                            WHEN 'I' THEN i.Amount
                            WHEN 'E' THEN -i.Amount
                            WHEN 'T' THEN CASE WHEN i.LinkedTransactionId IS NULL THEN -i.Amount ELSE i.Amount END
                            ELSE 0
                        END,
                    a.UpdatedAt = SYSUTCDATETIME()
                FROM dbo.Accounts a
                INNER JOIN inserted i ON i.AccountId = a.Id;
            END;
            """;

        private const string CreateAfterUpdate = """
            CREATE TRIGGER dbo.trg_Transactions_AfterUpdate
            ON dbo.Transactions
            AFTER UPDATE
            AS
            BEGIN
                SET NOCOUNT ON;
                SET XACT_ABORT ON;

                IF NOT EXISTS (SELECT 1 FROM inserted)
                    RETURN;

                IF NOT UPDATE(AccountId) AND NOT UPDATE(Amount) AND NOT UPDATE(Kind)
                    RETURN;

                UPDATE a
                SET a.Balance = a.Balance -
                        CASE d.Kind
                            WHEN 'I' THEN d.Amount
                            WHEN 'E' THEN -d.Amount
                            WHEN 'T' THEN CASE WHEN d.LinkedTransactionId IS NULL THEN -d.Amount ELSE d.Amount END
                            ELSE 0
                        END,
                    a.UpdatedAt = SYSUTCDATETIME()
                FROM dbo.Accounts a
                INNER JOIN deleted d ON d.AccountId = a.Id;

                UPDATE a
                SET a.Balance = a.Balance +
                        CASE i.Kind
                            WHEN 'I' THEN i.Amount
                            WHEN 'E' THEN -i.Amount
                            WHEN 'T' THEN CASE WHEN i.LinkedTransactionId IS NULL THEN -i.Amount ELSE i.Amount END
                            ELSE 0
                        END,
                    a.UpdatedAt = SYSUTCDATETIME()
                FROM dbo.Accounts a
                INNER JOIN inserted i ON i.AccountId = a.Id;
            END;
            """;

        private const string CreateAfterDelete = """
            CREATE TRIGGER dbo.trg_Transactions_AfterDelete
            ON dbo.Transactions
            AFTER DELETE
            AS
            BEGIN
                SET NOCOUNT ON;
                SET XACT_ABORT ON;

                IF NOT EXISTS (SELECT 1 FROM deleted)
                    RETURN;

                UPDATE a
                SET a.Balance = a.Balance -
                        CASE d.Kind
                            WHEN 'I' THEN d.Amount
                            WHEN 'E' THEN -d.Amount
                            WHEN 'T' THEN CASE WHEN d.LinkedTransactionId IS NULL THEN -d.Amount ELSE d.Amount END
                            ELSE 0
                        END,
                    a.UpdatedAt = SYSUTCDATETIME()
                FROM dbo.Accounts a
                INNER JOIN deleted d ON d.AccountId = a.Id;
            END;
            """;

        private const string CreatePreventAccountDelete = """
            CREATE TRIGGER dbo.trg_Accounts_PreventDeleteWhenHasTransactions
            ON dbo.Accounts
            INSTEAD OF DELETE
            AS
            BEGIN
                SET NOCOUNT ON;
                SET XACT_ABORT ON;

                IF NOT EXISTS (SELECT 1 FROM deleted)
                    RETURN;

                IF EXISTS (
                    SELECT 1
                    FROM dbo.Transactions t WITH (HOLDLOCK, UPDLOCK)
                    INNER JOIN deleted d ON d.Id = t.AccountId
                )
                BEGIN
                    RAISERROR(N'Неможливо видалити рахунок, по якому існують транзакції.', 16, 1);
                    RETURN;
                END;

                DELETE a
                FROM dbo.Accounts a
                INNER JOIN deleted d ON d.Id = a.Id;
            END;
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(CreateAfterInsert);
            migrationBuilder.Sql(CreateAfterUpdate);
            migrationBuilder.Sql(CreateAfterDelete);
            migrationBuilder.Sql(CreatePreventAccountDelete);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS dbo.trg_Accounts_PreventDeleteWhenHasTransactions;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS dbo.trg_Transactions_AfterDelete;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS dbo.trg_Transactions_AfterUpdate;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS dbo.trg_Transactions_AfterInsert;");
        }
    }
}
