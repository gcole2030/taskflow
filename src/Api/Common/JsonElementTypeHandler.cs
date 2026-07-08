using System.Data;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using Dapper;

namespace Api.Common;

public sealed class JsonElementTypeHandler : SqlMapper.TypeHandler<JsonElement>
{
    public override void SetValue(IDbDataParameter parameter, JsonElement value)
    {
        if (parameter is NpgsqlParameter npgsqlParameter)
            npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Jsonb;

        parameter.Value = value.GetRawText();
    }

    public override JsonElement Parse(object value) => value switch
    {
        string json => JsonDocument.Parse(json).RootElement,
        JsonElement element => element,
        _ => throw new InvalidCastException($"Cannot convert {value.GetType()} to JsonElement."),
    };
}
