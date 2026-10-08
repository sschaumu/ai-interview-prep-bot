using InterviewPrepBot.Api;
using InterviewPrepBot.Api.Endpoints;
using InterviewPrepBot.Core.Questions;
using InterviewPrepBot.Infrastructure.Embeddings;
using InterviewPrepBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OllamaSharp;

var builder = WebApplication.CreateBuilder(args);

// Options: validated at startup; secrets come from user-secrets (dev) or environment variables.
builder.Services.AddOptions<EmbeddingProviderOptions>()
    .BindConfiguration(EmbeddingProviderOptions.Section)
    .ValidateDataAnnotations()
    .Validate(o => o.Type == "ollama", "Providers:Embedding:Type must be 'ollama'.")
    .ValidateOnStart();
builder.Services.AddOptions<StorageOptions>()
    .BindConfiguration(StorageOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<SecurityOptions>()
    .BindConfiguration(SecurityOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Persistence.
var storagePath = StorageOptions.ResolvePath(
    builder.Configuration.GetSection(StorageOptions.Section).Get<StorageOptions>()?.Path ?? new StorageOptions().Path,
    builder.Environment.ContentRootPath);
Directory.CreateDirectory(Path.GetDirectoryName(storagePath)!);
builder.Services.AddDbContextFactory<AppDbContext>(o => o.UseSqlite($"Data Source={storagePath}"));
builder.Services.AddSingleton(TimeProvider.System);

// Embedding Provider: the one place a concrete adapter is constructed.
builder.Services.AddSingleton<IQuestionEmbedder>(sp =>
{
    var o = sp.GetRequiredService<IOptions<EmbeddingProviderOptions>>();
    IEmbeddingGenerator<string, Embedding<float>> generator =
        new OllamaApiClient(new Uri(o.Value.Endpoint), o.Value.Model);
    var nomic = o.Value.Model.StartsWith("nomic-embed-text", StringComparison.OrdinalIgnoreCase);
    if (nomic)
        generator = new PromptPrefixEmbeddingGenerator(generator);
    return new QuestionEmbedder(generator, o, nomic ? "nomic" : "none");
});

builder.Services.AddSingleton<IQuestionBank, QuestionBank>();
builder.Services.AddSingleton<AddQuestion>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseMiddleware<OriginCheckMiddleware>();

app.MapQuestionBankEndpoints();

app.Run();

public partial class Program;
