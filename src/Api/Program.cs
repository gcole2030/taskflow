using System.Text.Json.Serialization;
using Api.Common;
using Api.Features.Tasks;
using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
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
    SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
    SqlMapper.AddTypeHandler(new JsonElementTypeHandler());

    builder.Services.AddSingleton(sp =>
    {
        var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Db")
            ?? throw new InvalidOperationException("ConnectionStrings:Db is not configured.");
        return Db.CreateDataSource(connectionString);
    });

    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    builder.Services.AddSingleton<IClock, SystemClock>();
    builder.Services.AddSingleton<TasksRepository>();
    builder.Services.AddSingleton<CreateTaskValidator>();

    var app = builder.Build();

    var connectionString = app.Configuration.GetConnectionString("Db")
        ?? throw new InvalidOperationException("ConnectionStrings:Db is not configured.");

    Migrator.Run(connectionString, new DbUpSerilogLogger());

    app.MapGet("/healthz", () => TypedResults.Ok(new { status = "ok" }));

    app.MapGet("/readyz", async Task<Results<Ok<object>, ProblemHttpResult>> (NpgsqlDataSource dataSource) =>
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync();
            return TypedResults.Ok<object>(new { status = "ready" });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Readiness check failed");
            return TypedResults.Problem(detail: "Database unavailable", statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    });

    app.MapGroup("/api/v1").MapTasks();

    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
