using System.Reflection;
using DbUp;
using DbUp.Engine;
using DbUp.Engine.Output;

namespace Api.Common;

public static class Migrator
{
    public static DatabaseUpgradeResult Run(string connectionString, IUpgradeLog log)
    {
        EnsureDatabase.For.PostgresqlDatabase(connectionString, log);

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                Assembly.GetExecutingAssembly(),
                name => name.StartsWith("Api.Migrations", StringComparison.Ordinal))
            .LogTo(log)
            .WithTransaction()
            .Build();

        return upgrader.PerformUpgrade();
    }
}
