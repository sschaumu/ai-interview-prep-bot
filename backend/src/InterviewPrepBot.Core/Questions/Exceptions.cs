namespace InterviewPrepBot.Core.Questions;

public sealed class DuplicateQuestionException() : Exception("duplicate-question");

public sealed class ProviderUnavailableException(Exception? inner = null) : Exception("provider-unavailable", inner);

public sealed class InvalidRequestException(string message) : Exception(message);
