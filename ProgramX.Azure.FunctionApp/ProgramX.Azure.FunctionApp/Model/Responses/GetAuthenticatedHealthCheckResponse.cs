using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class GetAuthenticatedHealthCheckResponse : GetHealthCheckResponse
{
    
    [JsonPropertyName("isAuthenticated")]
    public virtual bool IsAuthenticated => true;

    
}