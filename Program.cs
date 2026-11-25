using SampleRAG.Options;
using SampleRAG.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddOptions<FoundryOptions>()
    .Bind(builder.Configuration.GetSection(FoundryOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<AzureSearchOptions>()
    .Bind(builder.Configuration.GetSection(AzureSearchOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<DocumentsOptions>()
    .Bind(builder.Configuration.GetSection(DocumentsOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(o => o.ChunkOverlap < o.ChunkSize, "Documents:ChunkOverlap must be less than Documents:ChunkSize.")
    .ValidateOnStart();

builder.Services.AddSingleton<FoundryService>();
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<AzureSearchService>();
builder.Services.AddSingleton<SearchIndexService>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<DocumentIngestionService>();
builder.Services.AddScoped<RAGService>();

var app = builder.Build();

// Ensure the index exists and ingest local PDFs before accepting requests.
using (var scope = app.Services.CreateScope())
{
    var indexService = scope.ServiceProvider.GetRequiredService<SearchIndexService>();
    await indexService.CreateIndexIfNotExistsAsync();

    var ingestionService = scope.ServiceProvider.GetRequiredService<DocumentIngestionService>();
    await ingestionService.ProcessDocumentsAsync(CancellationToken.None);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "SampleRAG API v1"));
}

app.MapControllers();
app.Run();
