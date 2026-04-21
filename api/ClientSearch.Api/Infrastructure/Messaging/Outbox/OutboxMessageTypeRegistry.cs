namespace ClientSearch.Api.Infrastructure.Messaging.Outbox;

public sealed class OutboxMessageTypeRegistry
{
    private readonly Dictionary<string, Type> _typesByName;

    public OutboxMessageTypeRegistry(IEnumerable<Type> messageTypes)
    {
        _typesByName = messageTypes.ToDictionary(
            t => t.FullName ?? throw new InvalidOperationException($"Type {t} has no FullName"),
            t => t,
            StringComparer.Ordinal);
    }

    public bool TryResolve(string messageType, out Type type) =>
        _typesByName.TryGetValue(messageType, out type!);

    public Type Resolve(string messageType) =>
        _typesByName.TryGetValue(messageType, out var type)
            ? type
            : throw new InvalidOperationException($"No outbox message type registered for '{messageType}'");
}
