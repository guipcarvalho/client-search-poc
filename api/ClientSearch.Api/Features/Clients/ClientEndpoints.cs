using ClientSearch.Api.Domain;
using ClientSearch.Api.Infrastructure.Database;
using ClientSearch.Api.Infrastructure.Elasticsearch;
using ClientSearch.Api.Infrastructure.Messaging;
using FluentValidation;
using MassTransit;

namespace ClientSearch.Api.Features.Clients;

public static class ClientEndpoints
{
    public static IEndpointRouteBuilder MapClientEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clients").WithTags("Clients");

        group.MapGet("/", ListClients)
            .WithSummary("List clients (paged) from Postgres");

        group.MapGet("/search", SearchClients)
            .WithSummary("Search clients in Elasticsearch");

        group.MapGet("/{id:guid}", GetClientById)
            .WithSummary("Fetch a single client by id");

        group.MapPost("/", CreateClient)
            .WithSummary("Create a new client; publishes ClientCreated to RabbitMQ");

        group.MapPut("/{id:guid}", UpdateClient)
            .WithSummary("Update a client; publishes ClientUpdated to RabbitMQ");

        group.MapDelete("/{id:guid}", DeleteClient)
            .WithSummary("Delete a client; publishes ClientDeleted to RabbitMQ");

        return app;
    }

    private static async Task<IResult> ListClients(
        IClientRepository repository,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var clients = await repository.ListAsync(skip, Math.Min(take, 200), cancellationToken);
        return Results.Ok(clients.Select(ClientResponse.FromDomain));
    }

    private static async Task<IResult> SearchClients(
        IClientSearchService searchService,
        string? q,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var hits = await searchService.SearchAsync(q, skip, Math.Min(take, 100), cancellationToken);
        return Results.Ok(hits);
    }

    private static async Task<IResult> GetClientById(
        Guid id,
        IClientRepository repository,
        CancellationToken cancellationToken)
    {
        var client = await repository.GetByIdAsync(id, cancellationToken);
        return client is null
            ? Results.NotFound()
            : Results.Ok(ClientResponse.FromDomain(client));
    }

    private static async Task<IResult> CreateClient(
        CreateClientRequest request,
        IValidator<CreateClientRequest> validator,
        IClientRepository repository,
        IPublishEndpoint publishEndpoint,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var client = new Client
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            Document = request.Document,
            Phone = request.Phone,
            CreatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(client, cancellationToken);

        await publishEndpoint.Publish(
            new ClientCreated(client.Id, client.Name, client.Email, client.Document, client.Phone, client.CreatedAt),
            cancellationToken);

        return Results.Created($"/api/clients/{client.Id}", ClientResponse.FromDomain(client));
    }

    private static async Task<IResult> UpdateClient(
        Guid id,
        UpdateClientRequest request,
        IValidator<UpdateClientRequest> validator,
        IClientRepository repository,
        IPublishEndpoint publishEndpoint,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var existing = await repository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return Results.NotFound();
        }

        existing.Name = request.Name;
        existing.Email = request.Email;
        existing.Document = request.Document;
        existing.Phone = request.Phone;
        existing.UpdatedAt = DateTime.UtcNow;

        var ok = await repository.UpdateAsync(existing, cancellationToken);
        if (!ok)
        {
            return Results.NotFound();
        }

        await publishEndpoint.Publish(
            new ClientUpdated(existing.Id, existing.Name, existing.Email, existing.Document, existing.Phone, existing.UpdatedAt!.Value),
            cancellationToken);

        return Results.Ok(ClientResponse.FromDomain(existing));
    }

    private static async Task<IResult> DeleteClient(
        Guid id,
        IClientRepository repository,
        IPublishEndpoint publishEndpoint,
        CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return Results.NotFound();
        }

        await publishEndpoint.Publish(new ClientDeleted(id), cancellationToken);
        return Results.NoContent();
    }
}
