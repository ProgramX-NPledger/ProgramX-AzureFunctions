using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using ProgramX.Azure.FunctionApp.AzureStorage;
using ProgramX.Azure.FunctionApp.Constants;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Constants;
using ProgramX.Azure.FunctionApp.Model.Criteria;
using ProgramX.Azure.FunctionApp.Model.Exceptions;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Model.Responses;
using ProgramX.Azure.FunctionApp.Model.Responses.Dtos;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using EmailMessage = ProgramX.Azure.FunctionApp.Model.EmailMessage;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class FilesHttpTrigger : AuthorisedHttpTriggerBase
{
    private readonly ILogger<UsersHttpTrigger> _logger;
    private readonly IStorageClient? _storageClient;
    private readonly IEmailSender _emailSender;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;


    public FilesHttpTrigger(ILogger<UsersHttpTrigger> logger,
        IStorageClient? storageClient,
        IConfiguration configuration,
        IEmailSender emailSender,
        IUserRepository userRepository,
        IRoleRepository roleRepository) : base(configuration, logger)
    {
        _logger = logger;
        _storageClient = storageClient;
        _emailSender = emailSender;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }


    
    [Function(nameof(GetFile))]
    public async Task<HttpResponseData> GetFile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "file/{imageType}/{fileName}")] HttpRequestData httpRequestData,
        string imageType,
        string fileName,
        int? w,
        int? h)
    {
        return await RequiresAuthentication(httpRequestData, null, async (userName, roles) =>
        {
            // find the location
            var storageFolder = await _storageClient!.GetStorageFolderAsync(imageType);
            var ext = Path.GetExtension(fileName);
            string blobFolder = fileName;
            string originalFileName = $"{blobFolder}/original{ext}";
            
            // get the security requirements
            
            var indexFileName = $"{blobFolder}/blobIndexEntry.json";
            var indexFile = await storageFolder.GetStorageFileAsync(indexFileName);
            if (indexFile == null) return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "File not found.");
            
             
            var blobIndexEntry = await JsonSerializer.DeserializeAsync<BlobIndexEntry>(
                indexFile.Content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            var isAllowed = blobIndexEntry.ReadRequiresRoles == null || 
                            !blobIndexEntry.ReadRequiresRoles.Any() || 
                            roles.Any(r => blobIndexEntry.ReadRequiresRoles.Contains(r));

            if (!isAllowed)
            {
                return await HttpResponseDataFactory.CreateForForbidden(httpRequestData, "You do not have permission to view this file.");
            }
            
            // identify if there is already a resized version
            var resizedFileName = $"{blobFolder}/";
            if (w.HasValue) resizedFileName += $"w{w.Value}";
            if (h.HasValue) resizedFileName += $"h{h.Value}";
            if (!w.HasValue && !h.HasValue) resizedFileName += "original";
            resizedFileName += ext;
            var resizedFile = await storageFolder.GetStorageFileAsync(resizedFileName);
            if (resizedFile == null)
            {
                // if not, resize it
                
            }
            else
            {
                // return the resized version    
            }


            return null; // TODO get file
        });
    }
    
    
    
    
    [Function(nameof(DeleteFile))]
    public async Task<HttpResponseData> DeleteFile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "user/{userName}")]
        HttpRequestData httpRequestData,
        string userName)
    {
        return await RequiresAuthentication(httpRequestData, null,  async (usernameMakingTheChange, _) =>
        {
            var user = await _userRepository.GetUserByUserNameAsync(userName);
            if (user == null) return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "User");
            await _userRepository.DeleteUserByIdAsync(user.Id);
            return HttpResponseDataFactory.CreateForSuccessNoContent(httpRequestData);
        });
    }
    
    [Function(nameof(CreateFile))]
    public async Task<HttpResponseData> CreateFile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "file/{imageType}/{fileName}")]
        HttpRequestData httpRequestData,
        string imageType,
        string fileName,
        string? mustHaveAnyOfRoles)
    {
        return await RequiresAuthentication(httpRequestData, null,  async (usernameMakingTheChange, _) =>
        {
            if (!httpRequestData.Headers.TryGetValues("Content-Type", out var ctValues))
            {
                return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Missing Content-Type header.");
            }

            var contentType = ctValues.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(contentType) || !contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Content-Type must be multipart/form-data.");
            }

            var mediaType = MediaTypeHeaderValue.Parse(contentType);
            var boundary = HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value;
            if (string.IsNullOrEmpty(boundary))
            {
                return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Missing multipart boundary.");
            }

            if (!IsValidImageType(imageType))
            {
                return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Invalid image type.");
            }

            var storageFolder = await _storageClient!.GetStorageFolderAsync(imageType);

            httpRequestData.Body.Position = 0;
            var multipartReader = new MultipartReader(boundary, httpRequestData.Body);
            MultipartSection? multipartSection;
            try
            {
                multipartSection = await multipartReader.ReadNextSectionAsync();
            }
            catch (IOException)
            {
                return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Missing multipart section.");
            }

            var fileNames = new List<string>();
            while (multipartSection != null)
            {
                if (ContentDispositionHeaderValue.TryParse(multipartSection.ContentDisposition, out var contentDisp)
                    && contentDisp.DispositionType.Equals("form-data")
                    && (!string.IsNullOrEmpty(contentDisp.FileName.Value) || !string.IsNullOrEmpty(contentDisp.FileNameStar.Value)))
                {
                    var originalName = contentDisp.FileName.Value ?? contentDisp.FileNameStar.Value ?? "file";
                    var ext = Path.GetExtension(originalName);

                    string blobFolder = originalName;
                    
                    string originalFileName = $"{blobFolder}/original{ext}";
                    
                    var base64EncodedData = DataForMultipartSection(multipartSection);
                    var rawData = Convert.FromBase64String(base64EncodedData);
                    
                    using var originalImageStream = new MemoryStream(rawData);
                    await storageFolder.SaveFileAsync($"{imageType}/{originalFileName}", originalImageStream, multipartSection.ContentType ?? "application/octet-stream");;

                    // create an index entry file
                    var blobIndexEntry = new BlobIndexEntry();
                    if (!string.IsNullOrEmpty(mustHaveAnyOfRoles))
                    {
                        var roles = mustHaveAnyOfRoles.Split(',');
                        blobIndexEntry.ReadRequiresRoles = roles;
                    }
                    else
                    {
                        blobIndexEntry.ReadRequiresRoles = new string[] { };
                    }
                    
                    var json = JsonSerializer.Serialize(blobIndexEntry, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    var jsonBytes = Encoding.ASCII.GetBytes(json);
                    using var blobIndexEntryMemoryStream = new MemoryStream(jsonBytes);
                    await storageFolder.SaveFileAsync($"{blobFolder}/blobIndexEntry.json", blobIndexEntryMemoryStream, "application/json");

                    fileNames.Add(originalFileName);
                }

                multipartSection = await multipartReader.ReadNextSectionAsync();
            }

            return await HttpResponseDataFactory.CreateForCreated(httpRequestData, new CreateFileResponse()
            {
                FileNames = fileNames
            },"file", fileNames.First());
        });
    }

    private bool IsValidImageType(string imageType)
    {
        var validImageTypes = new string[] { nameof(BlobNames.AvatarImages) };
        return validImageTypes.Contains(imageType, StringComparer.OrdinalIgnoreCase);
    }

    private string DataForMultipartSection(MultipartSection multipartSection)
    {
        // Reset position to the beginning if possible
        if (multipartSection.Body.CanSeek)
            multipartSection.Body.Position = 0;

        using var reader = new StreamReader(multipartSection.Body, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public static async Task<byte[]> ResizeAsync(byte[] input, int targetWidth, int? targetHeight = null)
    {
        using var inStream = new MemoryStream(input);
        
        using var image = await Image.LoadAsync(inStream); // auto-detect format

        // Respect EXIF orientation
        image.Mutate(x => x.AutoOrient());

        // Maintain aspect ratio when only width or height is given
        var size = targetHeight.HasValue
            ? new Size(targetWidth, targetHeight.Value)
            : new Size(targetWidth, 0); // height 0 -> preserve aspect

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = size,
            Mode = ResizeMode.Max,  // no upscaling beyond original
            Sampler = KnownResamplers.Lanczos3
        }));

        using var outStream = new MemoryStream();
        // Choose encoder based on desired output (JPEG here)
        var encoder = new JpegEncoder { Quality = 80 };
        await image.SaveAsync(outStream, encoder);
        return outStream.ToArray();
    }
    //
    //
    // [Function(nameof(RemoveUserPhoto))]
    // public async Task<HttpResponseData> RemoveUserPhoto(
    //     [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "user/{id}/photo")]
    //     HttpRequestData httpRequestData,
    //     string id)
    // {
    //     return await RequiresAuthentication(httpRequestData, null,  async (usernameMakingTheChange, _) =>
    //     {
    //         var user = await _userRepository.GetUserByIdAsync(id);
    //         if (user == null) return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "User");
    //
    //         Debug.Assert(_storageClient != null, nameof(_storageClient) + " != null");
    //         var storageFolder = await _storageClient.GetStorageFolderAsync(_storageClient.GetBlobName(BlobNames.AvatarImages));
    //         
    //         await storageFolder.DeleteFileAsync($"{usernameMakingTheChange}/{user.ProfilePhotographOriginal}");
    //         await storageFolder.DeleteFileAsync($"{usernameMakingTheChange}/{user.ProfilePhotographSmall}");
    //         
    //         // update record in DB
    //         user.ProfilePhotographSmall = null;
    //         user.ProfilePhotographOriginal = null;
    //         
    //         user.SchemaVersionNumber = user.SchemaVersionNumber > 2 ? user.SchemaVersionNumber : 2; // increment version number
    //         await _userRepository.UpdateUserAsync(user);
    //         
    //         return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new UpdateResponse()
    //         {
    //             ErrorMessage = null,
    //             IsOk = true,
    //             BytesTransferred = 0,
    //             HttpEventType = HttpEventType.Response,
    //             TotalBytesToTransfer = 0
    //         });
    //         
    //     });
    // }
    
    
}