namespace CovidLetter.Backend.Common.Infrastructure.Postgres;

using System.Data;
using Dapper;
using Npgsql;
using NpgsqlTypes;

public record JsonField(string Json);

public class JsonFieldHandler : SqlMapper.TypeHandler<JsonField?>
{
    public override JsonField? Parse(object? value)
    {
        return value is string json
            ? new JsonField(json)
            : null;
    }

    public override void SetValue(IDbDataParameter parameter, JsonField? value)
    {
        parameter.Value = value?.Json;
        parameter.DbType = DbType.String;
        if (parameter is NpgsqlParameter npgsqlParameter)
        {
            npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Jsonb;
        }
    }
}
