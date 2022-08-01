using Microsoft.AspNetCore.Builder;

namespace WinTenDev.Zizi.Extensions;

public static class HttpRequestMiddlewareExtension
{
    public static IApplicationBuilder UseRequestTimestamp(this IApplicationBuilder app)
    {
        return app.Use(async
        (
            context,
            next
        ) => {
            context.Items.Add("RequestStartedOn", DateTime.UtcNow);
            await next();
        });
    }
}