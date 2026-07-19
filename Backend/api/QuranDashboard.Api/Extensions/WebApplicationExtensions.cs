namespace QuranDashboard.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.RoutePrefix = "swagger";
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "QuranDashboard API v1");
            });
        }

        app.UseHttpsRedirection();
        app.UseCors("AngularDev");
        // The limiter keys per-client-IP, not per-user, so it belongs pre-auth — unauthenticated
        // traffic is rate-limited before it ever reaches authentication, and no per-user claim
        // keying is needed. It still sits after CORS so preflight OPTIONS is handled by CORS and a
        // 429 rejection carries CORS headers.
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}
