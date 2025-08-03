using System.Security.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Host;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Responses;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public abstract class AuthorisedHttpTriggerBase 
{
    private const string AuthenticationHeaderName = "Authorization";

    // Access the authentication info.
    protected AuthenticationInfo Auth { get; private set; }


    public Task AssertAuthorisationAsync(HttpRequestData httpRequestData)
    {
        if (!httpRequestData.Headers.Contains(AuthenticationHeaderName))
        {
            return Task.FromException(new AuthenticationException("No authentication header was found."));
        }

        var authorisationHeader = httpRequestData.Headers.GetValues(AuthenticationHeaderName);
        var safeAuthorisationHeader = authorisationHeader.First();
        if (safeAuthorisationHeader.StartsWith("Bearer ")) safeAuthorisationHeader = safeAuthorisationHeader.Substring(7);
        
        try
        {
            Auth = new AuthenticationInfo(safeAuthorisationHeader);
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }

        if (!Auth.IsValid)
        {
            return Task.FromException(new KeyNotFoundException("No identity key was found in the claims."));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Post-execution filter.
    /// </summary>
    public Task OnExecutedAsync(FunctionExecutedContext executedContext, CancellationToken cancellationToken)
    {
        // Nothing.
        return Task.CompletedTask;
    }
}