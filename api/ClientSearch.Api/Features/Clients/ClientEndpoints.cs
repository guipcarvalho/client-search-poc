using ClientSearch.Api.Domain;
using ClientSearch.Api.Infrastructure.Database;
using ClientSearch.Api.Infrastructure.Elasticsearch;
using ClientSearch.Api.Infrastructure.Messaging;
using ClientSearch.Api.Infrastructure.Messaging.Outbox;
using FluentValidation;

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
            .WithSummary("Create a new client; enqueues ClientCreated in the transactional outbox");

        group.MapPut("/{id:guid}", UpdateClient)
            .WithSummary("Update a client; enqueues ClientUpdated in the transactional outbox");

        group.MapDelete("/{id:guid}", DeleteClient)
            .WithSummary("Delete a client; enqueues ClientDeleted in the transactional outbox");

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
        IUnitOfWork unitOfWork,
        IOutboxWriter outbox,
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

        await unitOfWork.ExecuteAsync(async ct =>
        {
            await repository.AddAsync(client, ct);
            await outbox.EnqueueAsync(
                new ClientCreated(client.Id, client.Name, client.Email, client.Document, client.Phone, client.CreatedAt),
                ct);
        }, cancellationToken);

        return Results.Created($"/api/clients/{client.Id}", ClientResponse.FromDomain(client));
    }

    private static async Task<IResult> UpdateClient(
        Guid id,
        UpdateClientRequest request,
        IValidator<UpdateClientRequest> validator,
        IClientRepository repository,
        IUnitOfWork unitOfWork,
        IOutboxWriter outbox,
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

        var updated = await unitOfWork.ExecuteAsync(async ct =>
        {
            var ok = await repository.UpdateAsync(existing, ct);
            if (!ok)
            {
                return false;
            }

            await outbox.EnqueueAsync(
                new ClientUpdated(existing.Id, existing.Name, existing.Email, existing.Document, existing.Phone, existing.UpdatedAt!.Value),
                ct);
            return true;
        }, cancellationToken);

        return updated
            ? Results.Ok(ClientResponse.FromDomain(existing))
            : Results.NotFound();
    }

    private static async Task<IResult> DeleteClient(
        Guid id,
        IClientRepository repository,
        IUnitOfWork unitOfWork,
        IOutboxWriter outbox,
        CancellationToken cancellationToken)
    {
        var deleted = await unitOfWork.ExecuteAsync(async ct =>
        {
            var ok = await repository.DeleteAsync(id, ct);
            if (!ok)
            {
                return false;
            }

            await outbox.EnqueueAsync(new ClientDeleted(id), ct);
            return true;
        }, cancellationToken);

        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
