using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class HttpResponseDataFactory
{
    private static readonly JsonSerializerOptions DefaultJsonOptions = new JsonSerializerOptions 
    { 
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    
    public static async Task<HttpResponseData> CreateForBadRequest(HttpRequestData httpRequestData, string errorMessage)
    {
        var httpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.BadRequest);
        await httpResponseData.WriteStringAsync(errorMessage);
        return httpResponseData;
    }
    
    public static async Task<HttpResponseData> CreateForServerError(HttpRequestData httpRequestData, string errorMessage)
    {
        var httpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
        await httpResponseData.WriteStringAsync(errorMessage);
        return httpResponseData;
    }
    
    public static async Task<HttpResponseData> CreateForServerError(HttpRequestData httpRequestData, Exception exception)
    {
        var httpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
        await httpResponseData.WriteAsJsonAsync(new 
        {
            exception.Message,
        });
        return httpResponseData;
    }

    public static async Task<HttpResponseData> CreateForNotFound(HttpRequestData httpRequestData, string type)
    {
        var httpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.NotFound);
        await httpResponseData.WriteStringAsync($"{type} not found.");
        return httpResponseData;
    }

    public static async Task<HttpResponseData> CreateForSuccess(HttpRequestData httpRequestData)
    {
        var httpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.OK);
        return httpResponseData;
    }
    
    public static async Task<HttpResponseData> CreateForSuccess(HttpRequestData httpRequestData, object data)
    {
        var httpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.OK);
        var jsonString = JsonSerializer.Serialize(data, DefaultJsonOptions);
        
        httpResponseData.Headers.Add("Content-Type", "application/json");
        await httpResponseData.WriteStringAsync(jsonString);
        return httpResponseData;

    }
    
    
    public static HttpResponseData CreateForSuccessNoContent(HttpRequestData httpRequestData)
    {
        var httpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.NoContent);;
        return httpResponseData;
    }
    
 
    public static async Task<HttpResponseData> CreateForSuccess<T>(HttpRequestData httpRequestData,
        PagedResponse<T> pagedResponse)
    {
        var httpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.OK);
        if (!string.IsNullOrWhiteSpace(pagedResponse.ContinuationToken))
        {
            httpResponseData.Headers.Add("x-continuation-token", Uri.EscapeDataString(pagedResponse.ContinuationToken));
        }
        //await httpResponseData.WriteAsJsonAsync(pagedResponse);
        //var serializedPagedResponse = JsonSerializer.Serialize(pagedResponse);
        var serializedPagedResponse = JsonSerializer.Serialize(pagedResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        //httpResponseData.Headers..Headers.Add("Content-Type", "application/json");
        await httpResponseData.WriteStringAsync(serializedPagedResponse);
        return httpResponseData;
    }
    public async static Task<HttpResponseData> CreateForCreated(HttpRequestData httpRequestData, object created, string type, string uniqueId)
    {
        var httpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.Created);
        httpResponseData.Headers.Add("Location", new[] { $"{httpRequestData.Url}/{type}/{uniqueId}" });
        await httpResponseData.WriteAsJsonAsync(created);
        return httpResponseData;
    }

    public static async Task<HttpResponseData> CreateForUnauthorised(HttpRequestData httpRequestData)
    {
        var httpResponseData = httpRequestData.CreateResponse(System.Net.HttpStatusCode.Unauthorized);
        await httpResponseData.WriteStringAsync("Invalid Credentials or Unauthorised");
        return httpResponseData;
    }
}