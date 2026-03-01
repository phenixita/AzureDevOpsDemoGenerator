namespace AzdoGenCli.Models;

public sealed record DeleteProjectRequest(
    string OrganizationName,
    string ProjectName,
    string AccessToken,
    bool Force);
