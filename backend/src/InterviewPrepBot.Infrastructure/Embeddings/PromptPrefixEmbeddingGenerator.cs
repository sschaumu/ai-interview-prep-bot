using Microsoft.Extensions.AI;

namespace InterviewPrepBot.Infrastructure.Embeddings;

/// <summary>
/// Model-specific quirk isolated from Core: nomic-embed-text expects a task prefix on every input.
/// The intent arrives via <see cref="EmbeddingGenerationOptions.AdditionalProperties"/>.
/// </summary>
public sealed class PromptPrefixEmbeddingGenerator(IEmbeddingGenerator<string, Embedding<float>> inner)
    : DelegatingEmbeddingGenerator<string, Embedding<float>>(inner)
{
    public const string IntentKey = "intent";

    public override Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var isQuery = options?.AdditionalProperties?.TryGetValue(IntentKey, out var intent) == true
            && intent is string s && s == "query";
        var prefix = isQuery ? "search_query: " : "search_document: ";
        return base.GenerateAsync(values.Select(v => prefix + v), options, cancellationToken);
    }
}
