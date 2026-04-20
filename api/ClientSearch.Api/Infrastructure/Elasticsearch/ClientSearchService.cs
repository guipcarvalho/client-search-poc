using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace ClientSearch.Api.Infrastructure.Elasticsearch;

public interface IClientSearchService
{
    Task EnsureIndexExistsAsync(CancellationToken cancellationToken = default);
    Task IndexAsync(ClientDocument document, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClientDocument>> SearchAsync(string? query, int skip, int take, CancellationToken cancellationToken = default);
}

public sealed class ClientSearchService(ElasticsearchClient client, ILogger<ClientSearchService> logger) : IClientSearchService
{
    public const string IndexName = "clients";

    public async Task EnsureIndexExistsAsync(CancellationToken cancellationToken = default)
    {
        var exists = await client.Indices.ExistsAsync(IndexName, cancellationToken);
        if (exists.Exists)
        {
            return;
        }

        logger.LogInformation("Creating Elasticsearch index {Index}", IndexName);

        var response = await client.Indices.CreateAsync(IndexName, c => c
            .Mappings(m => m
                .Properties<ClientDocument>(p => p
                    .Keyword(k => k.Id)
                    .Text(t => t.Name, t => t.Fields(f => f.Keyword("keyword")))
                    .Keyword(k => k.Email)
                    .Keyword(k => k.Document)
                    .Keyword(k => k.Phone)
                    .Date(d => d.CreatedAt)
                    .Date(d => d.UpdatedAt))),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            logger.LogWarning("Could not create index {Index}: {Error}", IndexName, response.DebugInformation);
        }
    }

    public async Task IndexAsync(ClientDocument document, CancellationToken cancellationToken = default)
    {
        var response = await client.IndexAsync(document, i => i
            .Index(IndexName)
            .Id(document.Id.ToString())
            .Refresh(Elastic.Clients.Elasticsearch.Refresh.WaitFor),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            logger.LogError("Failed to index client {Id}: {Error}", document.Id, response.DebugInformation);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await client.DeleteAsync<ClientDocument>(id.ToString(), d => d
            .Index(IndexName)
            .Refresh(Elastic.Clients.Elasticsearch.Refresh.WaitFor),
            cancellationToken);
    }

    public async Task<IReadOnlyList<ClientDocument>> SearchAsync(string? query, int skip, int take, CancellationToken cancellationToken = default)
    {
        var response = await client.SearchAsync<ClientDocument>(s =>
        {
            s.Indices(IndexName).From(skip).Size(take);

            if (string.IsNullOrWhiteSpace(query))
            {
                s.Query(q => q.MatchAll(new MatchAllQuery()));
            }
            else
            {
                s.Query(q => q.MultiMatch(m => m
                    .Query(query)
                    .Fields(new[] { "name^3", "email", "document", "phone" })
                    .Fuzziness(new Fuzziness("AUTO"))));
            }
        }, cancellationToken);

        if (!response.IsValidResponse)
        {
            logger.LogWarning("Search failed: {Error}", response.DebugInformation);
            return Array.Empty<ClientDocument>();
        }

        return response.Documents.ToArray();
    }
}
