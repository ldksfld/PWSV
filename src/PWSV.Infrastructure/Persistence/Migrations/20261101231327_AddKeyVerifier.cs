using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PWSV.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKeyVerifier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "VerifierCipher",
                table: "KeyDerivationConfigs",
                type: "varbinary(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "VerifierNonce",
                table: "KeyDerivationConfigs",
                type: "varbinary(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "VerifierTag",
                table: "KeyDerivationConfigs",
                type: "varbinary(16)",
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerifierCipher",
                table: "KeyDerivationConfigs");

            migrationBuilder.DropColumn(
                name: "VerifierNonce",
                table: "KeyDerivationConfigs");

            migrationBuilder.DropColumn(
                name: "VerifierTag",
                table: "KeyDerivationConfigs");
        }
    }
}
