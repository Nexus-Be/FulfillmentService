namespace Fulfillment.UnitTests;

using Fulfillment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

public sealed class WebhookAuditLogMigrationTests
{
    [Fact]
    public void Migration_CreatesTheTableIfMissing_AndOnlyAddsTheMissingColumns()
    {
        // The GHN webhook writes to this table first; without these columns every webhook failed with 500.
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql("Host=localhost;Database=unused").Options);

        var script = db.GetService<IMigrator>().GenerateScript(
            fromMigration: "20261031002416_InitialCreate",
            toMigration: "20261116150000_EnsureWebhookAuditLogs");

        Assert.Contains("CREATE TABLE IF NOT EXISTS webhook_audit_logs", script);
        Assert.Contains("ADD COLUMN IF NOT EXISTS updated_at", script);
        Assert.Contains("ADD COLUMN IF NOT EXISTS is_deleted", script);
        Assert.Contains("ADD COLUMN IF NOT EXISTS deleted_at", script);
        Assert.DoesNotContain("DROP TABLE", script);
    }
}
