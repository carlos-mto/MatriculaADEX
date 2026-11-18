using Npgsql;

namespace Matricula.Infrastructure.Data;

public static class ConnectionStringFactory
{
    public static string FromEnvironment()
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = Environment.GetEnvironmentVariable("DB_HOST"),
            Username = Environment.GetEnvironmentVariable("DATABASE_USER"),
            Password = Environment.GetEnvironmentVariable("DATABASE_PASSWORD"),
            Database = Environment.GetEnvironmentVariable("DB_NAME"),
            Pooling = true
        };

        if (int.TryParse(Environment.GetEnvironmentVariable("DB_PORT"), out var port))
            builder.Port = port;

        return builder.ConnectionString;
    }
}
