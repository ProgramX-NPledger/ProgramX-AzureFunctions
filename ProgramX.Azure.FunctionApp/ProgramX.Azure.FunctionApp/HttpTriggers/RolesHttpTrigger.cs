using System.Text;
using Google.Protobuf.WellKnownTypes;
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
using ProgramX.Azure.FunctionApp.Model.Responses.Dtos;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class RolesHttpTrigger : AuthorisedHttpTriggerBase
{
    private readonly ILogger<RolesHttpTrigger> _logger;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IApplicationProvider _applicationProvider;


    public RolesHttpTrigger(ILogger<RolesHttpTrigger> logger,
        IConfiguration configuration,
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        IApplicationProvider applicationProvider
        ) : base(configuration,logger)
    {
        _logger = logger;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _applicationProvider = applicationProvider;
    }
 
    
    [Function(nameof(GetRole))]
    public async Task<HttpResponseData> GetRole(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "roles/{roleName?}")] HttpRequestData httpRequestData,
        string? roleName)
    {
         return await RequiresAuthentication(httpRequestData, null, async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(roleName))
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
                        ContainingText = containsText
                    };
                    var pagedCriteria = new PagedCriteria()
                    {
                        ItemsPerPage = itemsPerPage,
                        Offset = offset
                    };
                    
                    var allApplications = _applicationProvider.GetAllApplications(new GetAllApplicationsCriteria())
                        .Select(q => q.GetApplicationMetaData());
                    
                    // TODO filter by usersInRole

                    _logger.LogInformation("Retrieving roles with criteria {criteria} paged by {pagedCriteria}", criteria, pagedCriteria);
                    var roles = await _roleRepository.GetRolesAsync(criteria, pagedCriteria);

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
                    
                    return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new PagedResponse<Role,RoleDto>((IPagedResult<Role>)roles,pageUrls,
                        (role) =>
                            new RoleDto()
                            {
                                RoleName = role.RoleName,
                                Description = role.Description,
                                UsedInApplications = allApplications.Where(a => a.RequiresRoleNames.Any(r => r == role.RoleName)).Select(a => a.Name)
                            }
                    ));
                    
                }
            }
            else
            {
                using (_logger.BeginScope("Retrieving Role {name}", roleName))
                {
                    var roles = await _roleRepository.GetRolesAsync(new GetRolesCriteria()
                    {
                        AnyOfRoleNames = [roleName]
                    });
                    if (!roles.Items.Any())
                    {
                        _logger.LogWarning("Role {name} not found", roleName);
                        return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "Role");
                    }

                    _logger.LogInformation("Retrieved role {role}", roles.Items.First());

                    var applicationsWithRole = _applicationProvider.GetAllApplications(new GetAllApplicationsCriteria()
                    {
                        HasAnyOfRoles = [roleName]
                    }).Select(q => q.GetApplicationMetaData());
                    var usersInRole = await _userRepository.GetUsersAsync(new GetUsersCriteria()
                    {
                        WithRoles = [roleName]
                    });

                    return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new GetRoleResponse()
                    {
                        Role = new RoleDto()
                        {
                            RoleName = roles.Items.First().RoleName,
                            Description = roles.Items.First().Description,
                            UsedInApplications = applicationsWithRole.Select(a => a.Name)
                        },
                        ApplicationsWithRole = [], // TODO: get applications
                        UsersInRole = usersInRole.Items.Select(q => q.UserName)
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
    /// <response code="429">Role already exists.</response>
    /// <response code="401">Unauthorized.</response>
    [Function(nameof(CreateRole))]
    public async Task<HttpResponseData> CreateRole(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "roles")] HttpRequestData httpRequestData
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

                // create the role
                Role newRole;
                try
                {
                    newRole = await _roleRepository.CreateRoleAsync(createRoleRequest.Name, createRoleRequest.Description);
                }
                catch (ItemAlreadyExistsException)
                {
                    _logger.LogError("Role {role} already exists", createRoleRequest.Name);
                    return await HttpResponseDataFactory.CreateForConflict(httpRequestData, "Role already exists");
                }
                catch (ItemCreationException)
                {
                    _logger.LogError("Failed to create role {role}", createRoleRequest.Name);
                    return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Failed to create Role");           
                }

                // add users to role
                List<string> failedUsers = new List<string>();
                foreach (var user in createRoleRequest.AddToUsers)
                {
                    try
                    {
                        await _userRepository.AddRoleToUserAsync(newRole.RoleName, user);
                    }
                    catch (RepositoryException e)
                    {
                        _logger.LogError(e, "Failed to add User {user} to Role {role}", user, newRole.RoleName);
                        failedUsers.Add(user);
                    }
                }
                if (failedUsers.Any())
                {
                    _logger.LogError("Failed to add {usersCount} Users to Role {role}", failedUsers.Count, newRole.RoleName);
                    return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,
                        "One or more Users were not added to the Role");
                }
                
                _logger.LogInformation("Created role {role}, added {usersCount} Users", newRole.RoleName, createRoleRequest.AddToUsers.Count());
                return await HttpResponseDataFactory.CreateForCreated(httpRequestData, newRole, "role", newRole.RoleName);    
            }
        });
     }
    
    [Function(nameof(UpdateRole))]
    public async Task<HttpResponseData> UpdateRole(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "roles/{roleName}")]
        HttpRequestData httpRequestData,
        string roleName)
    {
        return await RequiresAuthentication(httpRequestData, null, async (usernameMakingTheChange, _) =>
        {
            using (_logger.BeginScope("Updating Role {roleName}", roleName))
            {
                var updateRoleRequest =
                    await HttpBodyUtilities.GetDeserializedJsonFromHttpRequestDataBodyAsync<UpdateRoleRequest>(
                        httpRequestData);

                if (updateRoleRequest == null)
                {
                    _logger.LogError("Invalid request body");
                    return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Invalid request body");
                }

                try
                {
                    await _roleRepository.UpdateRoleAsync(roleName, updateRoleRequest.Description, updateRoleRequest.UsersInRole);
                }
                catch (ItemNotFoundException)
                {
                    _logger.LogWarning("Role {roleName} not found", roleName);
                    return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "Role");
                }
                catch (UpdateImmutablePropertyException)
                {
                    _logger.LogWarning("Cannot update immutable property");
                    return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Cannot update immutable property");
                }
                catch (ItemUpdateException)
                {
                    _logger.LogError("Failed to update role {roleName}", roleName);
                    return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Failed to update Role");           
                }
                
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new UpdateRoleResponse()
                {
                    Name = roleName,
                });
            }
        });
    }
    

    
    [Function(nameof(DeleteRole))]
    public async Task<HttpResponseData> DeleteRole(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "roles/{roleName}")]
        HttpRequestData httpRequestData,
        string roleName)
    {
        return await RequiresAuthentication(httpRequestData, null,  async (usernameMakingTheChange, _) =>
        {
            using (_logger.BeginScope("Deleting Role {roleName}", roleName))
            {
                try
                {
                    await _roleRepository.DeleteRoleByNameAsync(roleName);
                }
                catch (ItemNotFoundException)
                {
                    _logger.LogWarning("Role {roleName} not found", roleName);
                    return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "Role");
                }
                catch (ItemUpdateException)
                {
                    _logger.LogError("Failed to delete role {roleName}", roleName);
                    return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Failed to delete Role");           
                }
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