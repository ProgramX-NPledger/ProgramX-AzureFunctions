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

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class ScoutingActivitiesHttpTrigger : AuthorisedHttpTriggerBase
{
    private readonly ILogger<ScoutingActivitiesHttpTrigger> _logger;
    private readonly IStorageClient? _storageClient;
    private readonly IScoutingRepository _scoutingRepository;

 
    public ScoutingActivitiesHttpTrigger(ILogger<ScoutingActivitiesHttpTrigger> logger,
        IStorageClient? storageClient,
        IConfiguration configuration,
        IScoutingRepository scoutingRepository) : base(configuration,logger)
    {
        _logger = logger;
        _storageClient = storageClient;
        _scoutingRepository = scoutingRepository;
    }


    //
    // [Function(nameof(GetScoresLedger))]
    // public async Task<HttpResponseData> GetScoresLedger(
    //     [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scouts/scoresledger/{id?}")] HttpRequestData httpRequestData,
    //     string? id)
    // {
    //     return await RequiresAuthentication(httpRequestData, null, async (userName, _) =>
    //     {
    //         if (id == null)
    //         {
    //             var continuationToken = httpRequestData.Query["continuationToken"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["continuationToken"]!);
    //             var containsText = httpRequestData.Query["containsText"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["containsText"]!);
    //             var withRoles = httpRequestData.Query["withRoles"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["withRoles"]!).Split(new [] {','});
    //             var hasAccessToApplications = httpRequestData.Query["hasAccessToApplications"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["hasAccessToApplications"]!).Split(new [] {','});
    //
    //             var sortByColumn = httpRequestData.Query["sortBy"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["sortBy"]!);
    //             var offset = UrlUtilities.GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["offset"]) ??
    //                          0;
    //             var itemsPerPage = UrlUtilities.GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["itemsPerPage"]) ?? PagingConstants.ItemsPerPage;
    //             
    //             var users = await _userRepository.GetUsersAsync(new GetUsersCriteria()
    //             {
    //                 HasAccessToApplications = hasAccessToApplications,
    //                 WithRoles = withRoles,
    //                 ContainingText = containsText
    //             }, new PagedCriteria()
    //             {
    //                 ItemsPerPage = itemsPerPage,
    //                 Offset = offset
    //             });
    //             
    //             var baseUrl =
    //                 $"{httpRequestData.Url.Scheme}://{httpRequestData.Url.Authority}{httpRequestData.Url.AbsolutePath}";
    //             
    //             var pageUrls = CalculatePageUrls((IPagedResult<User>)users,
    //                 baseUrl,
    //                 containsText,
    //                 withRoles,
    //                 hasAccessToApplications,
    //                 continuationToken, 
    //                 offset,
    //                 itemsPerPage);
    //             
    //             return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new PagedResponse<User>((IPagedResult<User>)users,pageUrls));
    //         }
    //         else
    //         {
    //             var user = await _userRepository.GetUserByIdAsync(id);
    //             if (user==null)
    //             {
    //                 return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "User");
    //             }
    //             
    //             List<Application> applications = user.roles.SelectMany(q=>q.applications).GroupBy(g=>g.name).Select(q=>q.First()).ToList();
    //             
    //             return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new
    //             {
    //                 user,
    //                 applications,
    //                 profilePhotoBase64 = string.Empty
    //             });
    //         }
    //     });
    // }
    //
    //
    //
    //
    // private IEnumerable<UrlAccessiblePage> CalculatePageUrls(IPagedResult<User> pagedResults, 
    //     string baseUrl, 
    //     string? containsText, 
    //     IEnumerable<string>? withRoles, 
    //     IEnumerable<string>? hasAccessToApplications, 
    //     string? continuationToken,
    //     int offset=0, 
    //     int itemsPerPage=PagingConstants.ItemsPerPage)
    // {
    //     var currentPageNumber = offset==0 ? 1 : (int)Math.Ceiling((offset+1.0) / itemsPerPage);
    //     
    //     List<UrlAccessiblePage> pageUrls = new List<UrlAccessiblePage>();
    //     for (var pageNumber = 1; pageNumber <= pagedResults.NumberOfPages; pageNumber++)
    //     {
    //         pageUrls.Add(new UrlAccessiblePage()
    //         {
    //             Url = BuildPageUrl(baseUrl, containsText, withRoles, hasAccessToApplications, continuationToken, (pageNumber * itemsPerPage)-itemsPerPage, itemsPerPage),
    //             PageNumber = pageNumber,
    //             IsCurrentPage = pageNumber == currentPageNumber,
    //         });
    //     }
    //     return pageUrls;
    // }
    //
    //
    //
    // private string BuildPageUrl(string baseUrl, string? containsText, IEnumerable<string>? withRoles, IEnumerable<string>? hasAccessToApplications, string? continuationToken, int? offset, int? itemsPerPage)
    // {
    //     var parametersDictionary = new Dictionary<string, string>();
    //     if (!string.IsNullOrWhiteSpace(containsText))
    //     {
    //         parametersDictionary.Add("containsText", Uri.EscapeDataString(containsText));
    //     }
    //
    //     if (withRoles != null && withRoles.Any())
    //     {
    //         parametersDictionary.Add("withRoles", Uri.EscapeDataString(string.Join(",", withRoles)));
    //     }
    //
    //     if (hasAccessToApplications != null && hasAccessToApplications.Any())
    //     {
    //         parametersDictionary.Add("hasAccessToApplications", Uri.EscapeDataString(string.Join(",", hasAccessToApplications)));       
    //     }
    //     
    //     if (!string.IsNullOrWhiteSpace(continuationToken))
    //     {
    //         parametersDictionary.Add("continuationToken", Uri.EscapeDataString(continuationToken));
    //     }
    //
    //     if (offset != null)
    //     {
    //         parametersDictionary.Add("offset",offset.Value.ToString());
    //     }
    //
    //     if (itemsPerPage != null)
    //     {
    //         parametersDictionary.Add("itemsPerPage",itemsPerPage.Value.ToString());       
    //     }
    //     
    //     var sb=new StringBuilder(baseUrl);
    //     if (parametersDictionary.Any())
    //     {
    //         sb.Append("?");
    //         foreach (var param in parametersDictionary)
    //         {
    //             sb.Append($"{param.Key}={param.Value}&");
    //         }
    //         sb.Remove(sb.Length-1,1);
    //     }
    //
    //     return sb.ToString();
    // }
    //
    
    //
    //
    // [Function(nameof(DeleteScoresLedger))]
    // public async Task<HttpResponseData> DeleteScoresLedger(
    //     [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "scouts/scoresledger/{id}")]
    //     HttpRequestData httpRequestData,
    //     string id)
    // {
    //     return await RequiresAuthentication(httpRequestData, null,  async (usernameMakingTheChange, _) =>
    //     {
    //         var user = await _userRepository.GetUserByIdAsync(id);
    //         if (user == null) return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "User");
    //         await _userRepository.DeleteUserByIdAsync(id);
    //         return HttpResponseDataFactory.CreateForSuccessNoContent(httpRequestData);
    //     });
    // }
    //
    //
    // [Function(nameof(UpdatesScoresLedger))]
    // public async Task<HttpResponseData> UpdatesScoresLedger(
    //     [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "scouts/scoresledger/{id}")]
    //     HttpRequestData httpRequestData,
    //     string id)
    // {
    //     var updateUserRequest =
    //         await HttpBodyUtilities.GetDeserializedJsonFromHttpRequestDataBodyAsync<UpdateUserRequest>(httpRequestData);
    //     if (updateUserRequest == null) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,"Invalid request body");
    //
    //     var isChangePasswordRequest=updateUserRequest.updatePasswordScope 
    //                                 && updateUserRequest is { newPassword: not null, updateProfilePictureScope: false, updateProfileScope: false, updateRolesScope: false };
    //     
    //     return await RequiresAuthentication(httpRequestData, null,  async (usernameMakingTheChange, _) =>
    //     {
    //         var user = await _userRepository.GetUserByIdAsync(id);
    //         if (user == null) return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "User");
    //         
    //         if (updateUserRequest.updateProfileScope)
    //         {
    //             if (user.userName!=updateUserRequest.userName) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Cannot change the username because it is used for the Partition Key");
    //             if (!IsValidEmail(updateUserRequest.emailAddress!)) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Invalid email address");
    //             
    //             user.emailAddress=updateUserRequest.emailAddress!;
    //             user.firstName=updateUserRequest.firstName!;
    //             user.lastName=updateUserRequest.lastName!;
    //         }
    //
    //         if (updateUserRequest.updateSettingsScope)
    //         {
    //             user.theme = updateUserRequest.theme;
    //             user.schemaVersionNumber = user.schemaVersionNumber >= 3 ? user.schemaVersionNumber : 3; 
    //         }
    //         
    //         if (updateUserRequest.updateRolesScope)
    //         {
    //             var roles=await _userRepository.GetRolesAsync(new GetRolesCriteria());
    //             user.roles = roles.Items.Where(q => updateUserRequest.roles.Contains(q.name)).OrderBy(q => q.name).ToList();
    //         }
    //
    //         if (updateUserRequest.updateProfilePictureScope)
    //         {
    //             return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Incorrect endpoint, use /user/{id}/photo instead");
    //         }
    //
    //         // store the nonce/etc. currently on the user before we reset it
    //         var passwordNonce = updateUserRequest.updatePasswordScope ? user.passwordConfirmationNonce : null;
    //         var passwordLinkExpiresAt = updateUserRequest.updatePasswordScope ? user.passwordLinkExpiresAt : null;
    //         
    //         if (updateUserRequest.updatePasswordScope)
    //         {
    //             // user is changing their password so reset these
    //             user.passwordConfirmationNonce = null;
    //             user.passwordLinkExpiresAt = null;
    //         }
    //         
    //         await _userRepository.UpdateUserAsync(user);
    //         
    //         if (updateUserRequest.updatePasswordScope)
    //         {
    //             if (string.IsNullOrWhiteSpace(updateUserRequest.newPassword)) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Password cannot be empty");
    //             if (user.userName!=updateUserRequest.userName) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Incorrect username");
    //             if (!string.IsNullOrEmpty(passwordNonce) && passwordNonce!=updateUserRequest.passwordConfirmationNonce) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Incorrect password confirmation nonce");
    //             if (passwordLinkExpiresAt.HasValue && passwordLinkExpiresAt.Value < DateTime.UtcNow) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Password confirmation link has expired");
    //             
    //             await _userRepository.UpdateUserPasswordAsync(user.userName, updateUserRequest.newPassword);
    //         }
    //
    //         return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new UpdateUserResponse()
    //         {
    //             Username = user.userName,
    //             ErrorMessage = null,
    //             IsOk = true
    //         });
    //     },isChangePasswordRequest);
    // }
    //
    
    
    [Function(nameof(CreateActivity))]
    public async Task<HttpResponseData> CreateActivity(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "scouts/activities")] HttpRequestData httpRequestData
    )
    {
        return await RequiresAuthentication(httpRequestData, ["admin","scout-writer"],  async (_, _) =>
        {
            var createActivityRequest =
                await HttpBodyUtilities.GetDeserializedJsonFromHttpRequestDataBodyAsync<CreateScoutingActivityRequest>(httpRequestData);
            if (createActivityRequest == null) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,"Invalid request body");
            
            var newActivity = new ScoutingActivity()
            {
                activityFormat = createActivityRequest.ActivityFormat,
                activityLocation = createActivityRequest.ActivityLocation,
                id = Guid.NewGuid().ToString(),
                preparationMarkdown = createActivityRequest.PreparationMarkdown,
                referencesMarkdown = createActivityRequest.ReferencesMarkdown,
                resources = createActivityRequest.Resources,
                descriptionMarkdown = createActivityRequest.DescriptionMarkdown,
                summary = createActivityRequest.Summary,
                title = createActivityRequest.Title,
                tags = createActivityRequest.Tags,
                winModes = createActivityRequest.WinModes,
                schemaVersionNumber = 1,
                createdAt = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow,
                sections = createActivityRequest.Sections,
                activityType = createActivityRequest.ActivityType,
                contributesTowardsOsmBadgeId = createActivityRequest.ContributesTowardsOsmBadgeId,
                contributesTowardsOsmBadgeName = createActivityRequest.ContributesTowardsOsmBadgeName,
            };
            
            await _scoutingRepository.CreateScoutingActivityAsync(newActivity);
            
            return await HttpResponseDataFactory.CreateForCreated(httpRequestData, newActivity, "activity", newActivity.id.ToString());    
        });
     }
    
    
    
    [Function(nameof(GetAllActivityFormats))]
    public async Task<HttpResponseData> GetAllActivityFormats(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scouts/activities/activity-formats")] HttpRequestData httpRequestData
    )
    {
        return await RequiresAuthentication(httpRequestData, ["admin","scout-reader"],  async (_, _) =>
        {
            return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new []
            {
                new { key = ActivityFormat.Individual, label = "Individual" },
                new { key = ActivityFormat.Team, label = "Team" },
                new { key = ActivityFormat.Pair, label = "Pair" }
            }.OrderBy(q=>q.label));    
        });
     }
    
    
    
    [Function(nameof(GetAllActivityTypes))]
    public async Task<HttpResponseData> GetAllActivityTypes(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scouts/activities/activity-types")] HttpRequestData httpRequestData
    )
    {
        return await RequiresAuthentication(httpRequestData, ["admin","scout-reader"],  async (_, _) =>
        {
            return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new []
            {
                new { key = ActivityType.Activity, label = "Activity" },
                new { key = ActivityType.Game, label = "Game" }
            }.OrderBy(q=>q.label));    
        });
    }

    [Function(nameof(GetAllActivityLocations))]
    public async Task<HttpResponseData> GetAllActivityLocations(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scouts/activities/activity-locations")] HttpRequestData httpRequestData
    )
    {
        return await RequiresAuthentication(httpRequestData, ["admin","scout-reader"],  async (_, _) =>
        {
            return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new []
            {
                new { key = ActivityLocation.Indoors, label = "Indoors" },
                new { key = ActivityLocation.Outdoors, label = "Outdoors" }
            }.OrderBy(q=>q.label));    
        });
    }
    
    
    [Function(nameof(GetAllWinModes))]
    public async Task<HttpResponseData> GetAllWinModes(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scouts/activities/win-modes")] HttpRequestData httpRequestData
    )
    {
        return await RequiresAuthentication(httpRequestData, ["admin","scout-reader"],  async (_, _) =>
        {
            return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new []
            {
                new { key = WinMode.Success, label = "Success" },
                new { key = WinMode.Time, label = "Time" },
                new { key = WinMode.Attrition, label = "Attrition" }
            }.OrderBy(q=>q.label));    
        });
    }
    
    
    [Function(nameof(GetAllSections))]
    public async Task<HttpResponseData> GetAllSections(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scouts/activities/sections")] HttpRequestData httpRequestData
    )
    {
        return await RequiresAuthentication(httpRequestData, ["admin","scout-reader"],  async (_, _) =>
        {
            return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new []
            {
                new { key = Section.Beavers, label = "Beavers" },
                new { key = Section.Squirrels, label = "Squirrels" },
                new { key = Section.Cubs, label = "Cubs" },
                new { key = Section.Scouts, label = "Scouts" },
                new { key = Section.Explorers, label = "Explorers" },
                new { key = Section.Network, label = "Network" },
            }.OrderBy(q=>q.key));    
        });
    }
    
    
    [Function(nameof(GetScoutingActivities))]
    public async Task<HttpResponseData> GetScoutingActivities(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scouts/activities/{id?}")] HttpRequestData httpRequestData,
        string? id)
    {
        return await RequiresAuthentication(httpRequestData, null, async (userName, _) =>
        {
            if (id == null)
            {
                var continuationToken = httpRequestData.Query["continuationToken"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["continuationToken"]!);
                var containsText = httpRequestData.Query["containsText"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["containsText"]!);
                var anyOfActivityLocations = httpRequestData.Query["activityLocations"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["activityLocations"]!).Split([',']).Select(q=>ToEnum<ActivityLocation>(q));
                var anyOfActivityFormats = httpRequestData.Query["activityFormats"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["activityFormats"]!).Split([',']).Select(q=>ToEnum<ActivityFormat>(q));
                var anyOfActivityTypes = httpRequestData.Query["activityTypes"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["activityTypes"]!).Split([',']).Select(q=>ToEnum<ActivityType>(q));
                var anyOfWinModes = httpRequestData.Query["winModes"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["winModes"]!).Split([',']).Select(q=>ToEnum<WinMode>(q));
                var anyOfSections = httpRequestData.Query["sections"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["sections"]!).Split([',']).Select(q=>ToEnum<Section>(q));
                var anyOfOsmBadgeIds = httpRequestData.Query["osmBadgeIds"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["osmBadgeIds"]!).Split([',']).Select(q=>ToInt(q));

                var sortByColumn = httpRequestData.Query["sortBy"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["sortBy"]!);
                var offset = UrlUtilities.GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["offset"]) ??
                             0;
                var itemsPerPage = UrlUtilities.GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["itemsPerPage"]) ?? PagingConstants.ItemsPerPage;
                
                var scoutingActivities = await _scoutingRepository.GetScoutingActivitiesAsync(new GetScoutingActivitiesCriteria()
                {
                    AnyOfActivityLocations = anyOfActivityLocations,
                    AnyOfActivityFormats = anyOfActivityFormats,
                    AnyOfActivityTypes = anyOfActivityTypes,
                    AnyOfWinModes = anyOfWinModes,                    
                    AnyOfSections = anyOfSections,
                    ContributesTowardsAnyOsmBadgeId = anyOfOsmBadgeIds,
                    ContainingText = containsText,
                }, new PagedCriteria()
                {
                    ItemsPerPage = itemsPerPage,
                    Offset = offset
                });
                
                var baseUrl =
                    $"{httpRequestData.Url.Scheme}://{httpRequestData.Url.Authority}{httpRequestData.Url.AbsolutePath}";
                
                var pageUrls = CalculatePageUrls((IPagedResult<ScoutingActivity>)scoutingActivities,
                    baseUrl,
                    containsText,
                    anyOfActivityLocations,
                    anyOfActivityFormats,
                    anyOfActivityTypes,
                    anyOfWinModes,
                    anyOfSections,
                    anyOfOsmBadgeIds,
                    continuationToken, 
                    offset,
                    itemsPerPage);
                
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new PagedResponse<ScoutingActivity>((IPagedResult<ScoutingActivity>)scoutingActivities,pageUrls));
            }
            else
            {
                var scoutingActivity = (await _scoutingRepository.GetScoutingActivitiesAsync(new GetScoutingActivitiesCriteria()
                {
                    Id = id
                })).Items.FirstOrDefault();
                if (scoutingActivity==null)
                {
                    return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, nameof(ScoutingActivity));
                }
                
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new
                {
                    scoutingActivity = scoutingActivity
                });
            }
        });
    }
    
    
    
    private IEnumerable<UrlAccessiblePage> CalculatePageUrls(IPagedResult<ScoutingActivity> pagedResults, 
        string baseUrl, 
        string? containsText, 
        IEnumerable<ActivityLocation>? anyOfActivityLocations, 
        IEnumerable<ActivityFormat>? anyOfActivityFormats, 
        IEnumerable<ActivityType>? anyOfActivityTypes, 
        IEnumerable<WinMode>? winModes, 
        IEnumerable<Section>? anyOfSections, 
        IEnumerable<int>? anyOfOsmBadgeIds, 
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
                Url = BuildPageUrl(baseUrl, 
                    containsText, 
                    anyOfActivityLocations, 
                    anyOfActivityFormats, 
                    anyOfActivityTypes, 
                    winModes, 
                    anyOfSections, 
                    anyOfOsmBadgeIds, 
                    continuationToken, 
                    (pageNumber * itemsPerPage)-itemsPerPage, itemsPerPage),
                PageNumber = pageNumber,
                IsCurrentPage = pageNumber == currentPageNumber,
            });
        }
        return pageUrls;
    }
    
    
    
    private string BuildPageUrl(string baseUrl, 
        string? containsText, 
        IEnumerable<ActivityLocation>? anyOfActivityLocations, 
        IEnumerable<ActivityFormat>? anyOfActivityFormats, 
        IEnumerable<ActivityType>? anyOfActivityTypes, 
        IEnumerable<WinMode>? winModes, 
        IEnumerable<Section>? anyOfSections, 
        IEnumerable<int>? anyOfOsmBadgeIds, 
        string? continuationToken, 
        int? offset, 
        int? itemsPerPage)
    {
        var parametersDictionary = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(containsText))
        {
            parametersDictionary.Add("containsText", Uri.EscapeDataString(containsText));
        }

        if (anyOfActivityLocations != null)
        {
            anyOfActivityLocations = anyOfActivityLocations.ToArray();
            if (anyOfActivityLocations.Any())
            {
                parametersDictionary.Add("anyOfActivityLocations", Uri.EscapeDataString(string.Join(",", anyOfActivityLocations)));    
            }
        }

        if (anyOfActivityFormats != null)
        {
            anyOfActivityFormats = anyOfActivityFormats.ToArray();
            if (anyOfActivityFormats.Any())
            {
                parametersDictionary.Add("anyOfActivityFormats", Uri.EscapeDataString(string.Join(",", anyOfActivityFormats)));    
            }
        }

        if (anyOfActivityTypes != null)
        {
            anyOfActivityTypes = anyOfActivityTypes.ToArray();
            if (anyOfActivityTypes.Any())
            {
                parametersDictionary.Add("anyOfActivityTypes", Uri.EscapeDataString(string.Join(",", anyOfActivityTypes)));    
            }
        }
        
        if (winModes != null)
        {
            winModes = winModes.ToArray();
            if (winModes.Any())
            {
                parametersDictionary.Add("winModes", Uri.EscapeDataString(string.Join(",", winModes)));    
            }
        }
        
        if (anyOfSections != null)
        {
            anyOfSections = anyOfSections.ToArray();
            if (anyOfSections.Any())
            {
                parametersDictionary.Add("anyOfSections", Uri.EscapeDataString(string.Join(",", anyOfSections)));    
            }
        }
        
        if (anyOfOsmBadgeIds != null)
        {
            anyOfOsmBadgeIds = anyOfOsmBadgeIds.ToArray();
            if (anyOfOsmBadgeIds.Any())
            {
                parametersDictionary.Add("anyOfOsmBadgeIds", Uri.EscapeDataString(string.Join(",", anyOfOsmBadgeIds)));    
            }
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
    
    

    private static int ToInt(string s, int defaultValue = 0)
    {
        if (int.TryParse(s, out var result)) return result;
        return defaultValue;
    }

    private static T ToEnum<T>(string s, T defaultValue = default) where T : struct
    {
        return Enum.TryParse(s, out T result) ? result : defaultValue;
    }
}