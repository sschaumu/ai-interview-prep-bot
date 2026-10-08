namespace InterviewPrepBot.Core.Questions;

public sealed record Question(Guid Id, string Text, string? ReferenceAnswer, DateTimeOffset CreatedAt);
