using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Application.Linking.Commands.ConfirmLinkingOperation;

public sealed record ConfirmLinkingOperationCommand(
    AuthorizationState Actor,
    LinkingOperationRequest Request);
