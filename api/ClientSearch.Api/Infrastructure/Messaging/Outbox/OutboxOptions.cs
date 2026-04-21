namespace ClientSearch.Api.Infrastructure.Messaging.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public int PollIntervalSeconds { get; set; } = 1;

    public int BatchSize { get; set; } = 50;

    public int MaxBackoffSeconds { get; set; } = 300;
}
