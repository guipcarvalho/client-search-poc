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

    private const string CreateOutboxTable = """
        CREATE TABLE IF NOT EXISTS outbox_messages (
            id              UUID PRIMARY KEY,
            occurred_at     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            message_type    TEXT        NOT NULL,
            payload         JSONB       NOT NULL,
            processed_at    TIMESTAMPTZ NULL,
            attempt_count   INT         NOT NULL DEFAULT 0,
            last_error      TEXT        NULL,
            next_attempt_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );

        CREATE INDEX IF NOT EXISTS idx_outbox_pending
            ON outbox_messages (next_attempt_at)
            WHERE processed_at IS NULL;
        """;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Ensuring database schema is in place");
        await using var connection = await connectionFactory.CreateAsync(cancellationToken);
        await connection.ExecuteAsync(CreateClientsTable);
        await connection.ExecuteAsync(CreateOutboxTable);
    }
}
