using InterviewPrepBot.Core.Questions;

namespace InterviewPrepBot.Core.Tests;

public class QuestionTextTests
{
    [Theory]
    [InlineData("  What is   DI?  ", "what is di?")]
    [InlineData("Tab\tand\nnewline", "tab and newline")]
    [InlineData("ALREADY lower", "already lower")]
    public void Normalize_trims_collapses_whitespace_and_case_folds(string input, string expected) =>
        Assert.Equal(expected, QuestionText.Normalize(input));
}

public class AddQuestionTests
{
    private sealed class RecordingBank : IQuestionBank
    {
        public (string Text, string? Reference)? Added;

        public Task<Question> AddAsync(string text, string? referenceAnswer, CancellationToken ct)
        {
            Added = (text, referenceAnswer);
            return Task.FromResult(new Question(Guid.NewGuid(), text, referenceAnswer, DateTimeOffset.UnixEpoch));
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \t ")]
    public async Task Empty_text_is_rejected_without_touching_the_bank(string? text)
    {
        var bank = new RecordingBank();
        await Assert.ThrowsAsync<InvalidRequestException>(() =>
            new AddQuestion(bank).ExecuteAsync(text, null, TestContext.Current.CancellationToken));
        Assert.Null(bank.Added);
    }

    [Fact]
    public async Task Text_and_reference_are_trimmed_and_blank_reference_becomes_null()
    {
        var bank = new RecordingBank();
        await new AddQuestion(bank).ExecuteAsync("  Why?  ", "   ", TestContext.Current.CancellationToken);
        Assert.Equal(("Why?", (string?)null), bank.Added);
    }
}

public class LayeringTests
{
    [Fact]
    public void Core_references_only_Microsoft_Extensions_AI()
    {
        var referenced = typeof(Question).Assembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(n => !n.StartsWith("System") && n != "netstandard" && n != "mscorlib")
            .ToList();

        Assert.DoesNotContain(referenced, n => n.StartsWith("InterviewPrepBot.Infrastructure") || n.StartsWith("InterviewPrepBot.Api"));
        Assert.All(referenced, n => Assert.StartsWith("Microsoft.Extensions.AI", n));
    }
}
