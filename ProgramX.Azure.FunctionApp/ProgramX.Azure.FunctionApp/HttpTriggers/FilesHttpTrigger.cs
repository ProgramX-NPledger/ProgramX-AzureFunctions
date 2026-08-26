using System.Diagnostics;
using System.Net;
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
    private readonly MultiPartContentHandler _multiPartContentHandler;


    public FilesHttpTrigger(ILogger<UsersHttpTrigger> logger,
        IStorageClient? storageClient,
        IConfiguration configuration,
        IEmailSender emailSender,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        MultiPartContentHandler multiPartContentHandler) : base(configuration, logger)
    {
        _logger = logger;
        _storageClient = storageClient;
        _emailSender = emailSender;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _multiPartContentHandler = multiPartContentHandler;
    }


    
    [Function(nameof(GetFile))]
    public async Task<HttpResponseData> GetFile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "files/{*fileName}")] HttpRequestData httpRequestData,
        string fileName,
        int? width,
        int? height,
        int? maximumWidth,
        int? maximumHeight)
    {
        return await RequiresAuthentication(httpRequestData, null, async (_, _) =>
        {
            // does the file already exist? If so, serve it
            // filename is in pattern <storage-folder>/<guid.ext>/<filename.ext>
            var splitFileName = fileName.Split('/');
            if (splitFileName.Length != 3)
            {
                return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, $"Invalid file name. Expected exactly 3 portions in path, but got {splitFileName.Length}");
            }

            var storageFolder = await _storageClient.GetStorageFolderAsync(splitFileName[0]);
            
            // construct filename according to requirements
            var requiredFileName = DeriveFileNameFromRequirements(splitFileName[2], width, height, maximumWidth, maximumHeight);

            var blobIndexEntry = await ReadBlobIndexEntryAsync(splitFileName[1], storageFolder);
            if (blobIndexEntry == null)
            {
                return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, $"Blob index entry not found for file {fileName}");
            }
            
            var response = httpRequestData.CreateResponse(HttpStatusCode.OK);
            var file = await storageFolder.GetStorageFileAsync(requiredFileName);
            if (file == null)
            {
                // verify an image is being requested
                var validImageExtensions = new string[]
                {
                    ".jpg",
                    ".gif",
                    ".png"
                };

                if (!validImageExtensions.Contains(Path.GetExtension(splitFileName[2].ToLower())))
                {
                    return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Attempted to resize a file that is not an image.");
                }

                // file does not exist, so needs to be created
                
                // get the original file, if it doesn't exist, return a 404
                var extension = Path.GetExtension(splitFileName[2]);
                var originalFile = await storageFolder.GetStorageFileAsync($"{splitFileName[1]}/original{extension}");
                if (originalFile == null)
                {
                    return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "File");
                }

                // get the stream into a byte array
                byte[] originalImage;
                using (var memoryStream = new MemoryStream())
                {
                    await originalFile.Content.CopyToAsync(memoryStream);
                    originalImage = memoryStream.ToArray();
                }
                
                // identify the required dimensions
                int? targetWidth = width ?? null;
                int? targetHeight = height ?? null;
                var resizedImageBytes = await ResizeAsync(originalImage, targetWidth, targetHeight, maximumWidth, maximumHeight);
                
                // save the resized image to storage
                using (var memoryStream = new MemoryStream(resizedImageBytes))
                {
                    var resizedFileName = $"{splitFileName[1]}/{requiredFileName}";
                    await storageFolder.SaveFileAsync(resizedFileName, memoryStream, blobIndexEntry.ContentType);
                    
                    // TODO: get roles of user
                    if (!await IsAuthorisedToReadFile(storageFolder, splitFileName[1], [], blobIndexEntry))
                    {
                        return await HttpResponseDataFactory.CreateForForbidden(httpRequestData, "File");
                    }

                    await memoryStream.CopyToAsync(response.Body);
                }
            }
            else
            {
                // get roles of user
                if (!await IsAuthorisedToReadFile(storageFolder, splitFileName[1], [], blobIndexEntry))
                {
                    return await HttpResponseDataFactory.CreateForForbidden(httpRequestData, "File");
                }
                await file.Content.CopyToAsync(response.Body);
            }
            
            // serve the file

            response.Headers.Add("Content-Type", blobIndexEntry.ContentType);
            response.Headers.Add("Content-Disposition", $"inline; filename=\"{fileName}\"");
            
            return response;

        }, true);
    }

    private async Task<bool> IsAuthorisedToReadFile(IStorageFolder storageFolder, string folderNameContainingFile, IEnumerable<string> requiredRoles, BlobIndexEntry? blobIndexEntry)
    {
        // there should be a blobIndexEntry.json file present that contains the required roles
        if (blobIndexEntry == null) return false;
        
        return !blobIndexEntry.ReadRequiresRoles.Any() || 
                        requiredRoles.Any(r => blobIndexEntry.ReadRequiresRoles.Contains(r));
    }

    private async Task<BlobIndexEntry?> ReadBlobIndexEntryAsync(string folderNameContainingFile, IStorageFolder storageFolder)
    {
        var indexFileName = $"{folderNameContainingFile}/blobIndexEntry.json";
        var indexFile = await storageFolder.GetStorageFileAsync(indexFileName);
        if (indexFile == null) return null;
             
        var blobIndexEntry = await JsonSerializer.DeserializeAsync<BlobIndexEntry>(
            indexFile.Content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        return blobIndexEntry;
    }
    
    private string DeriveFileNameFromRequirements(string fileName, int? requiredWidth, int? requiredHeight, int? maximumWidth, int? maximumHeight)
    {
        var fileNameWithoutExtension = fileName.Contains('.') ? fileName.Substring(0, fileName.LastIndexOf('.')) : fileName;
        var stringBuilder = new StringBuilder(fileNameWithoutExtension);
        if (requiredWidth.HasValue)
        {
            stringBuilder.Append($"_w{requiredWidth}");
        }
        if (requiredHeight.HasValue)
        {
            stringBuilder.Append($"_h{requiredHeight}");
        }
        if (maximumWidth.HasValue)
        {
            stringBuilder.Append($"_mw{maximumWidth}");
        }
        if (maximumHeight.HasValue)
        {
            stringBuilder.Append($"_mh{maximumHeight}");
        }
        
        // append extension back on to stringBuilder
        var extension = fileName.Contains('.') ? fileName.Substring(fileName.LastIndexOf('.')) : string.Empty;
        stringBuilder.Append(extension);
        
        return stringBuilder.ToString();
    }


    [Function(nameof(DeleteFile))]
    public async Task<HttpResponseData> DeleteFile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "files/{fileName}")]
        HttpRequestData httpRequestData,
        string fileName)
    {
        return await RequiresAuthentication(httpRequestData, null,  async (usernameMakingTheChange, _) =>
        {
            // TODO Delete file
            // var user = await _userRepository.GetUserByUserNameAsync(userName);
            // if (user == null) return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "User");
            // await _userRepository.DeleteUserByIdAsync(user.Id);
            return HttpResponseDataFactory.CreateForSuccessNoContent(httpRequestData);
        });
    }
    
    [Function(nameof(CreateFile))]
    public async Task<HttpResponseData> CreateFile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "files/{filePurpose}")]
        HttpRequestData httpRequestData,
        string filePurpose,
        string? mustHaveAnyOfRoles)
    {
        return await RequiresAuthentication(httpRequestData, null, async (usernameMakingTheChange, _) =>
        {
            var readRequiresRoles = mustHaveAnyOfRoles?.Split(',') ?? [];

            using (_logger.BeginScope("Uploading files"))
            {
                IEnumerable<UploadedFile> uploadedFiles;
                try
                {
                    uploadedFiles =
                        (await _multiPartContentHandler.GetFileDataFromMultiPartContentAsync(httpRequestData)).ToList();
                    _logger.LogInformation("{NumberOfFiles} Files extracted from multi-part content",
                        uploadedFiles.Count());
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error extracting files from multi-part content");
                    return await HttpResponseDataFactory.CreateForServerError(httpRequestData,
                        "Failed to extract files from multi-part content");
                }

                FileUploader fileUploader = new FileUploader(_storageClient);
                await fileUploader.UploadFilesAsync(uploadedFiles);

                return await HttpResponseDataFactory.CreateForCreated(httpRequestData, new CreateFileResponse()
                {
                    FileNames = uploadedFiles.Select(q => q.OriginalFileName)
                }, "file", uploadedFiles.Select(q => q.OriginalFileName).First());
            }
        });
    }
    
    public static async Task<byte[]> ResizeAsync(byte[] input, int? targetWidth = null, int? targetHeight = null, int? maximumWidth = null, int? maximumHeight = null)
    {
        using var inStream = new MemoryStream(input);
        
        using var image = await Image.LoadAsync(inStream); // auto-detect format

        // Respect EXIF orientation
        image.Mutate(x => x.AutoOrient());

        int requiredWidth = maximumWidth.HasValue
            ? targetWidth.HasValue
                ? Math.Min(targetWidth.Value, maximumWidth.Value)
                : Math.Min(image.Width, maximumWidth.Value)
            : image.Width;
        int requiredHeight = maximumHeight.HasValue
            ? targetHeight.HasValue
                ? Math.Min(targetHeight.Value, maximumHeight.Value)
                : Math.Min(image.Height, maximumHeight.Value)
            : image.Height;

        // Maintain aspect ratio when only width or height is given
        // var size = targetHeight.HasValue
        //     ? new Size(targetWidth, targetHeight.Value)
        //     : new Size(targetWidth, 0); // height 0 -> preserve aspect

        var size = new Size(requiredWidth, requiredHeight);
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