using ClientSearch.Api.Domain;
using Dapper;

namespace ClientSearch.Api.Infrastructure.Database;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Client>> ListAsync(int skip, int take, CancellationToken cancellationToken = default);
    Task AddAsync(Client client, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Client client, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class ClientRepository(IDbSession session) : IClientRepository
{
    public async Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Client>(new CommandDefinition(
            """
            SELECT id, name, email, document, phone, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM clients WHERE id = @Id
            """,
            new { Id = id },
            session.CurrentTransaction,
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Client>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<Client>(new CommandDefinition(
            """
            SELECT id, name, email, document, phone, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM clients
            ORDER BY created_at DESC
            OFFSET @Skip LIMIT @Take
            """,
            new { Skip = skip, Take = take },
            session.CurrentTransaction,
            cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public async Task AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO clients (id, name, email, document, phone, created_at)
            VALUES (@Id, @Name, @Email, @Document, @Phone, @CreatedAt)
            """,
            client,
            session.CurrentTransaction,
            cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(Client client, CancellationToken cancellationToken = default)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var rows = await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE clients
               SET name = @Name,
                   email = @Email,
                   document = @Document,
                   phone = @Phone,
                   updated_at = NOW()
             WHERE id = @Id
            """,
            client,
            session.CurrentTransaction,
            cancellationToken: cancellationToken));
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var rows = await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM clients WHERE id = @Id",
            new { Id = id },
            session.CurrentTransaction,
            cancellationToken: cancellationToken));
        return rows > 0;
    }
}
