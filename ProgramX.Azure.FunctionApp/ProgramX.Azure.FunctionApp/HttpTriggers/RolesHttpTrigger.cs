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
        IUserRepository userRepository) : base(configuration)
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
                    role = roles.Items.First(),
                    allUsers = allUsers.Items,
                    usersInRole,
                    allApplications.Items
                });
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
            var createRoleRequest =
                await HttpBodyUtilities.GetDeserializedJsonFromHttpRequestDataBodyAsync<CreateRoleRequest>(httpRequestData);
            if (createRoleRequest == null) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,"Invalid request body");

            // TODO: check if role already exists and return 409 if so
            
            var allApplications = await _userRepository.GetApplicationsAsync(new GetApplicationsCriteria()
            {
                ApplicationNames = createRoleRequest.addToApplications
            });

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
                return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, e.Message);           
            }

            return await HttpResponseDataFactory.CreateForCreated(httpRequestData, newRole, "role", newRole.name);    
        });
     }
    
    
    
    [Function(nameof(UpdateRole))]
    public async Task<HttpResponseData> UpdateRole(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "role/{id}")]
        HttpRequestData httpRequestData,
        string id)
    {
        return await RequiresAuthentication(httpRequestData, null,  async (usernameMakingTheChange, _) =>
        {
            var updateRoleRequest =
                await HttpBodyUtilities.GetDeserializedJsonFromHttpRequestDataBodyAsync<UpdateRoleRequest>(httpRequestData);
            if (updateRoleRequest == null) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,"Invalid request body");

            var role = await _userRepository.GetRoleByNameAsync(id);
            if (role == null) return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "Role");
            
            role.name=updateRoleRequest.name!;
            role.description = updateRoleRequest.decription!;

            var applications=await _userRepository.GetApplicationsAsync(new GetApplicationsCriteria());
            role.applications = applications.Items.Where(q => updateRoleRequest.applications.Contains(q.name)).OrderBy(q => q.name).ToList();

            await _userRepository.UpdateRoleAsync(id,role);

            return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new UpdateRoleResponse()
            {
                Name = role.name,
                ErrorMessage = null,
                IsOk = true
            });
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
            var role = await _userRepository.GetRoleByNameAsync(id);
            if (role == null) return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "Role");
            await _userRepository.DeleteRoleByNameAsync(id);
            return HttpResponseDataFactory.CreateForSuccessNoContent(httpRequestData);
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