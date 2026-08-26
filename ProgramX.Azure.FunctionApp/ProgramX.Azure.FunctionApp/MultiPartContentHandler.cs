using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Net.Http.Headers;
using ProgramX.Azure.FunctionApp.AzureStorage;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Exceptions;
using ProgramX.Azure.FunctionApp.Model.Responses;

namespace ProgramX.Azure.FunctionApp;

public class MultiPartContentHandler
{
    private readonly IStorageClient _storageClient;

    public MultiPartContentHandler(IStorageClient storageClient)
    {
        _storageClient = storageClient;
    }
    
    public async Task<IEnumerable<UploadedFile>> GetFileDataFromMultiPartContentAsync(HttpRequestData httpRequestData)
    {
        string multiPartContentBoundary = GetMultiPartBoundary(httpRequestData);
        
        // Copy to seekable buffer before any reading - reads entire stream into memory
        using var bodyStream = new MemoryStream();
        await httpRequestData.Body.CopyToAsync(bodyStream);
        bodyStream.Position = 0;
        
        var multipartReader = CreateMultipartReader(bodyStream, multiPartContentBoundary);
        
        MultipartSection? multipartSection;
        try
        {
            multipartSection = await multipartReader.ReadNextSectionAsync();
        }
        catch (IOException)
        {
            // incoming content has no multipart section
            throw new InvalidOperationException("Missing multipart section");
        }
        
        var uploadedFiles = new List<UploadedFile>();

        while (multipartSection != null)
        {
            if (ContentDispositionHeaderValue.TryParse(multipartSection.ContentDisposition, out var contentDisp)
                && contentDisp.DispositionType.Equals("form-data")
                && (!string.IsNullOrEmpty(contentDisp.FileName.Value) || !string.IsNullOrEmpty(contentDisp.FileNameStar.Value)))
            {
                var uploadedFile = new UploadedFile()
                {
                    OriginalFileName =
                        contentDisp.FileName.Value ?? contentDisp.FileNameStar.Value ?? "file", // eg. file.jpg
                    ContentType = multipartSection.ContentType ?? "application/octet-stream"
                };
                
                using var rawStream = new MemoryStream();
                await multipartSection.Body.CopyToAsync(rawStream);
                rawStream.Position = 0;

                uploadedFile.Data = rawStream.ToArray();
                
                uploadedFiles.Add(uploadedFile);
            }

            multipartSection = await multipartReader.ReadNextSectionAsync();
        }

        return uploadedFiles;
       
    }

    
    /// <summary>
    /// Asserts that the incoming HTTP request has a valid content type and headers.
    /// </summary>
    /// <param name="httpRequestData">The HTTP request data that will contain authentication data.</param>
    private static string GetMultiPartBoundary(HttpRequestData httpRequestData)
    {
        if (!httpRequestData.Headers.TryGetValues("Content-Type", out var ctValues))
        {
            throw new MediaHandlingException(MediaHandlingError.MissingContentTypeHeader);
        }

        var contentType = ctValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(contentType) || !contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
        {
            throw new MediaHandlingException(MediaHandlingError.InvalidContentTypeHeader);
            //return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Content-Type must be multipart/form-data.");
        }

        var mediaType = MediaTypeHeaderValue.Parse(contentType);
        var boundary = HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value;
        if (string.IsNullOrEmpty(boundary))
        {
            throw new MediaHandlingException(MediaHandlingError.MultipartContentBoundaryNotDefined);
            //return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Missing multipart boundary.");
        }

        return boundary;
    }

    /// <summary>
    /// Create a <see cref="MultipartReader"/> which can be used to read the multipart content from the request body.
    /// </summary>
    /// <param name="bodyStream">The body stream containing the multipart content.</param>
    /// <param name="multiPartContentBoundary">The multipart content boundary.</param>
    /// <returns>A <see cref="MultipartReader"/> which can be used to read the multipart content from the request body.</returns>
    private static MultipartReader CreateMultipartReader(Stream bodyStream, string multiPartContentBoundary)
    {
        var multipartReader = new MultipartReader(multiPartContentBoundary, bodyStream);
        return multipartReader;
    }
    

    private static string GetDataForMultipartSection(MultipartSection multipartSection)
    {
        // Reset position to the beginning if possible
        if (multipartSection.Body.CanSeek)
            multipartSection.Body.Position = 0;

        using var reader = new StreamReader(multipartSection.Body, Encoding.UTF8);
        return reader.ReadToEnd();
    }


}