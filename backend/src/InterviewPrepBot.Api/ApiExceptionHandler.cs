using InterviewPrepBot.Core.Questions;
using Microsoft.AspNetCore.Diagnostics;

namespace InterviewPrepBot.Api;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, slug) = exception switch
        {
            DuplicateQuestionException => (StatusCodes.Status409Conflict, "duplicate-question"),
            ProviderUnavailableException => (StatusCodes.Status503ServiceUnavailable, "provider-unavailable"),
            InvalidRequestException or BadHttpRequestException => (StatusCodes.Status400BadRequest, "invalid-request"),
            _ => (StatusCodes.Status500InternalServerError, "internal-error"),
        };

        // Log the exception type only: messages can echo user content.
        logger.LogWarning("Request failed: {ExceptionType} -> {Slug}", exception.GetType().Name, slug);
        await Problems.Write(context, status, slug);
        return true;
    }
}
