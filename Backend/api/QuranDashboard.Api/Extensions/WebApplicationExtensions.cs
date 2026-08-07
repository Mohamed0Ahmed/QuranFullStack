using Microsoft.AspNetCore.Routing;
using QuranDashboard.Api.Authorization.Validation;

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
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.Services.GetRequiredService<UnsafeEndpointMetadataValidator>()
            .Validate(app.Services.GetServices<EndpointDataSource>().SelectMany(source => source.Endpoints));

        return app;
    }
}
