using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using ProgramX.Azure.FunctionApp.Constants;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Constants;
using ProgramX.Azure.FunctionApp.Model.Criteria;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Model.Responses;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using EmailMessage = ProgramX.Azure.FunctionApp.Model.EmailMessage;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class UsersHttpTrigger : AuthorisedHttpTriggerBase
{
    private readonly ILogger<UsersHttpTrigger> _logger;
    private readonly IStorageClient? _storageClient;
    private readonly IEmailSender _emailSender;
    private readonly IUserRepository _userRepository;

 
    public UsersHttpTrigger(ILogger<UsersHttpTrigger> logger,
        IStorageClient? storageClient,
        IConfiguration configuration,
        IEmailSender emailSender,
        IUserRepository userRepository) : base(configuration, logger)
    {
        _logger = logger;
        _storageClient = storageClient;
        _emailSender = emailSender;
        _userRepository = userRepository;
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
                var continuationToken = httpRequestData.Query["continuationToken"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["continuationToken"]!);
                var containsText = httpRequestData.Query["containsText"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["containsText"]!);
                var withRoles = httpRequestData.Query["withRoles"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["withRoles"]!).Split(new [] {','});
                var hasAccessToApplications = httpRequestData.Query["hasAccessToApplications"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["hasAccessToApplications"]!).Split(new [] {','});

                var sortByColumn = httpRequestData.Query["sortBy"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["sortBy"]!);
                var offset = UrlUtilities.GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["offset"]) ??
                             0;
                var itemsPerPage = UrlUtilities.GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["itemsPerPage"]) ?? PagingConstants.ItemsPerPage;
                
                var users = await _userRepository.GetUsersAsync(new GetUsersCriteria()
                {
                    HasAccessToApplications = hasAccessToApplications,
                    WithRoles = withRoles,
                    ContainingText = containsText
                }, new PagedCriteria()
                {
                    ItemsPerPage = itemsPerPage,
                    Offset = offset
                });
                
                var baseUrl =
                    $"{httpRequestData.Url.Scheme}://{httpRequestData.Url.Authority}{httpRequestData.Url.AbsolutePath}";
                
                var pageUrls = CalculatePageUrls((IPagedResult<User>)users,
                    baseUrl,
                    containsText,
                    withRoles,
                    hasAccessToApplications,
                    continuationToken, 
                    offset,
                    itemsPerPage);
                
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new PagedResponse<User>((IPagedResult<User>)users,pageUrls));
            }
            else
            {
                var user = await _userRepository.GetUserByIdAsync(id);
                if (user==null)
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
    
    
    
    
    private IEnumerable<UrlAccessiblePage> CalculatePageUrls(IPagedResult<User> pagedResults, 
        string baseUrl, 
        string? containsText, 
        IEnumerable<string>? withRoles, 
        IEnumerable<string>? hasAccessToApplications, 
        string? continuationToken,
        int offset=0, 
        int itemsPerPage=PagingConstants.ItemsPerPage)
    {
        var currentPageNumber = offset==0 ? 1 : (int)Math.Ceiling((offset+1.0) / itemsPerPage);
        
        List<UrlAccessiblePage> pageUrls = new List<UrlAccessiblePage>();
        for (var pageNumber = 1; pageNumber <= pagedResults.NumberOfPages; pageNumber++)
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
    
    
    
    private string BuildPageUrl(string baseUrl, string? containsText, IEnumerable<string>? withRoles, IEnumerable<string>? hasAccessToApplications, string? continuationToken, int? offset, int? itemsPerPage)
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
    
    
    
    
    [Function(nameof(DeleteUser))]
    public async Task<HttpResponseData> DeleteUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "user/{id}")]
        HttpRequestData httpRequestData,
        string id)
    {
        return await RequiresAuthentication(httpRequestData, null,  async (usernameMakingTheChange, _) =>
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null) return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "User");
            await _userRepository.DeleteUserByIdAsync(id);
            return HttpResponseDataFactory.CreateForSuccessNoContent(httpRequestData);
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
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null) return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "User");
            
            if (updateUserRequest.updateProfileScope)
            {
                if (user.userName!=updateUserRequest.userName) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Cannot change the username because it is used for the Partition Key");
                if (!IsValidEmail(updateUserRequest.emailAddress!)) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Invalid email address");
                
                user.emailAddress=updateUserRequest.emailAddress!;
                user.firstName=updateUserRequest.firstName!;
                user.lastName=updateUserRequest.lastName!;
            }

            if (updateUserRequest.updateSettingsScope)
            {
                user.theme = updateUserRequest.theme;
                user.schemaVersionNumber = user.schemaVersionNumber >= 3 ? user.schemaVersionNumber : 3; 
            }
            
            if (updateUserRequest.updateRolesScope)
            {
                var roles=await _userRepository.GetRolesAsync(new GetRolesCriteria());
                user.roles = roles.Items.Where(q => updateUserRequest.roles.Contains(q.name)).OrderBy(q => q.name).ToList();
            }

            if (updateUserRequest.updateProfilePictureScope)
            {
                return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Incorrect endpoint, use /user/{id}/photo instead");
            }

            // store the nonce/etc. currently on the user before we reset it
            var passwordNonce = updateUserRequest.updatePasswordScope ? user.passwordConfirmationNonce : null;
            var passwordLinkExpiresAt = updateUserRequest.updatePasswordScope ? user.passwordLinkExpiresAt : null;
            
            if (updateUserRequest.updatePasswordScope)
            {
                // user is changing their password so reset these
                user.passwordConfirmationNonce = null;
                user.passwordLinkExpiresAt = null;
            }
            
            await _userRepository.UpdateUserAsync(user);
            
            if (updateUserRequest.updatePasswordScope)
            {
                if (string.IsNullOrWhiteSpace(updateUserRequest.newPassword)) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Password cannot be empty");
                if (user.userName!=updateUserRequest.userName) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Incorrect username");
                if (!string.IsNullOrEmpty(passwordNonce) && passwordNonce!=updateUserRequest.passwordConfirmationNonce) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Incorrect password confirmation nonce");
                if (passwordLinkExpiresAt.HasValue && passwordLinkExpiresAt.Value < DateTime.UtcNow) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Password confirmation link has expired");
                
                await _userRepository.UpdateUserPasswordAsync(user.userName, updateUserRequest.newPassword);
            }

            return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new UpdateUserResponse()
            {
                Username = user.userName,
                ErrorMessage = null,
                IsOk = true
            });
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

            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null) return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "User");

            var storageFolder = await _storageClient!.GetStorageFolderAsync(BlobConstants.AvatarImagesBlobName);

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

            string thumbnailImageUri=string.Empty;
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
                    
                    await storageFolder.SaveFileAsync($"{usernameMakingTheChange}/{originalBlobName}", originalImageStream, multipartSection.ContentType ?? "application/octet-stream");;

                    // having moved the cursor to the end of the stream, it is reset
                    originalImageStream.Position = 0;
                    
                    // the stream is copied into an array, which can be seeked
                    var resizedImage = await ResizeAsync(rawData, 32);
                    
                    // the array is set back into a stream
                    var resizedImageStream = new MemoryStream(resizedImage);
                    
                    // upload to blob storage
                    thumbnailImageUri = (await storageFolder.SaveFileAsync(
                        $"{usernameMakingTheChange}/{avatarSizedBlobName}", resizedImageStream,
                        multipartSection.ContentType ?? "application/octet-stream")).Url;

                    // update record in DB
                    user.profilePhotographSmall = avatarSizedBlobName;
                    user.profilePhotographOriginal = originalBlobName;
                    user.schemaVersionNumber = user.schemaVersionNumber > 2 ? user.schemaVersionNumber : 2; // increment version number
                    await _userRepository.UpdateUserAsync(user);
                }
                // else: handle form fields if needed (e.g., section.AsFormData())

                multipartSection = await multipartReader.ReadNextSectionAsync();
            }

            return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new UpdateProfilePhotoResponse()
            {
                photoUrl = thumbnailImageUri,
                ErrorMessage = null,
                IsOk = true,
                BytesTransferred = 0,
                HttpEventType = HttpEventType.Response,
                TotalBytesToTransfer = 0
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
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null) return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "User");

            Debug.Assert(_storageClient != null, nameof(_storageClient) + " != null");
            var storageFolder = await _storageClient.GetStorageFolderAsync(BlobConstants.AvatarImagesBlobName);
            
            await storageFolder.DeleteFileAsync($"{usernameMakingTheChange}/{user.profilePhotographOriginal}");
            await storageFolder.DeleteFileAsync($"{usernameMakingTheChange}/{user.profilePhotographSmall}");
            
            // update record in DB
            user.profilePhotographSmall = null;
            user.profilePhotographOriginal = null;
            
            user.schemaVersionNumber = user.schemaVersionNumber > 2 ? user.schemaVersionNumber : 2; // increment version number
            await _userRepository.UpdateUserAsync(user);
            
            return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new UpdateResponse()
            {
                ErrorMessage = null,
                IsOk = true,
                BytesTransferred = 0,
                HttpEventType = HttpEventType.Response,
                TotalBytesToTransfer = 0
            });
            
        });
    }
    
    [Function(nameof(CreateUser))]
    public async Task<HttpResponseData> CreateUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user")] HttpRequestData httpRequestData
    )
    {
        return await RequiresAuthentication(httpRequestData, ["admin"],  async (_, _) =>
        {
            var createUserRequest =
                await HttpBodyUtilities.GetDeserializedJsonFromHttpRequestDataBodyAsync<CreateUserRequest>(httpRequestData);
            if (createUserRequest == null) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,"Invalid request body");

            var allRoles = await _userRepository.GetRolesAsync(new GetRolesCriteria());
        
            var newUser = new User()
            {
                id = Guid.NewGuid().ToString("N"),
                emailAddress = createUserRequest.emailAddress,
                userName = createUserRequest.userName,
                roles = allRoles.Items.Where(q=>createUserRequest.addToRoles.Contains(q.name)),
                schemaVersionNumber = 6,
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
            
            await _userRepository.CreateUserAsync(newUser);

            if (!string.IsNullOrWhiteSpace(createUserRequest.password))
            {
                await _userRepository.UpdateUserPasswordAsync(
                    newUser.userName,
                    createUserRequest.password);
            }

            
            var emailMessage = new EmailMessage()
            {
                To =
                [
                    new EmailRecipient(createUserRequest.emailAddress, $"{createUserRequest.firstName} {createUserRequest.lastName}")
                ],
                From = new EmailRecipient("DoNotReply@5e4bfc81-f032-4b41-b32b-584d6f5510d0.azurecomm.net", "Support"), // must be a verified sender on your ACS domain
                PlainTextBody = @$"
Hi,

Please complete your programx.co.uk login by navigating to the following link:
{Configuration["ClientUrl"]}/confirm-password?t=new-user&u={createUserRequest.userName}&n={newUser.passwordConfirmationNonce}

This link is valid until {newUser.passwordLinkExpiresAt}.

",
                Subject = "Complete your programx.co.uk login",
                HtmlBody = @$"<h1>Please complete your programx.co.uk login</h1>
<p>Hi,</p>
<p>Please complete your programx.co.uk login by navigating to the following link:<br />
<a href=""{Configuration["ClientUrl"]}/confirm-password?t=new-user&u={createUserRequest.userName}&n={newUser.passwordConfirmationNonce}"">Complete login by entering your password</a></p>
<p>This link is valid until {newUser.passwordLinkExpiresAt}.</p>"
            };
            
            try
            {
                await _emailSender.SendEmailAsync(emailMessage);
            }
            catch (InvalidOperationException invalidOperationException)
            {
                return await HttpResponseDataFactory.CreateForServerError(httpRequestData, $"Failed to send email: {invalidOperationException.Message}");
            }

            return await HttpResponseDataFactory.CreateForCreated(httpRequestData, newUser, "user", newUser.id);    
        });
     }
    
    
    
}