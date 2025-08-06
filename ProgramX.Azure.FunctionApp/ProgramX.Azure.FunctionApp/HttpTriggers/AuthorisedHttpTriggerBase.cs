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


    public async Task<HttpResponseBase> RequiresAuthentication(HttpRequestData httpRequestData, string? requiredRole, Func<Task<HttpResponseBase>> httpResponseDelegate)
    {
        if (!httpRequestData.Headers.Contains(AuthenticationHeaderName))
        {
            return new BadRequestHttpResponse(httpRequestData, "No authentication header was found.");
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
            return new ServerErrorHttpResponse(httpRequestData, exception);
        }

        if (!Auth.IsValid)
        {
            // this should redirect
            return new InvalidCredentialsOrUnauthorisedHttpResponse(httpRequestData);
        }

        return await httpResponseDelegate.Invoke();
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