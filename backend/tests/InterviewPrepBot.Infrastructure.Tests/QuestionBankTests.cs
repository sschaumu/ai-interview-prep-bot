using Microsoft.Extensions.DependencyInjection;
using InterviewPrepBot.Core.Questions;
using InterviewPrepBot.Infrastructure.Embeddings;
using InterviewPrepBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace InterviewPrepBot.Infrastructure.Tests;

public sealed class FakeEmbedder : IQuestionEmbedder
{
    public bool Fail { get; set; }
    public int Calls { get; private set; }

    public Task<QuestionEmbedding> EmbedAsync(string text, EmbeddingIntent intent, CancellationToken ct)
    {
        Calls++;
        if (Fail) throw new ProviderUnavailableException();
        return Task.FromResult(new QuestionEmbedding([1f, 2f, 3f], "fake:model:3:none"));
    }
}

public sealed class SqliteFixture : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"ipb-{Guid.NewGuid():N}.db");

    public IDbContextFactory<AppDbContext> Factory { get; }

    public SqliteFixture()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<AppDbContext>(o => o.UseSqlite($"Data Source={_path};Pooling=False"));
        Factory = services.BuildServiceProvider().GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var db = Factory.CreateDbContext();
        db.Database.Migrate();
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var f in new[] { _path, _path + "-wal", _path + "-shm" })
            if (File.Exists(f)) File.Delete(f);
    }
}

public class QuestionBankTests : IDisposable
{
    private readonly SqliteFixture _db = new();
    private readonly FakeEmbedder _embedder = new();
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private QuestionBank Bank() => new(_db.Factory, _embedder, TimeProvider.System);

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Migration_creates_question_table_with_expected_columns_and_unique_index()
    {
        await using var db = _db.Factory.CreateDbContext();
        await using var conn = db.Database.GetDbConnection();
        await conn.OpenAsync(_ct);

        var columns = new List<string>();
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT name FROM pragma_table_info('question')";
            await using var r = await cmd.ExecuteReaderAsync(_ct);
            while (await r.ReadAsync(_ct)) columns.Add(r.GetString(0));
        }
        Assert.Equal(
            new[] { "created_at", "embedding", "embedding_profile", "id", "normalized_text", "reference_answer", "text" },
            columns.Order().ToArray());

        await using var idx = conn.CreateCommand();
        idx.CommandText = "SELECT COUNT(*) FROM pragma_index_list('question') WHERE \"unique\" = 1";
        Assert.True(Convert.ToInt32(await idx.ExecuteScalarAsync(_ct)) >= 1);
    }

    [Fact]
    public async Task Add_persists_text_reference_embedding_and_profile()
    {
        var q = await Bank().AddAsync("What is DI?", "Inversion of control.", _ct);

        await using var db = _db.Factory.CreateDbContext();
        var row = await db.Questions.SingleAsync(_ct);
        Assert.Equal(q.Id, row.Id);
        Assert.Equal("What is DI?", row.Text);
        Assert.Equal("what is di?", row.NormalizedText);
        Assert.Equal("Inversion of control.", row.ReferenceAnswer);
        Assert.Equal("fake:model:3:none", row.EmbeddingProfile);
        Assert.Equal(3 * sizeof(float), row.Embedding!.Length);
    }

    [Fact]
    public async Task Duplicate_by_normalized_text_throws_and_does_not_embed()
    {
        await Bank().AddAsync("What is DI?", null, _ct);
        var callsBefore = _embedder.Calls;

        await Assert.ThrowsAsync<DuplicateQuestionException>(() => Bank().AddAsync("  what IS   di? ", null, _ct));

        Assert.Equal(callsBefore, _embedder.Calls);
        await using var db = _db.Factory.CreateDbContext();
        Assert.Equal(1, await db.Questions.CountAsync(_ct));
    }

    [Fact]
    public async Task Embedding_failure_saves_nothing()
    {
        _embedder.Fail = true;

        await Assert.ThrowsAsync<ProviderUnavailableException>(() => Bank().AddAsync("Anything?", null, _ct));

        await using var db = _db.Factory.CreateDbContext();
        Assert.Equal(0, await db.Questions.CountAsync(_ct));
    }
}

public class EmbeddingAdapterTests
{
    private sealed class CapturingGenerator : IEmbeddingGenerator<string, Embedding<float>>
    {
        public List<string> Inputs { get; } = [];
        public bool Throw { get; set; }

        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (Throw) throw new HttpRequestException("connection refused");
            Inputs.AddRange(values);
            return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>([new Embedding<float>(new float[] { 0.5f, 0.25f })]));
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }

    private static QuestionEmbedder Build(CapturingGenerator inner, bool prefixed = true)
    {
        IEmbeddingGenerator<string, Embedding<float>> generator = prefixed ? new PromptPrefixEmbeddingGenerator(inner) : inner;
        var options = Options.Create(new EmbeddingProviderOptions());
        return new QuestionEmbedder(generator, options, prefixed ? "nomic" : "none");
    }

    [Fact]
    public async Task Document_intent_gets_search_document_prefix_and_profile_is_reported()
    {
        var inner = new CapturingGenerator();
        var result = await Build(inner).EmbedAsync("What is DI?", EmbeddingIntent.Document, TestContext.Current.CancellationToken);

        Assert.Equal(["search_document: What is DI?"], inner.Inputs);
        Assert.Equal("ollama:nomic-embed-text:768:nomic", result.Profile);
        Assert.Equal([0.5f, 0.25f], result.Vector);
    }

    [Fact]
    public async Task Query_intent_gets_search_query_prefix()
    {
        var inner = new CapturingGenerator();
        await Build(inner).EmbedAsync("DI?", EmbeddingIntent.Query, TestContext.Current.CancellationToken);
        Assert.Equal(["search_query: DI?"], inner.Inputs);
    }

    [Fact]
    public async Task Generator_failure_surfaces_as_ProviderUnavailable()
    {
        var inner = new CapturingGenerator { Throw = true };
        await Assert.ThrowsAsync<ProviderUnavailableException>(() =>
            Build(inner).EmbedAsync("x", EmbeddingIntent.Document, TestContext.Current.CancellationToken));
    }
}
