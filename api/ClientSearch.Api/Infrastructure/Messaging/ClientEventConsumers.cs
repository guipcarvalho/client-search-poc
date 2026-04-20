using ClientSearch.Api.Infrastructure.Elasticsearch;
using MassTransit;

namespace ClientSearch.Api.Infrastructure.Messaging;

public sealed class ClientCreatedConsumer(IClientSearchService searchService, ILogger<ClientCreatedConsumer> logger)
    : IConsumer<ClientCreated>
{
    public async Task Consume(ConsumeContext<ClientCreated> context)
    {
        var message = context.Message;
        logger.LogInformation("Indexing newly created client {ClientId}", message.Id);

        await searchService.IndexAsync(new ClientDocument
        {
            Id = message.Id,
            Name = message.Name,
            Email = message.Email,
            Document = message.Document,
            Phone = message.Phone,
            CreatedAt = message.CreatedAt
        }, context.CancellationToken);
    }
}

public sealed class ClientUpdatedConsumer(IClientSearchService searchService, ILogger<ClientUpdatedConsumer> logger)
    : IConsumer<ClientUpdated>
{
    public async Task Consume(ConsumeContext<ClientUpdated> context)
    {
        var message = context.Message;
        logger.LogInformation("Re-indexing updated client {ClientId}", message.Id);

        await searchService.IndexAsync(new ClientDocument
        {
            Id = message.Id,
            Name = message.Name,
            Email = message.Email,
            Document = message.Document,
            Phone = message.Phone,
            UpdatedAt = message.UpdatedAt
        }, context.CancellationToken);
    }
}

public sealed class ClientDeletedConsumer(IClientSearchService searchService, ILogger<ClientDeletedConsumer> logger)
    : IConsumer<ClientDeleted>
{
    public async Task Consume(ConsumeContext<ClientDeleted> context)
    {
        logger.LogInformation("Removing client {ClientId} from search index", context.Message.Id);
        await searchService.DeleteAsync(context.Message.Id, context.CancellationToken);
    }
}
