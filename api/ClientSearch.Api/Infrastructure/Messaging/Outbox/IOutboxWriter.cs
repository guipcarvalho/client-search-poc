using System.Text.Json;
using ClientSearch.Api.Infrastructure.Database;
using Dapper;

namespace ClientSearch.Api.Infrastructure.Messaging.Outbox;

public interface IOutboxWriter
{
    Task EnqueueAsync(object message, CancellationToken cancellationToken = default);
}

public sealed class OutboxWriter(IDbSession session) : IOutboxWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task EnqueueAsync(object message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (session.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "Outbox messages can only be enqueued inside an active unit of work (IUnitOfWork.ExecuteAsync).");
        }

        var messageType = message.GetType();
        var payload = JsonSerializer.Serialize(message, messageType, SerializerOptions);

        var connection = await session.GetConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO outbox_messages (id, message_type, payload)
            VALUES (@Id, @MessageType, @Payload::jsonb)
            """,
            new
            {
                Id = Guid.NewGuid(),
                MessageType = messageType.FullName
                    ?? throw new InvalidOperationException($"Cannot enqueue message without a full type name: {messageType}"),
                Payload = payload
            },
            session.CurrentTransaction,
            cancellationToken: cancellationToken));
    }
}
