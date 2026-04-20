namespace ClientSearch.Api.Infrastructure.Elasticsearch;

public sealed class ClientDocument
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Document { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
