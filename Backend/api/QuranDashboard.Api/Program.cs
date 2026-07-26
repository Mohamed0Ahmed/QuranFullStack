using QuranDashboard.Api.Abwab;
using QuranDashboard.Api.Extensions;
using QuranDashboard.Application;
using QuranDashboard.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAbwabStabilization();
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

app.UseApiPipeline();

app.Run();

public partial class Program;
