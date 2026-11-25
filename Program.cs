using SampleRAG.Options;
using SampleRAG.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<DocumentIngestionService>();

// One embedding client is shared by startup ingestion and by question answering.
builder.Services.AddSingleton<EmbeddingService>(sp =>
    new EmbeddingService(sp.GetRequiredService<FoundryService>().Embeddings));
builder.Services.AddSingleton<VectorSearchService>();
builder.Services.AddSingleton<SearchIndexService>();

builder.Services.AddScoped<RAGService>();

// Fail at startup when the Foundry section is missing required values.
builder.Services.AddOptions<FoundryOptions>()
    .Bind(builder.Configuration.GetSection(FoundryOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Chat and embedding clients are created once and reused for the life of the process.
builder.Services.AddSingleton<FoundryService>();

var app = builder.Build();

// Index the local PDFs before the API accepts traffic.
// A scope is required because document ingestion is registered as scoped.
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
