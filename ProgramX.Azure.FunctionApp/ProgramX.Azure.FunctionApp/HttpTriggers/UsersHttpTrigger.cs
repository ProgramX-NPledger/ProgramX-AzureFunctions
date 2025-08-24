using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
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
        _logger = logger;
        _cosmosClient = cosmosClient;
        _blobServiceClient = blobServiceClient;

        _container = _cosmosClient.GetContainer("core", "users");

        
    }


    
    [Function(nameof(GetUser))]
    public async Task<HttpResponseData> GetUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/{id?}")] HttpRequestData httpRequestData,
        string? id)
    {
        return await RequiresAuthentication(httpRequestData, null, async (userName, _) =>
        {
            // pass a filter into the below
            var pagedAndFilteredCosmosDbReader =
                new PagedCosmosDBReader<SecureUser>(_cosmosClient, DataConstants.CoreDatabaseName, DataConstants.UsersContainerName,DataConstants.UserNamePartitionKeyPath);
            
            var continuationToken = httpRequestData.Query["continuationToken"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["continuationToken"]);
            QueryDefinition queryDefinition;
            if (string.IsNullOrWhiteSpace(id))
            {
                queryDefinition = new QueryDefinition("SELECT * FROM c order by c.userName");
            }
            else
            {
                queryDefinition = new QueryDefinition("SELECT * FROM c where c.id=@id or c.userName=@id");
                queryDefinition.WithParameter("@id", id);
            }
            var users = await pagedAndFilteredCosmosDbReader.GetItems(queryDefinition,continuationToken,DataConstants.ItemsPerPage);
            
            if (string.IsNullOrWhiteSpace(id))
            {
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new PagedResponse<SecureUser>()
                {
                    ContinuationToken = users.ContinuationToken,
                    Items = users.Items,
                    IsLastPage = !users.IsMorePages(),
                    ItemsPerPage = users.MaximumItemsRequested
                });
            }
            else
            {
                var user = users.Items.FirstOrDefault(q=>q.id==id || q.userName==id);
                if (user == null)
                {
                    return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "User");
                }

                if (!userName.Equals(user.userName))
                {
                    return await HttpResponseDataFactory.CreateForUnauthorised(httpRequestData);
                }
                
                List<Application> applications = user.roles.SelectMany(q=>q.Applications).GroupBy(g=>g.Name).Select(q=>q.First()).ToList();
                
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new
                {
                    user,
                    applications,
                    profilePhotoBase64 = string.Empty
                });
                
                
            }
        });
    }

    [Function(nameof(UpdateUser))]
    public async Task<HttpResponseData> UpdateUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "user/{id}")]
        HttpRequestData httpRequestData,
        string id)
    {
        return await RequiresAuthentication(httpRequestData, null,  async (usernameMakingTheChange, _) =>
        {
            var updateUserRequest = await httpRequestData.ReadFromJsonAsync<UpdateUserRequest>();
            if (updateUserRequest==null) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,"Invalid request body");

            // get the User to get the password hash
            var pagedAndFilteredCosmosDbReader =
                new PagedCosmosDBReader<User>(_cosmosClient, DataConstants.CoreDatabaseName, DataConstants.UsersContainerName,DataConstants.UserNamePartitionKeyPath);

            QueryDefinition queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.id=@id OR c.userName=@id");
            queryDefinition.WithParameter("@id", id);
            var users = await pagedAndFilteredCosmosDbReader.GetItems(queryDefinition);
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
        });
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
                new PagedCosmosDBReader<User>(_cosmosClient, DataConstants.CoreDatabaseName, DataConstants.UsersContainerName,DataConstants.UserNamePartitionKeyPath);

            QueryDefinition queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.id=@id OR c.userName=@id");
            queryDefinition.WithParameter("@id", id);
            var users = await pagedAndFilteredCosmosDbReader.GetItems(queryDefinition);
            var originalUser = users.Items.FirstOrDefault();
            if (originalUser == null)
            {
                return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "User");
            }
            
            // var container = _blobServiceClient.GetBlobContainerClient(BlobConstants.AvatarImagesBlobName);
            // await container.CreateIfNotExistsAsync(PublicAccessType.None);

            var reader = new MultipartReader(boundary, httpRequestData.Body);
            var section = await reader.ReadNextSectionAsync();

            var uploaded = new List<object>();

            while (section != null)
            {
                if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisp)
                    && contentDisp.DispositionType.Equals("form-data")
                    && (!string.IsNullOrEmpty(contentDisp.FileName.Value) || !string.IsNullOrEmpty(contentDisp.FileNameStar.Value)))
                {
                    var originalName = contentDisp.FileName.Value ?? contentDisp.FileNameStar.Value ?? "file";
                    var ext = Path.GetExtension(originalName);
//                    var blobName = $"{Guid.NewGuid():N}{ext}";
//                    var blob = container.GetBlobClient(blobName);

                    var headers = new BlobHttpHeaders
                    {
                        ContentType = section.ContentType ?? "application/octet-stream"
                    };

                    // form image resizing task with following per image:
                    // image dimensions, image target
                    
                    // Stream directly to Blob Storage (no buffering in memory)
//                    await blob.UploadAsync(section.Body, new BlobUploadOptions { HttpHeaders = headers });

                    uploaded.Add(new
                    {
                        originalName
                    });
                }
                // else: handle form fields if needed (e.g., section.AsFormData())

                section = await reader.ReadNextSectionAsync();
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
    
    private bool IsValidEmail(string email)
    {
        return Regex.IsMatch(
            email,
            RegExConstants.ValidEmailAddress,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(250) // avoid catastrophic backtracking
        );
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
                new PagedCosmosDBReader<Role>(_cosmosClient, DataConstants.CoreDatabaseName, DataConstants.UsersContainerName, DataConstants.UserNamePartitionKeyPath);
            
            var queryDefinition= new QueryDefinition("SELECT r.name, r.description, r.applications FROM c JOIN r IN c.roles");
            var roles = await rolesCosmosDbReader.GetItems(queryDefinition,null,null);
            
            using var hmac = new HMACSHA512();
            var allRoles = roles.Items.ToList(); // avoid multiple enumeration
        
            var newUser = new User()
            {
                id = Guid.NewGuid().ToString("N"),
                emailAddress = createUserRequest.EmailAddress,
                userName = createUserRequest.UserName,
                passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(createUserRequest.Password)),
                passwordSalt = hmac.Key,
                roles = allRoles.Where(q=>createUserRequest.AddToRoles.Select(r=>r.Name).Contains(q.Name))
                    .Union(
                        createUserRequest.AddToRoles
                            .Where(q=>!allRoles
                                .Select(r=>r.Name)
                                .Contains(q.Name)
                            )
                            .Select(q=>new Role()
                            {
                                Name = q.Name,
                                Description = q.Description,
                                Applications = q.Applications
                            }))
                    .ToList()
            };
        
            var response = await _container.CreateItemAsync(newUser, new PartitionKey(newUser.userName));;

            if (response.StatusCode == HttpStatusCode.Created)
            {
                // redact the password
                newUser.passwordHash = new byte[0];
                newUser.passwordSalt = new byte[0];
                
                return await HttpResponseDataFactory.CreateForCreated(httpRequestData, newUser, "user", newUser.id);
                
            }
        
            return await HttpResponseDataFactory.CreateForServerError(httpRequestData, "Failed to create user");
        });
     }
    
    
    
}