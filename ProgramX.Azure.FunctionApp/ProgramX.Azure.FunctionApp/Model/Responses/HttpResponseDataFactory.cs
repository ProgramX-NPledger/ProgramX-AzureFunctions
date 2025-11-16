using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

/// <summary>
/// Creates HTTP Response Data objects.
/// </summary>
public class HttpResponseDataFactory
{
    private static readonly JsonSerializerOptions DefaultJsonOptions = new JsonSerializerOptions 
    { 
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Creates a BadRequest response.
    /// </summary>
    /// <param name="httpRequestData">The <see cref="HttpRequestData"/> to create the response from.</param>
    /// <param name="errorMessage">Error message to send back to the client.</param>
    /// <returns>Generated <see cref="HttpResponseData"/>.</returns>
    public static async Task<HttpResponseData> CreateForBadRequest(HttpRequestData httpRequestData, string errorMessage)
    {
        var httpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.BadRequest);
        await httpResponseData.WriteStringAsync(errorMessage);
        return httpResponseData;
    }
    
    /// <summary>
    /// Creates a Conflict response.
    /// </summary>
    /// <param name="httpRequestData">The <see cref="HttpRequestData"/> to create the response from.</param>
    /// <param name="errorMessage">Error message to send back to the client.</param>
    /// <returns>Generated <see cref="HttpResponseData"/>.</returns>
    public static async Task<HttpResponseData> CreateForConflict(HttpRequestData httpRequestData, string errorMessage)
    {
        var httpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.Conflict);
        await httpResponseData.WriteStringAsync(errorMessage);
        return httpResponseData;
    }
    
    /// <summary>
    /// Creates a TooManyRequests response.
    /// </summary>
    /// <param name="httpRequestData">The <see cref="HttpRequestData"/> to create the response from.</param>
    /// <returns>Generated <see cref="HttpResponseData"/>.</returns>
    public static Task<HttpResponseData> CreateForTooManyRequests(HttpRequestData httpRequestData)
    {
        var httpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.TooManyRequests);
        return Task.FromResult(httpResponseData);
    }

    /// <summary>
    /// Creates a ServerError response.
    /// </summary>
    /// <param name="httpRequestData">The <see cref="HttpRequestData"/> to create the response from.</param>
    /// <param name="errorMessage">Error message to send back to the client.</param>
    /// <returns>Generated <see cref="HttpResponseData"/>.</returns>
    public static async Task<HttpResponseData> CreateForServerError(HttpRequestData httpRequestData, string errorMessage)
    {
        var httpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
        await httpResponseData.WriteStringAsync(errorMessage);
        return httpResponseData;
    }

    /// <summary>
    /// Creates a ServerError response.
    /// </summary>
    /// <param name="httpRequestData">The <see cref="HttpRequestData"/> to create the response from.</param>
    /// <param name="exception">The exception that was caused.</param>
    /// <returns>Generated <see cref="HttpResponseData"/>.</returns>
    public static async Task<HttpResponseData> CreateForServerError(HttpRequestData httpRequestData, Exception exception)
    {
        var httpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
        await httpResponseData.WriteAsJsonAsync(new 
        {
            exception.Message,
        });
        return httpResponseData;
    }

    /// <summary>
    /// Creates a NotFound response.
    /// </summary>
    /// <param name="httpRequestData">The <see cref="HttpRequestData"/> to create the response from.</param>
    /// <param name="type">Name of the type of the item that was not found.</param>
    /// <returns>Generated <see cref="HttpResponseData"/>.</returns>
    public static async Task<HttpResponseData> CreateForNotFound(HttpRequestData httpRequestData, string type)
    {
        var httpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.NotFound);
        await httpResponseData.WriteStringAsync($"{type} not found.");
        return httpResponseData;
    }

    /// <summary>
    /// Creates a Success response.
    /// </summary>
    /// <param name="httpRequestData">The <see cref="HttpRequestData"/> to create the response from.</param>
    /// <returns>Generated <see cref="HttpResponseData"/>.</returns>
    public static Task<HttpResponseData> CreateForSuccess(HttpRequestData httpRequestData)
    {
        var httpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.OK);
        return Task.FromResult(httpResponseData);
    }
    
    /// <summary>
    /// Creates a Success response.
    /// </summary>
    /// <param name="httpRequestData">The <see cref="HttpRequestData"/> to create the response from.</param>
    /// <param name="data">A JSON-serializable object to return to the client.</param>
    /// <returns>Generated <see cref="HttpResponseData"/>.</returns>
    public static async Task<HttpResponseData> CreateForSuccess(HttpRequestData httpRequestData, object data)
    {
        var httpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.OK);
        var jsonString = JsonSerializer.Serialize(data, DefaultJsonOptions);
        
        httpResponseData.Headers.Add("Content-Type", "application/json");
        await httpResponseData.WriteStringAsync(jsonString);
        return httpResponseData;

    }
    
    
    /// <summary>
    /// Creates a NoContent response.
    /// </summary>
    /// <param name="httpRequestData">The <see cref="HttpRequestData"/> to create the response from.</param>
    /// <returns>Generated <see cref="HttpResponseData"/>.</returns>
    public static HttpResponseData CreateForSuccessNoContent(HttpRequestData httpRequestData)
    {
        var httpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.NoContent);;
        return httpResponseData;
    }
    
 
    /// <summary>
    /// Creates a Success response for a paged result with a continuation token..
    /// </summary>
    /// <param name="httpRequestData">The <see cref="HttpRequestData"/> to create the response from.</param>
    /// <param name="pagedResponse">The <see cref="PagedResponse{T}"/> to return to the client.</param>
    /// <returns>Generated <see cref="HttpResponseData"/>.</returns>
    public static async Task<HttpResponseData> CreateForSuccess<T>(HttpRequestData httpRequestData,
        PagedResponse<T> pagedResponse)
    {
        var httpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.OK);
        if (!string.IsNullOrWhiteSpace(pagedResponse.ContinuationToken))
        {
            httpResponseData.Headers.Add("x-continuation-token", Uri.EscapeDataString(pagedResponse.ContinuationToken));
        }
        var serializedPagedResponse = JsonSerializer.Serialize(pagedResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await httpResponseData.WriteStringAsync(serializedPagedResponse);
        return httpResponseData;
    }

    /// <summary>
    /// Creates a Created response.
    /// </summary>
    /// <param name="httpRequestData">The <see cref="HttpRequestData"/> to create the response from.</param>
    /// <param name="created">The JSON-serializable object that was created.</param>
    /// <param name="type">Name of the type of object that was created.</param>
    /// <param name="uniqueId">Unique identifier of object that was created.</param>
    /// <returns>Generated <see cref="HttpResponseData"/>.</returns>
    public async static Task<HttpResponseData> CreateForCreated(HttpRequestData httpRequestData, object created, string type, string uniqueId)
    {
        var httpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.Created);
        httpResponseData.Headers.Add("Location", new[] { $"{httpRequestData.Url}/{type}/{uniqueId}" });
        var serializedCreated = JsonSerializer.Serialize(created, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await httpResponseData.WriteStringAsync(serializedCreated);
        return httpResponseData;
    }

    /// <summary>
    /// Creates an Unauthorised response.
    /// </summary>
    /// <param name="httpRequestData">The <see cref="HttpRequestData"/> to create the response from.</param>
    /// <returns>Generated <see cref="HttpResponseData"/>.</returns>
    public static async Task<HttpResponseData> CreateForUnauthorised(HttpRequestData httpRequestData)
    {
        var httpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.Unauthorized);
        await httpResponseData.WriteStringAsync("Invalid Credentials or Unauthorised");
        return httpResponseData;
    }
}