using QuranDashboard.Api.Extensions;
using QuranDashboard.Application;
using QuranDashboard.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseApiPipeline();

await app.SynchronizePermissionCatalogueAsync();

app.Run();
