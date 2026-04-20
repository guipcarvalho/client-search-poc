using Dapper;

namespace ClientSearch.Api.Infrastructure.Database;

public sealed class DatabaseInitializer(IDbConnectionFactory connectionFactory, ILogger<DatabaseInitializer> logger)
{
    private const string CreateClientsTable = """
        CREATE TABLE IF NOT EXISTS clients (
            id UUID PRIMARY KEY,
            name TEXT NOT NULL,
            email TEXT NOT NULL UNIQUE,
            document TEXT NOT NULL UNIQUE,
            phone TEXT NULL,
            created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            updated_at TIMESTAMPTZ NULL
        );

        CREATE INDEX IF NOT EXISTS idx_clients_name ON clients (name);
        """;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Ensuring database schema is in place");
        using var connection = await connectionFactory.CreateAsync(cancellationToken);
        await connection.ExecuteAsync(CreateClientsTable);
    }
}
