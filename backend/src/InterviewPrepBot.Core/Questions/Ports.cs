namespace InterviewPrepBot.Core.Questions;

public enum EmbeddingIntent { Document, Query }

/// <summary>An embedding plus the profile string that describes how it was produced.</summary>
public sealed record QuestionEmbedding(float[] Vector, string Profile);

/// <summary>Core passes plain text plus an intent; model-specific handling lives in Infrastructure.</summary>
public interface IQuestionEmbedder
{
    /// <exception cref="ProviderUnavailableException">The Provider could not produce an embedding.</exception>
    Task<QuestionEmbedding> EmbedAsync(string text, EmbeddingIntent intent, CancellationToken ct);
}

/// <summary>The only writer of Questions.</summary>
public interface IQuestionBank
{
    /// <exception cref="DuplicateQuestionException"/>
    /// <exception cref="ProviderUnavailableException"/>
    Task<Question> AddAsync(string text, string? referenceAnswer, CancellationToken ct);
}
