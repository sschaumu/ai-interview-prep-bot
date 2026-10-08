namespace InterviewPrepBot.Infrastructure.Persistence;

public sealed class QuestionEntity
{
    public Guid Id { get; set; }
    public string Text { get; set; } = "";
    public string NormalizedText { get; set; } = "";
    public string? ReferenceAnswer { get; set; }
    public byte[]? Embedding { get; set; }
    public string? EmbeddingProfile { get; set; }
    public DateTime CreatedAt { get; set; }
}
