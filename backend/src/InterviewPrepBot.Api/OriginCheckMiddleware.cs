using Microsoft.Extensions.Options;

namespace InterviewPrepBot.Api;

/// <summary>Every non-GET request must carry an Origin listed in Security:AllowedOrigins (AD-17).</summary>
public sealed class OriginCheckMiddleware(RequestDelegate next, IOptions<SecurityOptions> options)
{
    private readonly HashSet<string> _allowed =
        new(options.Value.AllowedOrigins.Select(o => o.TrimEnd('/')), StringComparer.OrdinalIgnoreCase);

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        if (!HttpMethods.IsGet(method) && !HttpMethods.IsHead(method))
        {
            var origin = context.Request.Headers.Origin.ToString().TrimEnd('/');
            if (origin.Length == 0 || !_allowed.Contains(origin))
            {
                await Problems.Write(context, StatusCodes.Status403Forbidden, "invalid-request");
                return;
            }
        }

        await next(context);
    }
}
