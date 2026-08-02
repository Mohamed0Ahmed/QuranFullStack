using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Queries.GetTemplates;

public sealed class GetTemplatesHandler(
    ILogger<GetTemplatesHandler> logger,
    IAbwabTemplatesReader reader)
{
    private const string FeatureName = "Abwab";
    private const string OperationName = "GetTemplates";

    public async Task<GetTemplatesOutcome> HandleAsync(
        GetTemplatesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var templates = await reader.GetAllAsync(cancellationToken);

        logger.LogInformation(
            "Completed {feature} {operation} {templateCount}",
            FeatureName, OperationName, templates.Count);

        return new GetTemplatesOutcome.Success(templates);
    }
}
