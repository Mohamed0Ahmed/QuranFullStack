using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Queries.GetTemplate;

public sealed class GetTemplateHandler(
    ILogger<GetTemplateHandler> logger,
    IAbwabTemplatesReader reader)
{
    private const string FeatureName = "Abwab";
    private const string OperationName = "GetTemplate";

    public async Task<GetTemplateOutcome> HandleAsync(
        GetTemplateQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var template = await reader.GetAsync(query.TemplateId, cancellationToken);
        if (template is null)
        {
            return new GetTemplateOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {templateId} {nodeCount}",
            FeatureName, OperationName, query.TemplateId, template.Nodes.Count);

        return new GetTemplateOutcome.Success(template);
    }
}
