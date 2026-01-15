using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Constants;
using ProgramX.Azure.FunctionApp.Model.Criteria;
using ProgramX.Azure.FunctionApp.Model.Exceptions;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Model.Responses;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class RolesHttpTrigger : AuthorisedHttpTriggerBase
{
    private readonly ILogger<RolesHttpTrigger> _logger;
    private readonly IUserRepository _userRepository;

 
    public RolesHttpTrigger(ILogger<RolesHttpTrigger> logger,
        IConfiguration configuration,
        IUserRepository userRepository) : base(configuration,logger)
    {
        _logger = logger;
        _userRepository = userRepository;
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
                using (_logger.BeginScope("Listing Roles"))
                {
                    _logger.LogInformation("Request parameters {queryString}",httpRequestData.Query);
                    var continuationToken = httpRequestData.Query["continuationToken"] == null
                        ? null
                        : Uri.UnescapeDataString(httpRequestData.Query["continuationToken"]!);
                    var containsText = httpRequestData.Query["containsText"] == null
                        ? null
                        : Uri.UnescapeDataString(httpRequestData.Query["containsText"]!);
                    var usedInApplications = httpRequestData.Query["usedInApplications"] == null
                        ? null
                        : Uri.UnescapeDataString(httpRequestData.Query["usedInApplications"]!).Split(new[] { ',' });

                    var offset =
                        UrlUtilities.GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["offset"]) ?? 0;
                    var itemsPerPage =
                        UrlUtilities.GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["itemsPerPage"]) ??
                        PagingConstants.ItemsPerPage;

                    var criteria = new GetRolesCriteria()
                    {
                        UsedInApplicationNames = usedInApplications,
                        ContainingText = containsText
                    };
                    var pagedCriteria = new PagedCriteria()
                    {
                        ItemsPerPage = itemsPerPage,
                        Offset = offset
                    };

                    _logger.LogInformation("Retrieving roles with criteria {criteria} paged by {pagedCriteria}", criteria, pagedCriteria);
                    var roles = await _userRepository.GetRolesAsync(criteria, pagedCriteria);

                    _logger.LogInformation("Retrieved roles {result}", roles);
                    
                    var baseUrl =
                        $"{httpRequestData.Url.Scheme}://{httpRequestData.Url.Authority}{httpRequestData.Url.AbsolutePath}";
                    
                    var pageUrls = CalculatePageUrls((IPagedResult<Role>)roles,
                        baseUrl,
                        containsText,
                        usedInApplications,
                        continuationToken,
                        offset,
                        itemsPerPage);
                    _logger.LogInformation("Calculated page urls {pageUrls}", pageUrls);

                    return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new PagedResponse<Role>((IPagedResult<Role>)roles,pageUrls));
                }
            }
            else
            {
                using (_logger.BeginScope("Retrieving Role {name}", name))
                {
                    var roles = await _userRepository.GetRolesAsync(new GetRolesCriteria()
                    {
                        RoleName = name
                    });
                    if (!roles.Items.Any())
                    {
                        _logger.LogWarning("Role {name} not found", name);
                        return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "Role");
                    }

                    _logger.LogInformation("Retrieved role {role}", roles.Items.First());

                    var allUsers = await _userRepository.GetUsersAsync(new GetUsersCriteria());
                    var usersInRole = _userRepository.GetUsersInRole(name, allUsers.Items);
                    var allApplications = await _userRepository.GetApplicationsAsync(new GetApplicationsCriteria());
                    
                    _logger.LogInformation("Retrieved users in role {usersInRole}", usersInRole);
                    _logger.LogInformation("Retrieved all applications {allApplications}", allApplications);
                    
                    return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new
                    {
                        role = roles.Items.First(),
                        allUsers = allUsers.Items,
                        usersInRole,
                        allApplications.Items
                    });
                }

            }
        });
    }

    
    /// <summary>
    /// Create a Role.
    /// </summary>
    /// <param name="httpRequestData"></param>
    /// <returns></returns>
    /// <response code="201">Role created.</response>
    /// <response code="400">Invalid request body.</response>
    /// <response code="409">Role already exists.</response>
    /// <response code="500">Internal server error.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="403">Forbidden.</response>
    [Function(nameof(CreateRole))]
    public async Task<HttpResponseData> CreateRole(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "role")] HttpRequestData httpRequestData
    )
    {
        return await RequiresAuthentication(httpRequestData, null,  async (_, _) =>
        {
            using (_logger.BeginScope("Creating Role"))
            {
                var createRoleRequest =
                    await HttpBodyUtilities.GetDeserializedJsonFromHttpRequestDataBodyAsync<CreateRoleRequest>(httpRequestData);
                if (createRoleRequest == null)
                {
                    _logger.LogError("Invalid request body");
                    return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Invalid request body");
                }

                var existingRole = await _userRepository.GetRoleByNameAsync(createRoleRequest.name);
                if (existingRole != null)
                {
                    _logger.LogWarning("Role {name} already exists", createRoleRequest.name);
                    return await HttpResponseDataFactory.CreateForConflict(httpRequestData, "Role already exists");
                }
            
                var allApplications = await _userRepository.GetApplicationsAsync(new GetApplicationsCriteria()
                {
                    ApplicationNames = createRoleRequest.addToApplications
                });
                _logger.LogInformation("Adding applications {allApplications} to created Role", allApplications);
                
                var newRole = new Role()
                {
                    name = createRoleRequest.name,
                    description = createRoleRequest.description,
                    applications = allApplications.Items,
                    schemaVersionNumber = 2,
                    createdAt = DateTime.UtcNow,
                    updatedAt = DateTime.UtcNow,
                };

                try
                {
                    await _userRepository.CreateRoleAsync(newRole, createRoleRequest.addToUsers);
                }
                catch (RepositoryException e)
                {
                    _logger.LogError(e, "Failed to create role {role}", newRole);
                    return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, e.Message);           
                }

                _logger.LogInformation("Created role {role}", newRole);
                return await HttpResponseDataFactory.CreateForCreated(httpRequestData, newRole, "role", newRole.name);    
            }

        });
     }
    
    
    
    [Function(nameof(UpdateRole))]
    public async Task<HttpResponseData> UpdateRole(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "role/{id}")]
        HttpRequestData httpRequestData,
        string id)
    {
        return await RequiresAuthentication(httpRequestData, null, async (usernameMakingTheChange, _) =>
        {
            using (_logger.BeginScope("Updating Role {id}", id))
            {
                var updateRoleRequest =
                    await HttpBodyUtilities.GetDeserializedJsonFromHttpRequestDataBodyAsync<UpdateRoleRequest>(
                        httpRequestData);

                if (updateRoleRequest == null)
                {
                    _logger.LogError("Invalid request body");
                    return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Invalid request body");
                }

                var role = await _userRepository.GetRoleByNameAsync(id);
                if (role == null)
                {
                    _logger.LogWarning("Role {id} not found", id);
                    return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "Role");
                }

                role.name = updateRoleRequest.name!;
                role.description = updateRoleRequest.description!;

                var applications = await _userRepository.GetApplicationsAsync(new GetApplicationsCriteria());
                role.applications = applications.Items.Where(q => updateRoleRequest.applications.Contains(q.name))
                    .OrderBy(q => q.name).ToList();

                _logger.LogInformation("Updating role {role}", role);
                await _userRepository.UpdateRoleAsync(id, role);

                if (updateRoleRequest.usersInRole != null)
                {
                    // need to update users in role
                    foreach (var userName in updateRoleRequest.usersInRole)
                    {
                        var user = await _userRepository.GetUserByUserNameAsync(userName);
                        if (user == null) continue;

                        
                        var userIsAdded = user.roles.All(q => q.name != role.name);
                        if (userIsAdded)
                        {
                            _logger.LogInformation("Adding user {userName} to role {roleName}", userName, role.name);
                            await _userRepository.AddRoleToUserAsync(role, userName);
                        }
                        
                    }
                }
                
                // get all users for role (therefore all Users with that Role), removed will be difference between before and after
                var allUsers = await _userRepository.GetUsersAsync(new GetUsersCriteria()
                {
                    WithRoles = [role.name]
                });
                foreach (var user in allUsers.Items)
                {
                    if (updateRoleRequest.usersInRole?.Contains(user.userName) ?? false) continue;
                    _logger.LogInformation("Removing user {userName} from role {roleName}", user.userName, role.name);
                    await _userRepository.RemoveRoleFromUserAsync(role.name, user.userName);
                }

                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new UpdateRoleResponse()
                {
                    Name = role.name,
                    ErrorMessage = null,
                    IsOk = true
                });
            }
        });
    }
    

    
    [Function(nameof(DeleteRole))]
    public async Task<HttpResponseData> DeleteRole(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "role/{id}")]
        HttpRequestData httpRequestData,
        string id)
    {
        return await RequiresAuthentication(httpRequestData, null,  async (usernameMakingTheChange, _) =>
        {
            using (_logger.BeginScope("Deleting Role {id}", id))
            {
                var role = await _userRepository.GetRoleByNameAsync(id);
                if (role == null)
                {
                    _logger.LogWarning("Role {id} not found", id);
                    return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "Role");
                }
                await _userRepository.DeleteRoleByNameAsync(id);
                return HttpResponseDataFactory.CreateForSuccessNoContent(httpRequestData);
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
    
    
}