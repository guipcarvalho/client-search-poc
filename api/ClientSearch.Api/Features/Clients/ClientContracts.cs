using ClientSearch.Api.Domain;
using FluentValidation;

namespace ClientSearch.Api.Features.Clients;

public record CreateClientRequest(string Name, string Email, string Document, string? Phone);

public record UpdateClientRequest(string Name, string Email, string Document, string? Phone);

public record ClientResponse(Guid Id, string Name, string Email, string Document, string? Phone, DateTime CreatedAt, DateTime? UpdatedAt)
{
    public static ClientResponse FromDomain(Client client) =>
        new(client.Id, client.Name, client.Email, client.Document, client.Phone, client.CreatedAt, client.UpdatedAt);
}

public sealed class CreateClientValidator : AbstractValidator<CreateClientRequest>
{
    public CreateClientValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Document).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Phone).MaximumLength(32);
    }
}

public sealed class UpdateClientValidator : AbstractValidator<UpdateClientRequest>
{
    public UpdateClientValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Document).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Phone).MaximumLength(32);
    }
}
