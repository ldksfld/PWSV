using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PWSV.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixTransferDirection : Migration
    {
        private const string AlterAfterInsert = """
            CREATE OR ALTER TRIGGER dbo.trg_Transactions_AfterInsert
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
                            WHEN 'T' THEN CASE WHEN i.LinkedTransactionId IS NULL OR i.LinkedTransactionId > i.Id THEN -i.Amount ELSE i.Amount END
                            ELSE 0
                        END,
                    a.UpdatedAt = SYSUTCDATETIME()
                FROM dbo.Accounts a
                INNER JOIN inserted i ON i.AccountId = a.Id;
            END;
            """;

        private const string AlterAfterUpdate = """
            CREATE OR ALTER TRIGGER dbo.trg_Transactions_AfterUpdate
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
                            WHEN 'T' THEN CASE WHEN d.LinkedTransactionId IS NULL OR d.LinkedTransactionId > d.Id THEN -d.Amount ELSE d.Amount END
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
                            WHEN 'T' THEN CASE WHEN i.LinkedTransactionId IS NULL OR i.LinkedTransactionId > i.Id THEN -i.Amount ELSE i.Amount END
                            ELSE 0
                        END,
                    a.UpdatedAt = SYSUTCDATETIME()
                FROM dbo.Accounts a
                INNER JOIN inserted i ON i.AccountId = a.Id;
            END;
            """;

        private const string AlterAfterDelete = """
            CREATE OR ALTER TRIGGER dbo.trg_Transactions_AfterDelete
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
                            WHEN 'T' THEN CASE WHEN d.LinkedTransactionId IS NULL OR d.LinkedTransactionId > d.Id THEN -d.Amount ELSE d.Amount END
                            ELSE 0
                        END,
                    a.UpdatedAt = SYSUTCDATETIME()
                FROM dbo.Accounts a
                INNER JOIN deleted d ON d.AccountId = a.Id;
            END;
            """;

        private const string RecalculateBalances = """
            UPDATE a
            SET a.Balance = totals.Balance
            FROM dbo.Accounts a
            CROSS APPLY (
                SELECT COALESCE(SUM(
                    CASE t.Kind
                        WHEN 'I' THEN t.Amount
                        WHEN 'E' THEN -t.Amount
                        WHEN 'T' THEN CASE WHEN t.LinkedTransactionId IS NULL OR t.LinkedTransactionId > t.Id THEN -t.Amount ELSE t.Amount END
                        ELSE 0
                    END), 0) AS Balance
                FROM dbo.Transactions t
                WHERE t.AccountId = a.Id
            ) totals
            WHERE a.Balance <> totals.Balance;
            """;

        private const string RestoreAfterInsert = """
            CREATE OR ALTER TRIGGER dbo.trg_Transactions_AfterInsert
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

        private const string RestoreAfterUpdate = """
            CREATE OR ALTER TRIGGER dbo.trg_Transactions_AfterUpdate
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

        private const string RestoreAfterDelete = """
            CREATE OR ALTER TRIGGER dbo.trg_Transactions_AfterDelete
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

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(AlterAfterInsert);
            migrationBuilder.Sql(AlterAfterUpdate);
            migrationBuilder.Sql(AlterAfterDelete);
            migrationBuilder.Sql(RecalculateBalances);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(RestoreAfterInsert);
            migrationBuilder.Sql(RestoreAfterUpdate);
            migrationBuilder.Sql(RestoreAfterDelete);
        }
    }
}
