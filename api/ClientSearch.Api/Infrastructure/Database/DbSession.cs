using System.Data;
using Npgsql;

namespace ClientSearch.Api.Infrastructure.Database;

public interface IDbSession
{
    Task<IDbConnection> GetConnectionAsync(CancellationToken cancellationToken = default);

    IDbTransaction? CurrentTransaction { get; }
}

public interface IUnitOfWork
{
    Task ExecuteAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken = default);

    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken = default);
}

public sealed class DbSession(IDbConnectionFactory connectionFactory) : IDbSession, IUnitOfWork, IAsyncDisposable
{
    private NpgsqlConnection? _connection;

    public IDbTransaction? CurrentTransaction { get; private set; }

    public async Task<IDbConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        _connection ??= await connectionFactory.CreateAsync(cancellationToken);
        return _connection;
    }

    public async Task ExecuteAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync<object?>(async ct =>
        {
            await work(ct);
            return null;
        }, cancellationToken);
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken = default)
    {
        if (CurrentTransaction is not null)
        {
            throw new InvalidOperationException("A transaction is already active on this session; nested transactions are not supported.");
        }

        _connection ??= await connectionFactory.CreateAsync(cancellationToken);
        var transaction = await _connection.BeginTransactionAsync(cancellationToken);
        CurrentTransaction = transaction;
        try
        {
            var result = await work(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            CurrentTransaction = null;
            await transaction.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
