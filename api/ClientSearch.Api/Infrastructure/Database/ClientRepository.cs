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

public sealed class ClientRepository(IDbConnectionFactory connectionFactory) : IClientRepository
{
    public async Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Client>(
            """
            SELECT id, name, email, document, phone, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM clients WHERE id = @Id
            """,
            new { Id = id });
    }

    public async Task<IReadOnlyList<Client>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateAsync(cancellationToken);
        var rows = await connection.QueryAsync<Client>(
            """
            SELECT id, name, email, document, phone, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM clients
            ORDER BY created_at DESC
            OFFSET @Skip LIMIT @Take
            """,
            new { Skip = skip, Take = take });
        return rows.AsList();
    }

    public async Task AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            INSERT INTO clients (id, name, email, document, phone, created_at)
            VALUES (@Id, @Name, @Email, @Document, @Phone, @CreatedAt)
            """,
            client);
    }

    public async Task<bool> UpdateAsync(Client client, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateAsync(cancellationToken);
        var rows = await connection.ExecuteAsync(
            """
            UPDATE clients
               SET name = @Name,
                   email = @Email,
                   document = @Document,
                   phone = @Phone,
                   updated_at = NOW()
             WHERE id = @Id
            """,
            client);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateAsync(cancellationToken);
        var rows = await connection.ExecuteAsync("DELETE FROM clients WHERE id = @Id", new { Id = id });
        return rows > 0;
    }
}
