using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using InterviewPrepBot.Core.Questions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InterviewPrepBot.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"ipb-api-{Guid.NewGuid():N}.db");

    public bool EmbedderFails { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Storage:Path", _dbPath);
        builder.UseSetting("Security:AllowedOrigins:0", "http://localhost:5173");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IQuestionEmbedder>();
            services.AddSingleton<IQuestionEmbedder>(new FakeEmbedder(this));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    private sealed class FakeEmbedder(ApiFactory owner) : IQuestionEmbedder
    {
        public Task<QuestionEmbedding> EmbedAsync(string text, EmbeddingIntent intent, CancellationToken ct) =>
            owner.EmbedderFails
                ? throw new ProviderUnavailableException()
                : Task.FromResult(new QuestionEmbedding([1f], "fake:m:1:none"));
    }
}

public class AddQuestionEndpointTests : IClassFixture<ApiFactory>
{
    private const string Url = "/api/question-bank/questions";
    private readonly ApiFactory _factory;

    public AddQuestionEndpointTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.EmbedderFails = false;
    }

    private static HttpRequestMessage Post(object body, string? origin = "http://localhost:5173")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Url) { Content = JsonContent.Create(body) };
        if (origin is not null) request.Headers.Add("Origin", origin);
        return request;
    }

    private static async Task<string?> ProblemType(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("type").GetString();
    }

    [Fact]
    public async Task Add_returns_201_with_the_created_question()
    {
        var client = _factory.CreateClient();
        var response = await client.SendAsync(Post(new { text = "What is a closure?", referenceAnswer = "A function plus its scope." }), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("What is a closure?", doc.RootElement.GetProperty("text").GetString());
        Assert.Equal("A function plus its scope.", doc.RootElement.GetProperty("referenceAnswer").GetString());
        Assert.True(doc.RootElement.TryGetProperty("createdAt", out _));
    }

    [Fact]
    public async Task Duplicate_returns_409_duplicate_question()
    {
        var client = _factory.CreateClient();
        await client.SendAsync(Post(new { text = "Duplicate me?" }), TestContext.Current.CancellationToken);
        var response = await client.SendAsync(Post(new { text = "  duplicate   ME? " }), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("duplicate-question", await ProblemType(response));
    }

    [Fact]
    public async Task Provider_failure_returns_503_provider_unavailable()
    {
        _factory.EmbedderFails = true;
        var response = await _factory.CreateClient().SendAsync(Post(new { text = "Unreachable provider?" }), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("provider-unavailable", await ProblemType(response));
    }

    [Fact]
    public async Task Blank_text_returns_400_invalid_request()
    {
        var response = await _factory.CreateClient().SendAsync(Post(new { text = "   " }), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid-request", await ProblemType(response));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("http://evil.example")]
    public async Task Non_get_without_an_allowed_origin_returns_403_invalid_request(string? origin)
    {
        var response = await _factory.CreateClient().SendAsync(Post(new { text = "Blocked?" }, origin), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("invalid-request", await ProblemType(response));
    }

    [Fact]
    public async Task No_endpoint_returns_configuration()
    {
        var client = _factory.CreateClient();
        foreach (var path in new[] { "/api/config", "/api/configuration", "/api/settings", "/config" })
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path, TestContext.Current.CancellationToken)).StatusCode);
    }
}
