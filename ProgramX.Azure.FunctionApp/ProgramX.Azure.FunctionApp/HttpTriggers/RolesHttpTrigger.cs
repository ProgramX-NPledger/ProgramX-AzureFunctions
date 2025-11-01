using System.Net;
using System.Text;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Constants;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Cosmos;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Constants;
using ProgramX.Azure.FunctionApp.Model.Criteria;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Model.Responses;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class RolesHttpTrigger : AuthorisedHttpTriggerBase
{
    private readonly ILogger<RolesHttpTrigger> _logger;
    private readonly CosmosClient _cosmosClient;
    private readonly IUserRepository _userRepository;
    private readonly Container _container;

 
    public RolesHttpTrigger(ILogger<RolesHttpTrigger> logger,
        CosmosClient cosmosClient, 
        IConfiguration configuration,
        IUserRepository userRepository) : base(configuration)
    {
        _logger = logger;
        _cosmosClient = cosmosClient;
        _userRepository = userRepository;

        _container = _cosmosClient.GetContainer("core", "roles");
    }
 
    
    [Function(nameof(GetRole))]
    public async Task<HttpResponseData> GetRole(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "role/{name?}")] HttpRequestData httpRequestData,
        string? name)
    {
         return await RequiresAuthentication(httpRequestData, null, async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                var continuationToken = httpRequestData.Query["continuationToken"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["continuationToken"]);
                var containsText = httpRequestData.Query["containsText"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["containsText"]);
                var usedInApplications = httpRequestData.Query["usedInApplications"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["usedInApplications"]).Split(new [] {','});

                var offset = UrlUtilities.GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["offset"]) ?? 0;
                var itemsPerPage = UrlUtilities.GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["itemsPerPage"]) ?? PagingConstants.ItemsPerPage;
                
                var roles = await _userRepository.GetRolesAsync(new GetRolesCriteria()
                {
                    UsedInApplicationNames = usedInApplications,
                    ContainingText = containsText
                }, new PagedCriteria()
                {
                    ItemsPerPage = itemsPerPage,
                    Offset = offset
                });
                
                var baseUrl =
                    $"{httpRequestData.Url.Scheme}://{httpRequestData.Url.Authority}{httpRequestData.Url.AbsolutePath}";
                
                var pageUrls = CalculatePageUrls((IPagedResult<Role>)roles,
                    baseUrl,
                    containsText,
                    usedInApplications,
                    continuationToken, 
                    offset,
                    itemsPerPage);
                
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new PagedResponse<Role>((IPagedResult<Role>)roles,pageUrls));
                
            }
            else
            {
                var roles = await _userRepository.GetRolesAsync(new GetRolesCriteria()
                {
                    RoleName = name
                });
                if (!roles.Items.Any())
                {
                    return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "Role");
                }

                var allUsers = await _userRepository.GetUsersAsync(new GetUsersCriteria());
                var usersInRole = _userRepository.GetUsersInRole(name, allUsers.Items);
                var allApplications = await _userRepository.GetApplicationsAsync(new GetApplicationsCriteria());
                
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new
                {
                    role = roles.Items.Single(),
                    allUsers = allUsers.Items,
                    usersInRole,
                    allApplications.Items
                });
            }
        });
    }


    
    private IEnumerable<UrlAccessiblePage> CalculatePageUrls(IPagedResult<Role> cosmosPagedResult, 
        string baseUrl, 
        string? containsText, 
        IEnumerable<string>? usedInApplications,
        string? continuationToken,
        int offset=0, 
        int itemsPerPage=PagingConstants.ItemsPerPage)
    {
        var currentPageNumber = offset==0 ? 1 : (int)Math.Ceiling((double)offset / itemsPerPage);
        
        List<UrlAccessiblePage> pageUrls = new List<UrlAccessiblePage>();
        for (var pageNumber = 1; pageNumber <= cosmosPagedResult.NumberOfPages; pageNumber++)
        {
            pageUrls.Add(new UrlAccessiblePage()
            {
                Url = BuildPageUrl(baseUrl, containsText, usedInApplications, continuationToken, offset, itemsPerPage),
                PageNumber = pageNumber,
                IsCurrentPage = pageNumber == currentPageNumber
            });
        }
        return pageUrls;
    }

    private string BuildPageUrl(string baseUrl, string? containsText, IEnumerable<string>? usedInApplications, string? continuationToken, int? offset, int? itemsPerPage)
    {
        var parametersDictionary = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(containsText))
        {
            parametersDictionary.Add("containsText", Uri.EscapeDataString(containsText));
        }

        if (usedInApplications != null && usedInApplications.Any())
        {
            parametersDictionary.Add("usedInApplications", Uri.EscapeDataString(string.Join(",", usedInApplications)));
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
    
    
    

    [Function(nameof(CreateRole))]
    public async Task<HttpResponseData> CreateRole(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "role")]
        HttpRequestData httpRequestData
    )
    {
        return await RequiresAuthentication(httpRequestData, null,  async (_, _) =>
        {
            var createRoleRequest = await httpRequestData.ReadFromJsonAsync<CreateRoleRequest>();
            if (createRoleRequest==null) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Invalid request body");

            // get all users that were selected for the role
            var usersCosmosDbReader =
                new PagedCosmosDbReader<User>(_cosmosClient, DataConstants.CoreDatabaseName, DataConstants.UsersContainerName, DataConstants.UserNamePartitionKeyPath);
            
            var usersQueryDefinition= new QueryDefinition("SELECT * from u "); // TODO: Limit to addToUsers
            var users = await usersCosmosDbReader.GetNextItemsAsync(usersQueryDefinition,null,null);
            
            var allUsers = users.Items.ToList(); // avoid multiple enumeration
            
            // get all applications that were selected for the role
            var applicationsCosmosDbReader =
                new PagedCosmosDbReader<Application>(_cosmosClient, DataConstants.CoreDatabaseName, DataConstants.UsersContainerName, DataConstants.UserNamePartitionKeyPath);
            
            var applicationsQueryDefinition= new QueryDefinition("SELECT a.name, a.description, a.imageUrl, a.targetUrl, a.isDefaultApplicationOnLogin, a.ordinal, a.type, a.schemeVersionNumber FROM c JOIN a IN c.roles.applications"); // TODO: fix SQL
            var applications = await applicationsCosmosDbReader.GetNextItemsAsync(applicationsQueryDefinition,null,null);
            
            var allApplications = applications.Items.ToList(); // avoid multiple enumeration
            
            // create the role
            var newRole = new Role()
            {
                name = createRoleRequest.name,
                description = createRoleRequest.description,
                applications = allApplications.Where(q=>createRoleRequest.addToApplications.Contains(q.name)),
                schemaVersionNumber = 2,
                createdAt = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow,
            };

            List<string> userUpdateErrors = new List<string>();
            foreach (var user in allUsers)
            {
                List<Role> roles = new List<Role>(user.roles);
                roles.Add(newRole);
                user.roles = roles;
                var response = await _container.ReplaceItemAsync(user,user.id);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    userUpdateErrors.Add($"{user.userName}: {response.StatusCode}");
                }
            }
            
            return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new
            {
                userUpdateErrors,
                role = newRole
            });
        });
     }
    
    
    
}