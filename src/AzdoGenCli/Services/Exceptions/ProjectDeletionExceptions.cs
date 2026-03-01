using System;
using System.Net;

namespace AzdoGenCli.Services.Exceptions;

public sealed class ProjectNotFoundException : Exception
{
    public string ProjectName { get; }
    public ProjectNotFoundException(string projectName) 
        : base($"Error: Project {projectName} not found") 
    {
        ProjectName = projectName;
    }
}

public sealed class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException(string message) : base(message) { }
}

public sealed class AuthorizationFailedException : Exception
{
    public AuthorizationFailedException(string message) : base(message) { }
}

public sealed class ProjectDeletionFailedException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public ProjectDeletionFailedException(HttpStatusCode statusCode, string message) 
        : base(message)
    {
        StatusCode = statusCode;
    }
}
