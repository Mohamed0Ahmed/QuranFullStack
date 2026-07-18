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
        app.UseAuthentication();
        app.UseAuthorization();
        // UseRateLimiter sits after CORS (so preflight is handled by CORS and a 429 carries CORS
        // headers) and after the reserved auth slot (so per-user keying can read claims later).
        app.UseRateLimiter();
        app.MapControllers();

        return app;
    }
}
