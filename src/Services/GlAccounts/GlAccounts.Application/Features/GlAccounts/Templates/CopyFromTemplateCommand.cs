namespace GlAccounts.Application.Features.GlAccounts.Templates;

public sealed record CopyFromTemplateCommand(
    string SourceAccountId,
    string NewAccountId,
    string Description,
    string UserId);
