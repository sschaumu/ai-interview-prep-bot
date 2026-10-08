namespace InterviewPrepBot.Core.Questions;

public sealed class AddQuestion(IQuestionBank bank)
{
    public Task<Question> ExecuteAsync(string? text, string? referenceAnswer, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidRequestException("Question text is required.");

        var reference = string.IsNullOrWhiteSpace(referenceAnswer) ? null : referenceAnswer.Trim();
        return bank.AddAsync(text.Trim(), reference, ct);
    }
}
