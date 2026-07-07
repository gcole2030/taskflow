using Api.Common;
using Dapper;
using Npgsql;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration).WriteTo.Console());

    SqlMapper.AddTypeHandler(new EnumTypeHandler<Api.Domain.TaskStatus>());
    SqlMapper.AddTypeHandler(new EnumTypeHandler<Api.Domain.Priority>());

    builder.Services.AddSingleton(sp =>
    {
        var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Db")
            ?? throw new InvalidOperationException("ConnectionStrings:Db is not configured.");
        return Db.CreateDataSource(connectionString);
    });

    var app = builder.Build();

    var connectionString = app.Configuration.GetConnectionString("Db")
        ?? throw new InvalidOperationException("ConnectionStrings:Db is not configured.");

    Migrator.Run(connectionString, new DbUpSerilogLogger());

    app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

    app.MapGet("/readyz", async (NpgsqlDataSource dataSource) =>
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync();
            return Results.Ok(new { status = "ready" });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Readiness check failed");
            return Results.Problem(detail: "Database unavailable", statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    });

    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
