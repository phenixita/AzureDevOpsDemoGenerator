namespace AzdoGenCli.Models;

public sealed record DeleteProjectResult(
    string OrganizationName,
    string ProjectName,
    string ProjectId,
    bool DeletionAccepted,
    string? OperationId,
    string? OperationUrl);
