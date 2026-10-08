using InterviewPrepBot.Core.Questions;
using Microsoft.EntityFrameworkCore;

namespace InterviewPrepBot.Infrastructure.Persistence;

public sealed class QuestionBank(
    IDbContextFactory<AppDbContext> dbFactory,
    IQuestionEmbedder embedder,
    TimeProvider clock) : IQuestionBank
{
    public async Task<Question> AddAsync(string text, string? referenceAnswer, CancellationToken ct)
    {
        var normalized = QuestionText.Normalize(text);

        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            if (await db.Questions.AnyAsync(q => q.NormalizedText == normalized, ct))
                throw new DuplicateQuestionException();
        }

        // Embed first, outside any transaction. A failure here saves nothing.
        var embedding = await embedder.EmbedAsync(text, EmbeddingIntent.Document, ct);

        var id = Guid.NewGuid();
        var createdAt = clock.GetUtcNow().UtcDateTime;
        var vector = ToBytes(embedding.Vector);

        await using var write = await dbFactory.CreateDbContextAsync(ct);
        await using var tx = await write.Database.BeginTransactionAsync(ct);
        var inserted = await write.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO question (id, text, normalized_text, reference_answer, embedding, embedding_profile, created_at)
            VALUES ({id}, {text}, {normalized}, {referenceAnswer}, {vector}, {embedding.Profile}, {createdAt})
            ON CONFLICT(normalized_text) DO NOTHING
            """, ct);
        if (inserted == 0)
            throw new DuplicateQuestionException();
        await tx.CommitAsync(ct);

        return new Question(id, text, referenceAnswer, new DateTimeOffset(createdAt, TimeSpan.Zero));
    }

    private static byte[] ToBytes(float[] vector)
    {
        var bytes = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
        return bytes;
    }
}
