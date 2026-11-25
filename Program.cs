using OpenAI.Embeddings;
using SampleRAG.Options;
using SampleRAG.Services;

var builder = WebApplication.CreateBuilder(args);

// Register API controllers, OpenAPI, and document processing services
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<DocumentIngestionService>();

builder.Services.AddSingleton<EmbeddingService>(sp =>
    new EmbeddingService(sp.GetRequiredService<FoundryService>().Embeddings));
builder.Services.AddSingleton<VectorSearchService>();
builder.Services.AddSingleton<SearchIndexService>();

builder.Services.AddScoped<RAGService>();

// Bind and validate Azure AI Foundry settings from the "Foundry" config section at startup
builder.Services.AddOptions<FoundryOptions>()
    .Bind(builder.Configuration.GetSection(FoundryOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Register FoundryService as a singleton so chat and embedding clients are reused
builder.Services.AddSingleton<FoundryService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var indexService = scope.ServiceProvider
        .GetRequiredService<SearchIndexService>();
    await indexService.CreateIndexIfNotExistsAsync();

    var ingestionService = scope.ServiceProvider
        .GetRequiredService<DocumentIngestionService>();
    await ingestionService.ProcessDocumentsAsync(
        CancellationToken.None);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "SampleRAG API v1");
    });
}

app.MapControllers();
app.Run();
