namespace Fulfillment.UnitTests;

using FluentAssertions;
using Fulfillment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

/// <summary>The repair migrations run against shared databases whose history differs, so every statement must be safe to repeat.</summary>
public sealed class MigrationIdempotencyTests
{
    [Theory]
    [InlineData("20261116150000_EnsureWebhookAuditLogs")]
    [InlineData("20261116160000_AddWebhookAuditLogNotifyOutbox")]
    [InlineData("20261117090000_EnsureShipmentLookupIndexes")]
    [InlineData("20261118100000_AddBuyerIdToShipment")]
    public void RepairMigrations_OnlyUseIfNotExistsStatements(string id)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql("Host=localhost;Database=unused").Options;
        using var db = new AppDbContext(options);
        var migrations = db.GetService<IMigrationsAssembly>().Migrations;
        migrations.Should().ContainKey(id);

        var migration = (Migration)Activator.CreateInstance(migrations[id].AsType())!;
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        migration.GetType().GetMethod("Up", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(migration, new object[] { builder });

        var sql = builder.Operations.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.SqlOperation>().Select(o => o.Sql).ToList();
        sql.Should().NotBeEmpty();
        foreach (var statement in sql.SelectMany(s => s.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))
        {
            var upper = statement.ToUpperInvariant();
            if (upper.StartsWith("CREATE")) upper.Should().Contain("IF NOT EXISTS", statement);
            if (upper.Contains("ADD COLUMN")) upper.Should().Contain("ADD COLUMN IF NOT EXISTS", statement);
            upper.Should().NotContain("DROP ", statement);
        }
    }
}
