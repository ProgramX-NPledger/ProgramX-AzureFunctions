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
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Model.Responses;
using User = ProgramX.Azure.FunctionApp.Model.User;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class RolesHttpTrigger : AuthorisedHttpTriggerBase
{
    private readonly ILogger<RolesHttpTrigger> _logger;
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;

 
    public RolesHttpTrigger(ILogger<RolesHttpTrigger> logger,
        CosmosClient cosmosClient, IConfiguration configuration) : base(configuration)
    {
        _logger = logger;
        _cosmosClient = cosmosClient;

        _container = _cosmosClient.GetContainer("core", "roles");
    }
 
    
    [Function(nameof(GetRole))]
    public async Task<HttpResponseData> GetRole(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "role/{name?}")] HttpRequestData httpRequestData,
        string? name)
    {
        return await RequiresAuthentication(httpRequestData, null, async (_, _) =>
        {
            var rolesPagedCosmosDbReader =
                new PagedCosmosDbReader<Role>(_cosmosClient, DataConstants.CoreDatabaseName, DataConstants.UsersContainerName,DataConstants.UserNamePartitionKeyPath);;
            
            HttpResponseData httpResponseData;
            if (name == null)
            {
                var continuationToken = httpRequestData.Query["continuationToken"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["continuationToken"]);
                var containsText = httpRequestData.Query["containsText"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["containsText"]);
                var offset = GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["offset"]);
                var itemsPerPage = GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["itemsPerPage"]);
                
                var pagedCosmosDbRolesResults=await GetPagedMultipleItemsAsync(containsText,continuationToken,offset,itemsPerPage);
                var baseUrl =
                    $"{httpRequestData.Url.Scheme}://{httpRequestData.Url.Authority}{httpRequestData.Url.AbsolutePath}";
                var pageUrls = CalculatePageUrls(pagedCosmosDbRolesResults,baseUrl,continuationToken,containsText, offset,itemsPerPage);
                
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new PagedResponse<Role>(pagedCosmosDbRolesResults,null,pageUrls));
            }
            else
            {
                var role = await GetSingleItemAsync(name);
                if (role == null)
                {
                    return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "Role");
                }

                var usersCosmosDbReader = new PagedCosmosDbReader<User>(_cosmosClient, DataConstants.CoreDatabaseName, DataConstants.UsersContainerName, DataConstants.UserNamePartitionKeyPath);
                var users = await usersCosmosDbReader.GetNextItemsAsync(new QueryDefinition("SELECT * FROM u"),null,null);
                var distinctUsers = users.Items.GroupBy(q=>q.id).Select(q=>new SecureUser()
                {
                    id = q.First().id,
                    userName = q.First().userName,
                    emailAddress = q.First().emailAddress,
                    roles = q.First().roles,
                }).ToList();
                var usersInRole = users.Items.GroupBy(q=>q.id)
                    .Where(q=>q.First().roles.Select(q=>q.name).Contains(role.name))
                    .Select(q=>new UserInRole()
                {
                    Id = q.First().id,
                    UserName = q.First().userName,
                    EmailAddress = q.First().emailAddress,
                }).ToList();
                
                List<Application> applications = distinctUsers.SelectMany(q=>q.roles)
                        .SelectMany(q=>q.applications)
                        .GroupBy(g=>g.name)
                        .Select(q=>q.First()).ToList();

                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new
                {
                    role,
                    applications,
                    allUsers = distinctUsers,
                    usersInRole,
                });
                
            }
        });
    }

    
    private async  Task<Role?> GetSingleItemAsync(string name)
    {
        var sql = new StringBuilder("SELECT r.name, r.description, r.applications FROM c JOIN r IN c.roles where r.name=@name");

        QueryDefinition queryDefinition  = new QueryDefinition(sql.ToString());
        queryDefinition.WithParameter("@name",name);       
        
        var usersCosmosDbReader = new PagedCosmosDbReader<User>(_cosmosClient, DataConstants.CoreDatabaseName, DataConstants.UsersContainerName, DataConstants.UserNamePartitionKeyPath);
        PagedCosmosDBResult<User> pagedCosmosDbResult;
        pagedCosmosDbResult = await usersCosmosDbReader.GetPagedItemsAsync(queryDefinition,"r.name");
        
        var pagedCosmosDbResultForRoles = pagedCosmosDbResult.TransformItemsToDifferentType<Role>(m =>
        {
            List<Role> roles = new List<Role>();
            foreach (var role in m.roles)
            {
                roles.Add(role);           
            }
            return roles;
        },(role,allRoles) => allRoles.Any(q=>q.name.Equals(role.name,StringComparison.InvariantCultureIgnoreCase)));
        
        return pagedCosmosDbResultForRoles.Items.FirstOrDefault();
    }


    
    private IEnumerable<UrlAccessiblePage> CalculatePageUrls(PagedCosmosDBResult<Role> pagedCosmosDbRolesResults, 
        string baseUrl, 
        string? containsText, 
        string? continuationToken,
        int? offset=0, 
        int? itemsPerPage=DataConstants.ItemsPerPage)
    {
        var totalPages = (int)Math.Ceiling((double)pagedCosmosDbRolesResults.TotalItems / itemsPerPage ??
                                           DataConstants.ItemsPerPage);
        var currentPageNumber = (int)Math.Ceiling((double)offset / itemsPerPage ??
                                                  DataConstants.ItemsPerPage);
        
        List<UrlAccessiblePage> pageUrls = new List<UrlAccessiblePage>();
        for (var pageNumber = 1; pageNumber <= pagedCosmosDbRolesResults.TotalItems; pageNumber++)
        {

            
            pageUrls.Add(new UrlAccessiblePage()
            {
                Url = BuildPageUrl(baseUrl, containsText, continuationToken, offset, itemsPerPage),
                PageNumber = pageNumber,
                IsCurrentPage = pageNumber == currentPageNumber,
                IsFirstPage = pageNumber == 1,
                IsLastPage = pageNumber == totalPages,
            });
        }
        return pageUrls;
    }

    
    private async  Task<PagedCosmosDBResult<Role>> GetPagedMultipleItemsAsync(string? containsText,
        string? continuationToken,
        int? offset=0,
        int? itemsPerPage = DataConstants.ItemsPerPage)
    {
        var sql = new StringBuilder("SELECT r.name, r.description, r.applications FROM c JOIN r IN c.roles WHERE 1=1 ");
        var parameters = new List<(string name, object value)>();
        if (!string.IsNullOrWhiteSpace(containsText))
        {
            parameters.Add(("containsText", containsText));
            sql.Append($" AND CONTAINS(UPPER(r.name), @containsText)");
        }
        
        QueryDefinition queryDefinition  = new QueryDefinition(sql.ToString());
        foreach (var parameter in parameters)
        {
            queryDefinition.WithParameter(parameter.name, parameter.value);       // do we need to prefix the name with @? 
        }
        
        // if continuationToken is null, then we are returning the first page or all items up to itemsPerPage
        var usersCosmosDbReader = new PagedCosmosDbReader<User>(_cosmosClient, DataConstants.CoreDatabaseName, DataConstants.UsersContainerName, DataConstants.UserNamePartitionKeyPath);
        PagedCosmosDBResult<User> pagedCosmosDbResult;
        if (string.IsNullOrEmpty(continuationToken))
        {
            pagedCosmosDbResult = await usersCosmosDbReader.GetNextItemsAsync(new QueryDefinition("SELECT * FROM u"),null,itemsPerPage);
        }
        else
        {
            pagedCosmosDbResult = await usersCosmosDbReader.GetPagedItemsAsync(queryDefinition,"r.name",offset,itemsPerPage);
        }
        
        var pagedCosmosDbResultForRoles = pagedCosmosDbResult.TransformItemsToDifferentType<Role>(m =>
        {
            List<Role> roles = new List<Role>();
            foreach (var role in m.roles)
            {
                roles.Add(role);           
            }
            return roles;
        },(role,allRoles) => allRoles.Any(q=>q.name.Equals(role.name,StringComparison.InvariantCultureIgnoreCase)));
        
        return pagedCosmosDbResultForRoles;
    }

    private int? GetValidIntegerQueryStringParameterOrNull(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (!int.TryParse(s, out var offset)) return null;
        if (offset < 0) return null;
        return offset;
    }


    private string BuildPageUrl(string baseUrl, string? containsText, string? continuationToken, int? offset, int? itemsPerPage)
    {
        var parametersDictionary = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(containsText))
        {
            parametersDictionary.Add("containsText", Uri.EscapeDataString(containsText));
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