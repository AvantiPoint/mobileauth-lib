using Microsoft.AspNetCore.Http;

namespace AvantiPoint.MobileAuth.Http;

internal static class HttpContextHelpers
{
    public static Task StatusCode(this HttpContext context, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.Body.FlushAsync();
    }

    public static Task Ok(this HttpContext context)
            => context.StatusCode(StatusCodes.Status200OK);

    public static Task BadRequest(this HttpContext context)
            => context.StatusCode(StatusCodes.Status400BadRequest);

    public static Task NoContent(this HttpContext context)
            => context.StatusCode(StatusCodes.Status204NoContent);
}
