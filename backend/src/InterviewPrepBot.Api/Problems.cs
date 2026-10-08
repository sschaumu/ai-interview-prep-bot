using Microsoft.AspNetCore.Mvc;

namespace InterviewPrepBot.Api;

/// <summary>RFC 9457 ProblemDetails with a stable <c>type</c> slug (AD-15). Never carries exception text.</summary>
public static class Problems
{
    public static Task Write(HttpContext context, int status, string slug)
    {
        context.Response.StatusCode = status;
        return Results.Problem(new ProblemDetails { Type = slug, Status = status })
            .ExecuteAsync(context);
    }
}
