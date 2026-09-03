using QuranDashboard.Api.Extensions;
using QuranDashboard.Api.Testing.DatabaseActivity;
using QuranDashboard.Application;
using QuranDashboard.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var databaseActivityPolicy = TestingDatabaseActivityPolicyResolver.Resolve(
    builder.Configuration,
    builder.Environment);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, databaseActivityPolicy);
builder.Services.AddApiServices(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseApiPipeline();

await app.SynchronizePermissionCatalogueAsync();

app.Run();
