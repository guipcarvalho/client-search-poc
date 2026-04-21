using System.Text.Json;
using ClientSearch.Api.Infrastructure.Database;
using Dapper;
using MassTransit;
using Microsoft.Extensions.Options;

namespace ClientSearch.Api.Infrastructure.Messaging.Outbox;

public sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    OutboxMessageTypeRegistry typeRegistry,
    IOptionsMonitor<OutboxOptions> options,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox dispatcher starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            var current = options.CurrentValue;
            try
            {
                var dispatched = await DispatchBatchAsync(current, stoppingToken);
                if (dispatched == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, current.PollIntervalSeconds)), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox dispatcher iteration failed");
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, current.PollIntervalSeconds)), stoppingToken);
            }
        }

        logger.LogInformation("Outbox dispatcher stopping");
    }

    private async Task<int> DispatchBatchAsync(OutboxOptions opts, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var session = scope.ServiceProvider.GetRequiredService<IDbSession>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        return await unitOfWork.ExecuteAsync(async innerCt =>
        {
            var connection = await session.GetConnectionAsync(innerCt);
            var transaction = session.CurrentTransaction;

            var rows = (await connection.QueryAsync<OutboxRow>(new CommandDefinition(
                """
                SELECT id            AS Id,
                       message_type  AS MessageType,
                       payload       AS Payload,
                       attempt_count AS AttemptCount
                  FROM outbox_messages
                 WHERE processed_at IS NULL
                   AND next_attempt_at <= NOW()
                 ORDER BY occurred_at
                 LIMIT @BatchSize
                 FOR UPDATE SKIP LOCKED
                """,
                new { BatchSize = opts.BatchSize },
                transaction,
                cancellationToken: innerCt))).AsList();

            foreach (var row in rows)
            {
                try
                {
                    if (!typeRegistry.TryResolve(row.MessageType, out var clrType))
                    {
                        throw new InvalidOperationException(
                            $"No outbox message type registered for '{row.MessageType}'");
                    }

                    var message = JsonSerializer.Deserialize(row.Payload, clrType, SerializerOptions)
                        ?? throw new InvalidOperationException(
                            $"Failed to deserialize payload for outbox message {row.Id} ({row.MessageType})");

                    await publishEndpoint.Publish(message, clrType, innerCt);

                    await connection.ExecuteAsync(new CommandDefinition(
                        """
                        UPDATE outbox_messages
                           SET processed_at = NOW(),
                               last_error   = NULL
                         WHERE id = @Id
                        """,
                        new { row.Id },
                        transaction,
                        cancellationToken: innerCt));

                    logger.LogDebug("Published outbox message {OutboxId} ({MessageType})", row.Id, row.MessageType);
                }
                catch (Exception ex)
                {
                    var nextAttempt = row.AttemptCount + 1;
                    var backoffSeconds = Math.Min(
                        opts.MaxBackoffSeconds,
                        (int)Math.Pow(2, Math.Min(nextAttempt, 20)));

                    logger.LogError(
                        ex,
                        "Failed to publish outbox message {OutboxId} ({MessageType}); attempt {Attempt}, backing off {Backoff}s",
                        row.Id, row.MessageType, nextAttempt, backoffSeconds);

                    await connection.ExecuteAsync(new CommandDefinition(
                        """
                        UPDATE outbox_messages
                           SET attempt_count   = @AttemptCount,
                               last_error      = @LastError,
                               next_attempt_at = NOW() + (@BackoffSeconds * INTERVAL '1 second')
                         WHERE id = @Id
                        """,
                        new
                        {
                            row.Id,
                            AttemptCount = nextAttempt,
                            LastError = ex.ToString(),
                            BackoffSeconds = backoffSeconds
                        },
                        transaction,
                        cancellationToken: innerCt));
                }
            }

            return rows.Count;
        }, ct);
    }

    private sealed record OutboxRow(Guid Id, string MessageType, string Payload, int AttemptCount);
}
