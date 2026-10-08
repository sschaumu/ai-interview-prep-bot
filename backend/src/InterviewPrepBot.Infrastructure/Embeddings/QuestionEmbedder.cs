using InterviewPrepBot.Core.Questions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace InterviewPrepBot.Infrastructure.Embeddings;

/// <summary>Adapts <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> to Core's port and owns the profile string.</summary>
public sealed class QuestionEmbedder(
    IEmbeddingGenerator<string, Embedding<float>> generator,
    IOptions<EmbeddingProviderOptions> options,
    string promptScheme) : IQuestionEmbedder
{
    private readonly EmbeddingProviderOptions _o = options.Value;

    public string Profile => $"{_o.Type}:{_o.Model}:{_o.Dimensions}:{promptScheme}";

    public async Task<QuestionEmbedding> EmbedAsync(string text, EmbeddingIntent intent, CancellationToken ct)
    {
        var generationOptions = new EmbeddingGenerationOptions
        {
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                [PromptPrefixEmbeddingGenerator.IntentKey] = intent == EmbeddingIntent.Query ? "query" : "document",
            },
        };

        try
        {
            var result = await generator.GenerateAsync([text], generationOptions, ct);
            return new QuestionEmbedding(result[0].Vector.ToArray(), Profile);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ProviderUnavailableException(ex);
        }
    }
}
