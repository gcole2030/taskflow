using Npgsql;

namespace Api.Common;

public static class Db
{
    public static NpgsqlDataSource CreateDataSource(string connectionString) =>
        new NpgsqlDataSourceBuilder(connectionString).Build();
}
