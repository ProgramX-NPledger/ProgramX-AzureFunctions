using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Helpers;

/// <summary>
/// Provides HTTP Body utilities.
/// </summary>
public static class HttpBodyUtilities
{
    /// <summary>
    /// Returns the body of the request as a string.
    /// </summary>
    /// <param name="httpRequestData">The <see cref="HttpRequestData"/> that contains the body.</param>
    /// <returns>The HTTP body as a string.</returns>
    public static async Task<string> GetStringFromHttpRequestDataBodyAsync(HttpRequestData httpRequestData)
    {
        httpRequestData.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpRequestData.Body);
        return await reader.ReadToEndAsync();
    }

    /// <summary>
    /// Returns the deserialized form of Type parameter T from the request body.
    /// </summary>
    /// <param name="httpRequestData">The <see cref="HttpRequestData"/> that contains the body.</param>
    /// <param name="throwIfNull">Set to <c>True</c> is an exception is required if the deserialization fails.</param>
    /// <typeparam name="T">The Type of the item to deserialize.</typeparam>
    /// <returns>The deserialized form of the HTTP body.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the data in the body could not be deserialized.</exception>
    public static async Task<T?> GetDeserializedJsonFromHttpRequestDataBodyAsync<T>(HttpRequestData httpRequestData, bool throwIfNull = false)
    {
        var serialised = await GetStringFromHttpRequestDataBodyAsync(httpRequestData);
        var deserialised = JsonSerializer.Deserialize<T>(serialised); 
        if (deserialised==null && throwIfNull) throw new InvalidOperationException("Could not deserialize json");
        return deserialised;
    }
}