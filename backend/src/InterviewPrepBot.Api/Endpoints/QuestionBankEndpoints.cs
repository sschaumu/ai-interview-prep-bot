using InterviewPrepBot.Core.Questions;

namespace InterviewPrepBot.Api.Endpoints;

public sealed record AddQuestionRequest(string? Text, string? ReferenceAnswer);

public sealed record QuestionDto(Guid Id, string Text, string? ReferenceAnswer, DateTimeOffset CreatedAt)
{
    public static QuestionDto From(Question q) => new(q.Id, q.Text, q.ReferenceAnswer, q.CreatedAt);
}

public static class QuestionBankEndpoints
{
    public static void MapQuestionBankEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/question-bank");

        group.MapPost("/questions", async (AddQuestionRequest? request, AddQuestion addQuestion, CancellationToken ct) =>
        {
            var question = await addQuestion.ExecuteAsync(request?.Text, request?.ReferenceAnswer, ct);
            return TypedResults.Created($"/api/question-bank/questions/{question.Id}", QuestionDto.From(question));
        });
    }
}
