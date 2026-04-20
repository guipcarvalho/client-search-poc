namespace ClientSearch.Api.Domain;

public sealed class Client
{
    public Guid Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; set; }
}
