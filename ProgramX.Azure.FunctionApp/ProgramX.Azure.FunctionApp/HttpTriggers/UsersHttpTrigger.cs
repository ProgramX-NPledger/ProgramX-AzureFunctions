using System.Net;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Azure;
using Azure.Communication.Email;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using ProgramX.Azure.FunctionApp.Constants;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Model.Responses;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class UsersHttpTrigger : AuthorisedHttpTriggerBase
{
    private readonly ILogger<UsersHttpTrigger> _logger;
    private readonly CosmosClient _cosmosClient;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly Container _container;

 
    public UsersHttpTrigger(ILogger<UsersHttpTrigger> logger,
        CosmosClient cosmosClient,
        BlobServiceClient blobServiceClient,
        IConfiguration configuration) : base(configuration)
    {
        if (configuration==null) throw new ArgumentNullException(nameof(configuration));
        
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));

        _container = _cosmosClient.GetContainer("core", "users");
    }


    
    [Function(nameof(GetUser))]
    public async Task<HttpResponseData> GetUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/{id?}")] HttpRequestData httpRequestData,
        string? id)
    {
        return await RequiresAuthentication(httpRequestData, null, async (userName, _) =>
        {
            if (id == null)
            {
                var continuationToken = httpRequestData.Query["continuationToken"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["continuationToken"]);
                var containsText = httpRequestData.Query["containsText"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["containsText"]);
                var withRoles = httpRequestData.Query["withRoles"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["withRoles"]).Split(new [] {','});
                var hasAccessToApplications = httpRequestData.Query["hasAccessToApplications"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["hasAccessToApplications"]).Split(new [] {','});

                var sortByColumn = httpRequestData.Query["sortBy"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["sortBy"]);
                var offset = UrlUtilities.GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["offset"]);
                var itemsPerPage = UrlUtilities.GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["itemsPerPage"]);
                
                var pagedCosmosDbUsersResults=await GetPagedMultipleItemsAsync(containsText,withRoles, hasAccessToApplications, sortByColumn ?? "c.userName",offset,itemsPerPage);
                var baseUrl =
                    $"{httpRequestData.Url.Scheme}://{httpRequestData.Url.Authority}{httpRequestData.Url.AbsolutePath}";
                var pageUrls = CalculatePageUrls(pagedCosmosDbUsersResults,
                    baseUrl,
                    containsText,
                    withRoles,
                    hasAccessToApplications,
                    continuationToken, 
                    offset ?? 0,
                    itemsPerPage ?? DataConstants.ItemsPerPage);
                
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new PagedResponse<SecureUser>(pagedCosmosDbUsersResults,pageUrls));
            }
            else
            {
                var user = await GetSingleItemAsync(id);
                if (user == null)
                {
                    return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "User");
                }
                
                List<Application> applications = user.roles.SelectMany(q=>q.applications).GroupBy(g=>g.name).Select(q=>q.First()).ToList();
                
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new
                {
                    user,
                    applications,
                    profilePhotoBase64 = string.Empty
                });
            }
        });
    }
    
    
    private async  Task<User?> GetSingleItemAsync(string name)
    {
        QueryDefinition queryDefinition = BuildQueryDefinition(name,null,null,null);
        
        var usersCosmosDbReader = new PagedCosmosDbReader<User>(_cosmosClient, DataConstants.CoreDatabaseName, DataConstants.UsersContainerName, DataConstants.UserNamePartitionKeyPath);
        PagedCosmosDbResult<User> pagedCosmosDbResult;
        pagedCosmosDbResult = await usersCosmosDbReader.GetPagedItemsAsync(queryDefinition,"c.id");
        
        return pagedCosmosDbResult.Items.FirstOrDefault();
    }

    
    
    private IEnumerable<UrlAccessiblePage> CalculatePageUrls(PagedCosmosDbResult<SecureUser> pagedCosmosDbRolesResults, 
        string baseUrl, 
        string? containsText, 
        IEnumerable<string>? withRoles, 
        IEnumerable<string>? hasAccessToApplications, 
        string? continuationToken,
        int offset=0, 
        int itemsPerPage=DataConstants.ItemsPerPage)
    {
        var totalPages = (int)Math.Ceiling((double)pagedCosmosDbRolesResults.TotalItems / itemsPerPage);
        var currentPageNumber = offset==0 ? 1 : (int)Math.Ceiling((offset+1.0) / itemsPerPage);
        
        List<UrlAccessiblePage> pageUrls = new List<UrlAccessiblePage>();
        for (var pageNumber = 1; pageNumber <= totalPages; pageNumber++)
        {
            pageUrls.Add(new UrlAccessiblePage()
            {
                Url = BuildPageUrl(baseUrl, containsText, withRoles, hasAccessToApplications, continuationToken, (pageNumber * itemsPerPage)-itemsPerPage, itemsPerPage),
                PageNumber = pageNumber,
                IsCurrentPage = pageNumber == currentPageNumber,
            });
        }
        return pageUrls;
    }
    
    
    private async Task<PagedCosmosDbResult<SecureUser>> GetPagedMultipleItemsAsync(string? containsText,
        string[]? withRoles,
        string[]? hasAccessToApplications,
        string sortByColumn,
        int? offset = 0,
        int? itemsPerPage = DataConstants.ItemsPerPage)
    {
        QueryDefinition queryDefinition = BuildQueryDefinition(null, containsText, withRoles, hasAccessToApplications);
        
        var usersCosmosDbReader = new PagedCosmosDbReader<SecureUser>(_cosmosClient, DataConstants.CoreDatabaseName, DataConstants.UsersContainerName, DataConstants.UserNamePartitionKeyPath);
        PagedCosmosDbResult<SecureUser> pagedCosmosDbResult = await usersCosmosDbReader.GetPagedItemsAsync(queryDefinition,sortByColumn,offset,itemsPerPage);
        
        return pagedCosmosDbResult;
    }
    
    
    private string BuildPageUrl(string baseUrl, string? containsText, IEnumerable<string>? withRoles, IEnumerable<string>? hasAccessToApplications, string continuationToken, int? offset, int? itemsPerPage)
    {
        var parametersDictionary = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(containsText))
        {
            parametersDictionary.Add("containsText", Uri.EscapeDataString(containsText));
        }

        if (withRoles != null && withRoles.Any())
        {
            parametersDictionary.Add("withRoles", Uri.EscapeDataString(string.Join(",", withRoles)));
        }

        if (hasAccessToApplications != null && hasAccessToApplications.Any())
        {
            parametersDictionary.Add("hasAccessToApplications", Uri.EscapeDataString(string.Join(",", hasAccessToApplications)));       
        }
        
        if (!string.IsNullOrWhiteSpace(continuationToken))
        {
            parametersDictionary.Add("continuationToken", Uri.EscapeDataString(continuationToken));
        }

        if (offset != null)
        {
            parametersDictionary.Add("offset",offset.Value.ToString());
        }

        if (itemsPerPage != null)
        {
            parametersDictionary.Add("itemsPerPage",itemsPerPage.Value.ToString());       
        }
        
        var sb=new StringBuilder(baseUrl);
        if (parametersDictionary.Any())
        {
            sb.Append("?");
            foreach (var param in parametersDictionary)
            {
                sb.Append($"{param.Key}={param.Value}&");
            }
            sb.Remove(sb.Length-1,1);
        }

        return sb.ToString();
    }
    
    

    

    private QueryDefinition BuildQueryDefinition(string? id, string? containsText, IEnumerable<string>? withRoles, IEnumerable<string>? hasAccessToApplications)
    {
        var sb = new StringBuilder("SELECT c.id, c.userName, c.emailAddress, c.roles,c.type,c.versionNumber,c.firstName,c.lastName,c.profilePhotographSmall,c.profilePhotographOriginal,c.theme,c.createdAt,c.updatedAt,c.lastLoginAt,c.lastPasswordChangeAt FROM c WHERE 1=1");
        var parameters = new List<(string name, object value)>();
        if (string.IsNullOrWhiteSpace(id))
        {
            if (!string.IsNullOrWhiteSpace(containsText))
            {
                sb.Append(@" AND (
                                CONTAINS(UPPER(c.userName), @containsText) OR 
                                CONTAINS(UPPER(c.emailAddress), @containsText) OR 
                                CONTAINS(UPPER(c.firstName), @containsText) OR 
                                CONTAINS(UPPER(c.lastName), @containsText)
                                )");
                parameters.Add(("@containsText", containsText.ToUpperInvariant()));
            }

            if (withRoles != null && withRoles.Any())
            {
                var conditions = new List<string>();
                var rolesList = withRoles.ToList();
            
                for (int i = 0; i < rolesList.Count; i++)
                {
                    conditions.Add($"EXISTS(SELECT VALUE r FROM r IN c.roles WHERE r.name = @role{i})");
                    parameters.Add(($"@role{i}", rolesList[i]));
                }
            
                sb.Append($" AND ({string.Join(" OR ", conditions)})");
            }

            if (hasAccessToApplications != null && hasAccessToApplications.Any())
            {
                var conditions = new List<string>();
                var applicationsList = hasAccessToApplications.ToList();
            
                for (int i = 0; i < applicationsList.Count; i++)
                {
                    conditions.Add(@$"EXISTS(SELECT VALUE r FROM r IN c.roles JOIN a IN r.applications WHERE a.name = @appname{i})");
                    parameters.Add(($"@appname{i}", applicationsList[i]));
                }
            
                sb.Append($" AND ({string.Join(" OR ", conditions)})");


            }
            
            //sb.Append(" ORDER BY c.userName");
            

        }
        else
        {
            sb.Append(" AND (c.id=@id OR c.userName=@id)");
            parameters.Add(("@id", id));
        }

        var queryDefinition = new QueryDefinition(sb.ToString());
        foreach (var param in parameters)
        {
            queryDefinition.WithParameter(param.name, param.value);
        }
        return queryDefinition;
        
    }
    
    
    [Function(nameof(DeleteUser))]
    public async Task<HttpResponseData> DeleteUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "user/{id}")]
        HttpRequestData httpRequestData,
        string id)
    {
        return await RequiresAuthentication(httpRequestData, null,  async (usernameMakingTheChange, _) =>
        {
            var pagedAndFilteredCosmosDbReader =
                new PagedCosmosDbReader<User>(_cosmosClient, DataConstants.CoreDatabaseName, DataConstants.UsersContainerName,DataConstants.UserNamePartitionKeyPath);

            QueryDefinition queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.id=@id OR c.userName=@id");
            queryDefinition.WithParameter("@id", id);
            var users = await pagedAndFilteredCosmosDbReader.GetNextItemsAsync(queryDefinition);
            var originalUser = users.Items.FirstOrDefault();
            if (originalUser == null)
            {
                return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "User");
            }

            try
            {
                var response = await _container.DeleteItemAsync<User>(originalUser.id, new PartitionKey(id));

                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    return HttpResponseDataFactory.CreateForSuccessNoContent(httpRequestData);
                }
            }
            catch (CosmosException e)
            {
                return await HttpResponseDataFactory.CreateForServerError(httpRequestData, "Error deleting user");
            }
        
            return await HttpResponseDataFactory.CreateForServerError(httpRequestData, "Failed to delete user");
        });
    }
    

    [Function(nameof(UpdateUser))]
    public async Task<HttpResponseData> UpdateUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "user/{id}")]
        HttpRequestData httpRequestData,
        string id)
    {
        var updateUserRequest =
            await HttpBodyUtilities.GetDeserializedJsonFromHttpRequestDataBodyAsync<UpdateUserRequest>(httpRequestData);
        if (updateUserRequest == null) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,"Invalid request body");

        var isChangePasswordRequest=updateUserRequest.updatePasswordScope 
                                    && updateUserRequest is { newPassword: not null, updateProfilePictureScope: false, updateProfileScope: false, updateRolesScope: false };
        
        return await RequiresAuthentication(httpRequestData, null,  async (usernameMakingTheChange, _) =>
        {
            // get the User to get the password hash
            var pagedAndFilteredCosmosDbReader =
                new PagedCosmosDbReader<User>(_cosmosClient, DataConstants.CoreDatabaseName, DataConstants.UsersContainerName,DataConstants.UserNamePartitionKeyPath);

            QueryDefinition queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.id=@id OR c.userName=@id");
            queryDefinition.WithParameter("@id", id);
            var users = await pagedAndFilteredCosmosDbReader.GetNextItemsAsync(queryDefinition);
            var originalUser = users.Items.FirstOrDefault();
            if (originalUser == null)
            {
                return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "User");
            }
            
            if (updateUserRequest.updateProfileScope)
            {
                if (originalUser.userName!=updateUserRequest.userName) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Cannot change the username because it is used for the Partition Key");
                if (!IsValidEmail(updateUserRequest.emailAddress!)) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Invalid email address");
                
                originalUser.emailAddress=updateUserRequest.emailAddress!;
                originalUser.firstName=updateUserRequest.firstName!;
                originalUser.lastName=updateUserRequest.lastName!;
            }

            if (updateUserRequest.updateSettingsScope)
            {
                originalUser.theme = updateUserRequest.theme;
                originalUser.versionNumber = originalUser.versionNumber >= 3 ? originalUser.versionNumber : 3; 
            }

            if (updateUserRequest.updatePasswordScope)
            {
                if (string.IsNullOrWhiteSpace(updateUserRequest.newPassword)) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Password cannot be empty");
                if (originalUser.userName!=updateUserRequest.userName) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Incorrect username");
                if (!string.IsNullOrEmpty(originalUser.passwordConfirmationNonce) && originalUser.passwordConfirmationNonce!=updateUserRequest.passwordConfirmationNonce) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Incorrect password confirmation nonce");
                if (originalUser.passwordLinkExpiresAt.HasValue && originalUser.passwordLinkExpiresAt.Value < DateTime.UtcNow) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Password confirmation link has expired");
                
                using var hmac = new HMACSHA512(originalUser.passwordSalt);
                var passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(updateUserRequest.newPassword));
                originalUser.passwordHash = passwordHash;
                originalUser.passwordSalt = hmac.Key;
                originalUser.passwordConfirmationNonce = null;
                originalUser.passwordLinkExpiresAt = null;
            }
            
            if (updateUserRequest.updateRolesScope)
            {
                originalUser.roles=updateUserRequest.roles;
            }

            if (updateUserRequest.updateProfilePictureScope)
            {
                return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Incorrect endpoint, use /user/{id}/photo instead");
            }
            
            var response = await _container.ReplaceItemAsync(originalUser, originalUser.id, new PartitionKey(updateUserRequest.userName));

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new UpdateResponse()
                {
                    errorMessage = null,
                    isOk = true
                });
            }
        
            return await HttpResponseDataFactory.CreateForServerError(httpRequestData, "Failed to update user");
        },isChangePasswordRequest);
    }
    
    
    [Function(nameof(UpdateUserPhoto))]
    public async Task<HttpResponseData> UpdateUserPhoto(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "user/{id}/photo")]
        HttpRequestData httpRequestData,
        string id)
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

            var pagedAndFilteredCosmosDbReader =
                new PagedCosmosDbReader<User>(_cosmosClient, DataConstants.CoreDatabaseName, DataConstants.UsersContainerName,DataConstants.UserNamePartitionKeyPath);

            QueryDefinition queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.id=@id OR c.userName=@id");
            queryDefinition.WithParameter("@id", id);
            var users = await pagedAndFilteredCosmosDbReader.GetNextItemsAsync(queryDefinition);
            var originalUser = users.Items.FirstOrDefault();
            if (originalUser == null)
            {
                return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "User");
            }
            
            var avatarImagesBlockContainerClient = _blobServiceClient.GetBlobContainerClient(BlobConstants.AvatarImagesBlobName);
            await avatarImagesBlockContainerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            httpRequestData.Body.Position = 0;
            var multipartReader = new MultipartReader(boundary, httpRequestData.Body);
            MultipartSection? multipartSection;
            try
            {
                multipartSection = await multipartReader.ReadNextSectionAsync();
            }
            catch (IOException e)
            {
                return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Missing multipart section.");
            }
            while (multipartSection != null)
            {
                if (ContentDispositionHeaderValue.TryParse(multipartSection.ContentDisposition, out var contentDisp)
                    && contentDisp.DispositionType.Equals("form-data")
                    && (!string.IsNullOrEmpty(contentDisp.FileName.Value) || !string.IsNullOrEmpty(contentDisp.FileNameStar.Value)))
                {
                    var originalName = contentDisp.FileName.Value ?? contentDisp.FileNameStar.Value ?? "file";
                    var originalNameWithoutExtension = Path.GetFileNameWithoutExtension(originalName);
                    var ext = Path.GetExtension(originalName);
                    
                    var originalBlobName = $"{originalNameWithoutExtension}-orig{ext}";
                    var avatarSizedBlobName = $"{originalNameWithoutExtension}-32x32{ext}";
                    
                    var base64EncodedData = DataForMultipartSection(multipartSection);
                    var rawData = Convert.FromBase64String(base64EncodedData);
                    
                    using var originalImageStream = new MemoryStream(rawData);
                    
                    await UploadBlob(avatarImagesBlockContainerClient, multipartSection,  originalImageStream,$"{usernameMakingTheChange}/{originalBlobName}");

                    // having moved the cursor to the end of the stream, it is reset
                    originalImageStream.Position = 0;
                    
                    // the stream is copied into an array, which can be seeked
                    var resizedImage = await ResizeAsync(rawData, 32);
                    
                    // the array is set back into a stream
                    var resizedImageStream = new MemoryStream(resizedImage);
                    
                    // upload to blob storage
                    await UploadBlob(avatarImagesBlockContainerClient, multipartSection, resizedImageStream, $"{usernameMakingTheChange}/{avatarSizedBlobName}");
                    
                    // update record in DB
                    originalUser.profilePhotographSmall = avatarSizedBlobName;
                    originalUser.profilePhotographOriginal = originalBlobName;
                    originalUser.versionNumber = originalUser.versionNumber > 2 ? originalUser.versionNumber : 2; // increment version number
                    var response = await _container.ReplaceItemAsync(originalUser, originalUser.id, new PartitionKey(originalUser.userName));
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return await HttpResponseDataFactory.CreateForServerError(httpRequestData, "Failed to update user");
                    }
                }
                // else: handle form fields if needed (e.g., section.AsFormData())

                multipartSection = await multipartReader.ReadNextSectionAsync();
            }

            return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new UpdateProfilePhotoResponse()
            {
                photoUrl = $"{avatarImagesBlockContainerClient.Uri}/{usernameMakingTheChange}/{originalUser.profilePhotographSmall}",
                errorMessage = null,
                isOk = true,
                bytesTransferred = 0,
                httpEventType = HttpEventType.Response,
                totalBytesToTransfer = 0
            });
            
        });
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

    

    private async Task UploadBlob(BlobContainerClient avatarImagesBlockContainerClient, MultipartSection multipartSection, Stream data, string fileName)
    {
        var blob = avatarImagesBlockContainerClient.GetBlobClient(fileName);
        var headers = new BlobHttpHeaders
        {
            ContentType = multipartSection.ContentType ?? "application/octet-stream"
        };

        // Stream directly to Blob Storage (no buffering in memory)
        await blob.UploadAsync(data, new BlobUploadOptions { HttpHeaders = headers });
    }

    private bool IsValidEmail(string email)
    {
        return Regex.IsMatch(
            email,
            RegExConstants.ValidEmailAddress,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(250) // avoid catastrophic backtracking
        );
    }
    
    
    [Function(nameof(RemoveUserPhoto))]
    public async Task<HttpResponseData> RemoveUserPhoto(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "user/{id}/photo")]
        HttpRequestData httpRequestData,
        string id)
    {
        return await RequiresAuthentication(httpRequestData, null,  async (usernameMakingTheChange, _) =>
        {
            var pagedAndFilteredCosmosDbReader =
                new PagedCosmosDbReader<User>(_cosmosClient, DataConstants.CoreDatabaseName, DataConstants.UsersContainerName,DataConstants.UserNamePartitionKeyPath);

            QueryDefinition queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.id=@id OR c.userName=@id");
            queryDefinition.WithParameter("@id", id);
            var users = await pagedAndFilteredCosmosDbReader.GetNextItemsAsync(queryDefinition);
            var originalUser = users.Items.FirstOrDefault();
            if (originalUser == null)
            {
                return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "User");
            }

            // delete files if they already exist
            var avatarImagesBlockContainerClient = _blobServiceClient.GetBlobContainerClient(BlobConstants.AvatarImagesBlobName);
            await avatarImagesBlockContainerClient.DeleteBlobIfExistsAsync($"{usernameMakingTheChange}/{originalUser.profilePhotographOriginal}");
            await avatarImagesBlockContainerClient.DeleteBlobIfExistsAsync($"{usernameMakingTheChange}/{originalUser.profilePhotographSmall}");
            
            // update record in DB
            originalUser.profilePhotographSmall = null;
            originalUser.profilePhotographOriginal = null;
            
            originalUser.versionNumber = originalUser.versionNumber > 2 ? originalUser.versionNumber : 2; // increment version number
            var response = await _container.ReplaceItemAsync(originalUser, originalUser.id, new PartitionKey(originalUser.userName));
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return await HttpResponseDataFactory.CreateForServerError(httpRequestData, "Failed to update user");
            }
            
            return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new UpdateResponse()
            {
                errorMessage = null,
                isOk = true,
                bytesTransferred = 0,
                httpEventType = HttpEventType.Response,
                totalBytesToTransfer = 0
            });
            
        });
    }
    
    [Function(nameof(CreateUser))]
    public async Task<HttpResponseData> CreateUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user")] HttpRequestData httpRequestData
    )
    {
        return await RequiresAuthentication(httpRequestData, null,  async (_, _) =>
        {
            var createUserRequest = await httpRequestData.ReadFromJsonAsync<CreateUserRequest>();
            if (createUserRequest==null) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Invalid request body");

            var rolesCosmosDbReader =
                new PagedCosmosDbReader<Role>(_cosmosClient, DataConstants.CoreDatabaseName, DataConstants.UsersContainerName, DataConstants.UserNamePartitionKeyPath);
            
            var queryDefinition= new QueryDefinition("SELECT r.name, r.description, r.applications FROM c JOIN r IN c.roles");
            var roles = await rolesCosmosDbReader.GetNextItemsAsync(queryDefinition,null,null);
            
            var allRoles = roles.Items.ToList(); // avoid multiple enumeration
        
            var newUser = new User()
            {
                id = Guid.NewGuid().ToString("N"),
                emailAddress = createUserRequest.emailAddress,
                userName = createUserRequest.userName,
                roles = allRoles.Where(q=>createUserRequest.addToRoles.Contains(q.name)),
                passwordHash = [],
                passwordSalt = [],
                versionNumber = 5,
                createdAt = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow,
                theme = "light",
                firstName = createUserRequest.firstName,
                lastName = createUserRequest.lastName,
                lastLoginAt = null,
                lastPasswordChangeAt = null,
                passwordConfirmationNonce = Guid.NewGuid().ToString("N"),
            };

            if (createUserRequest.passwordConfirmationLinkExpiryDate.HasValue)
            {
                newUser.passwordLinkExpiresAt = createUserRequest.passwordConfirmationLinkExpiryDate.Value;    
            }
            
            if (!string.IsNullOrWhiteSpace(createUserRequest.password))
            {
                using var hmac = new HMACSHA512();
                newUser.passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(createUserRequest.password));
                newUser.passwordSalt = hmac.Key;
            }
            var response = await _container.CreateItemAsync(newUser, new PartitionKey(newUser.userName));;

            if (response.StatusCode == HttpStatusCode.Created)
            {
                // redact the password
                newUser.passwordHash = new byte[0];
                newUser.passwordSalt = new byte[0];
                
               // send email with link to set password
                var client = new EmailClient(Configuration["AzureCommunicationServicesConnection"]);

                var content = new EmailContent("Please complete your programx.co.uk login")
                {
                    PlainText = @$"
Hi,

Please complete your programx.co.uk login by navigating to the following link:
{Configuration["ClientUrl"]}/confirm-password?t=new-user&u={createUserRequest.userName}&n={newUser.passwordConfirmationNonce}

This link is valid until {newUser.passwordLinkExpiresAt}.

",
                    Html = @$"<h1>Please complete your programx.co.uk login</h1>
<p>Hi,</p>
<p>Please complete your programx.co.uk login by navigating to the following link:<br />
<a href=""{Configuration["ClientUrl"]}/confirm-password?t=new-user&u={createUserRequest.userName}&n={newUser.passwordConfirmationNonce}"">Complete login by entering your password</a></p>
<p>This link is valid until {newUser.passwordLinkExpiresAt}.</p>"
                };

                var recipients = new EmailRecipients(new[]
                {
                    new EmailAddress(createUserRequest.emailAddress, $"{createUserRequest.firstName} {createUserRequest.lastName}")
                });

                var message = new EmailMessage(
                    senderAddress: "DoNotReply@5e4bfc81-f032-4b41-b32b-584d6f5510d0.azurecomm.net", // must be a verified sender on your ACS domain
                    content: content,
                    recipients: recipients);

                // WaitUntil.Completed waits for the initial send operation to complete (not full delivery).
                EmailSendOperation operation = await client.SendAsync(WaitUntil.Completed, message);
                EmailSendResult result = operation.Value;
                if (result.Status == EmailSendStatus.Succeeded)
                {
                    return await HttpResponseDataFactory.CreateForCreated(httpRequestData, newUser, "user", newUser.id);    
                }
                else
                {
                    return await HttpResponseDataFactory.CreateForServerError(httpRequestData, $"Failed to send email: {result.Status}");
                }
            }
        
            return await HttpResponseDataFactory.CreateForServerError(httpRequestData, "Failed to create user");
        });
     }
    
    
    
}