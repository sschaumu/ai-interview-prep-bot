using System.ComponentModel.DataAnnotations;

namespace InterviewPrepBot.Infrastructure.Embeddings;

public sealed class EmbeddingProviderOptions
{
    public const string Section = "Providers:Embedding";

    [Required] public string Type { get; set; } = "ollama";
    [Required] public string Model { get; set; } = "nomic-embed-text";
    [Required] public string Endpoint { get; set; } = "http://localhost:11434";
    public string? ApiKey { get; set; }
    [Range(1, 65536)] public int Dimensions { get; set; } = 768;
}
