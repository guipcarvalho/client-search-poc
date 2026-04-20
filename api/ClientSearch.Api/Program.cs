using ClientSearch.Api.Features.Clients;
using ClientSearch.Api.Infrastructure.Database;
using ClientSearch.Api.Infrastructure.Elasticsearch;
using ClientSearch.Api.Infrastructure.Messaging;
using Elastic.Clients.Elasticsearch;
using FluentValidation;
using MassTransit;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, _, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext();
    });

    builder.Services.AddOpenApi();

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy => policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin());
    });

    var postgresConnection = builder.Configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("Missing ConnectionStrings:Postgres");
    builder.Services.AddSingleton<IDbConnectionFactory>(_ => new NpgsqlConnectionFactory(postgresConnection));
    builder.Services.AddScoped<IClientRepository, ClientRepository>();
    builder.Services.AddSingleton<DatabaseInitializer>();

    var elasticUri = builder.Configuration["Elasticsearch:Uri"] ?? "http://localhost:9200";
    builder.Services.AddSingleton(_ =>
    {
        var settings = new ElasticsearchClientSettings(new Uri(elasticUri))
            .DefaultIndex(ClientSearchService.IndexName);
        return new ElasticsearchClient(settings);
    });
    builder.Services.AddScoped<IClientSearchService, ClientSearchService>();

    builder.Services.AddValidatorsFromAssemblyContaining<CreateClientValidator>();

    builder.Services.AddMassTransit(bus =>
    {
        bus.AddConsumer<ClientCreatedConsumer>();
        bus.AddConsumer<ClientUpdatedConsumer>();
        bus.AddConsumer<ClientDeletedConsumer>();

        bus.UsingRabbitMq((context, cfg) =>
        {
            var host = builder.Configuration["RabbitMq:Host"] ?? "localhost";
            var vhost = builder.Configuration["RabbitMq:VirtualHost"] ?? "/";
            var user = builder.Configuration["RabbitMq:Username"] ?? "guest";
            var pass = builder.Configuration["RabbitMq:Password"] ?? "guest";

            cfg.Host(host, vhost, h =>
            {
                h.Username(user);
                h.Password(pass);
            });

            cfg.ConfigureEndpoints(context);
        });
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseCors();

    app.MapOpenApi();
    app.MapScalarApiReference(options => options
        .WithTitle("Client Search API")
        .WithTheme(ScalarTheme.Purple));

    app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
    app.MapGet("/health", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }));

    app.MapClientEndpoints();

    using (var scope = app.Services.CreateScope())
    {
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        var searchService = scope.ServiceProvider.GetRequiredService<IClientSearchService>();

        try
        {
            await initializer.InitializeAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not initialize Postgres schema on startup; will retry on first request");
        }

        try
        {
            await searchService.EnsureIndexExistsAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not ensure Elasticsearch index exists on startup");
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
