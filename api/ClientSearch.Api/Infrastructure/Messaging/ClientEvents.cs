namespace ClientSearch.Api.Infrastructure.Messaging;

public record ClientCreated(Guid Id, string Name, string Email, string Document, string? Phone, DateTime CreatedAt);

public record ClientUpdated(Guid Id, string Name, string Email, string Document, string? Phone, DateTime UpdatedAt);

public record ClientDeleted(Guid Id);
