using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Constants;
using ProgramX.Azure.FunctionApp.Model.Criteria;
using ProgramX.Azure.FunctionApp.Model.DTOs;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Model.Responses;
using ProgramX.Azure.FunctionApp.Osm;
using ProgramX.Azure.FunctionApp.Osm.Model.Criteria;
using ProgramX.Azure.FunctionApp.Scouting.Contract;
using ProgramX.Azure.FunctionApp.Scouting.Model;
using ProgramX.Azure.FunctionApp.Scouting.Model.DTOs;
using ProgramX.Azure.FunctionApp.Scouting.Model.Requests;
using ProgramX.Azure.FunctionApp.Scouting.Model.Responses;
using ScoutingScoreItemDto = ProgramX.Azure.FunctionApp.Scouting.Model.DTOs.ScoutingScoreItemDto;

namespace ProgramX.Azure.FunctionApp.HttpTriggers.Scouting;

public class ScoresLedgerHttpTrigger : AuthorisedHttpTriggerBase
{
    private readonly ILogger<ScoresLedgerHttpTrigger> _logger;
    private readonly IStorageClient? _storageClient;
    private readonly IOsmClient _osmClient;
    private readonly IScoutingRepository _scoutingRepository;
    private readonly IOptions<OsmOptions> _osmOptions;


    public ScoresLedgerHttpTrigger(ILogger<ScoresLedgerHttpTrigger> logger,
        IStorageClient? storageClient,
        IConfiguration configuration,
        IOsmClient osmClient,
        IScoutingRepository scoutingRepository,
        IOptions<OsmOptions> osmOptions) : base(configuration,logger)
    {
        _logger = logger;
        _storageClient = storageClient;
        _osmClient = osmClient;
        _scoutingRepository = scoutingRepository;
        _osmOptions = osmOptions;
    }

    
    [Function(nameof(CreateScoutingScoreItem))]
    public async Task<HttpResponseData> CreateScoutingScoreItem(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "scouts/scores")] HttpRequestData httpRequestData
    )
    {
        return await RequiresAuthentication(httpRequestData, ["admin", "scouts-writer"],  async (_, _) =>
        {
            var createScoutingScoreItemRequest =
                await HttpBodyUtilities.GetDeserializedJsonFromHttpRequestDataBodyAsync<CreateScoutingScoreItemRequest>(httpRequestData);
            if (createScoutingScoreItemRequest == null) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,"Invalid request body");
            
            var newScoutingScoreItem = await _scoutingRepository.CreateScoutingScoreItemAsync(createScoutingScoreItemRequest.OsmScoutId, 
                DateOnly.FromDateTime(createScoutingScoreItemRequest.Date), 
                createScoutingScoreItemRequest.ScoreName, 
                createScoutingScoreItemRequest.Score, 
                createScoutingScoreItemRequest.PatrolName, 
                createScoutingScoreItemRequest.Notes);
            
            return await HttpResponseDataFactory.CreateForCreated(httpRequestData, newScoutingScoreItem, "scoutingScore", newScoutingScoreItem.Id);    
        });
     }
    
    
    
    
    
    [Function(nameof(GetScoutingScoreItemsAsync))]
    public async Task<HttpResponseData> GetScoutingScoreItemsAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scouts/scoresledger/{id?}")] HttpRequestData httpRequestData,
        string? id)
    { 
        return await RequiresAuthentication(httpRequestData, ["admin","scouts-reader"], async (_, _) =>
        {
            if (id == null)
            {
                var currentTerm = (await _osmClient.GetTermsAsync(new GetTermsCriteria()
                {
                    SectionId = _osmOptions.Value.SectionId
                })).Where(q => q.IsCurrent);
                if (currentTerm.Count() != 1)
                {
                    return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "Term");
                }
                
                var members = await _osmClient.GetMembersAsync(new GetMembersCriteria()
                { 
                    SectionId = _osmOptions.Value.SectionId,
                    TermId = currentTerm.Single().OsmTermId 
                });
                
                var continuationToken = httpRequestData.Query["continuationToken"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["continuationToken"]!);
                var patrolNames = httpRequestData.Query["patrolName"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["patrolNames"]!).Split(new [] {','});
                var scoreIds = httpRequestData.Query["scoreIds"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["scoreIds"]!).Split(new [] {','});
                var onOrAfter = httpRequestData.Query["onOrAfter"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["onOrAfter"]!);
                var onOrBefore = httpRequestData.Query["onOrBefore"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["onOrBefore"]!);
                var osmMemberIds = httpRequestData.Query["osmMemberIds"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["osmMemberIds"]!).Split(new [] {','});
                
                var sortByColumn = httpRequestData.Query["sortBy"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["sortBy"]!);
                var offset = UrlUtilities.GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["offset"]) ?? 0;
                var itemsPerPage = UrlUtilities.GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["itemsPerPage"]) ?? PagingConstants.ItemsPerPage;
    
                var criteria = new GetScoutingScoreItemsCriteria()
                {
                    PatrolNames = patrolNames,
                    ScoreIds = scoreIds,
                    OnOrAfter = string.IsNullOrWhiteSpace(onOrAfter) ? null : DateOnly.Parse(onOrAfter),
                    OnOrBefore = string.IsNullOrWhiteSpace(onOrBefore) ? null : DateOnly.Parse(onOrBefore)
                };
                if (osmMemberIds != null && osmMemberIds.Any())
                {
                    criteria.OsmMemberIds = osmMemberIds.Where(q => int.TryParse(q, out _))
                        .Select(q => int.Parse(q))
                        .ToArray();
                }
                var scoutingScoreItems = await _scoutingRepository.GetScoutingScoreItemsAsync(criteria, new PagedCriteria()
                {
                    ItemsPerPage = itemsPerPage,
                    Offset = offset
                });
                
                var baseUrl =
                    $"{httpRequestData.Url.Scheme}://{httpRequestData.Url.Authority}{httpRequestData.Url.AbsolutePath}";
                
                var pageUrls = CalculateScoutingScoreItemPageUrls((IPagedResult<ScoutingScoreItem>)scoutingScoreItems,
                    baseUrl,
                    criteria.PatrolNames,
                    criteria.ScoreIds,
                    criteria.OsmMemberIds,
                    criteria.OnOrAfter,
                    criteria.OnOrBefore,
                    continuationToken, 
                    offset,
                    itemsPerPage);
                
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new PagedResponse<ScoutingScoreItem, ScoutingScoreItemDto>((IPagedResult<ScoutingScoreItem>)scoutingScoreItems,pageUrls,(scoutingScoreItem) =>
                    new ScoutingScoreItemDto()
                    {
                        CreatedAt = scoutingScoreItem.CreatedAt,
                        Date = scoutingScoreItem.Date,
                        Id = scoutingScoreItem.Id,
                        MemberName = members.SingleOrDefault(q => q.OsmScoutId == scoutingScoreItem.OsmMemberId)?.FullName ?? "(unknown)",
                        Notes = scoutingScoreItem.Notes,
                        OsmMemberId = scoutingScoreItem.OsmMemberId,
                        PatrolName = scoutingScoreItem.PatrolName,
                        Score = scoutingScoreItem.Score,
                        ScoreName = scoutingScoreItem.ScoreName,
                        UpdatedAt = scoutingScoreItem.UpdatedAt
                    }));
            }
            else
            {
                var scoutingScoreItem = await _scoutingRepository.GetScoutingScoreItemByIdAsync(id);
                if (scoutingScoreItem==null)
                {
                    return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "ScoutingScoreItem");
                }
                
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new
                {
                    user = scoutingScoreItem
                });
            }
            
        });
    }

    
    
    private IEnumerable<UrlAccessiblePage> CalculateScoutingScoreItemPageUrls(IPagedResult<ScoutingScoreItem> pagedResults, 
        string baseUrl, 
        IEnumerable<string>? patrolNames, 
        IEnumerable<string>? scoreIds, 
        IEnumerable<int>? osmMemberIds,
        DateOnly? onOrAfter, 
        DateOnly? onOrBefore, 
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
                Url = BuildScoutingScoreItemPageUrl(baseUrl, patrolNames, scoreIds, osmMemberIds, onOrAfter, onOrBefore, continuationToken, (pageNumber * itemsPerPage)-itemsPerPage, itemsPerPage),
                PageNumber = pageNumber,
                IsCurrentPage = pageNumber == currentPageNumber,
            });
        }
        return pageUrls;
    }
    
    
    
    private string BuildScoutingScoreItemPageUrl(string baseUrl, 
        IEnumerable<string>? patrolNames, 
        IEnumerable<string>? scoreIds, 
        IEnumerable<int>? osmMemberIds,
        DateOnly? onOrAfter, 
        DateOnly? onOrBefore, 
        string? continuationToken,
        int? offset, 
        int? itemsPerPage)
    {
        var parametersDictionary = new Dictionary<string, string>();
        if (patrolNames != null && patrolNames.Any())
        {
            parametersDictionary.Add("patrolNames", Uri.EscapeDataString(string.Join(",", patrolNames)));
        }

        if (scoreIds != null && scoreIds.Any())
        {
            parametersDictionary.Add("scoreIds", Uri.EscapeDataString(string.Join(",", scoreIds)));
        }
        if (osmMemberIds != null && osmMemberIds.Any())
        {
            parametersDictionary.Add("osmMemberIds", Uri.EscapeDataString(string.Join(",", osmMemberIds)));
        }

        if (onOrAfter != null)
        {
            parametersDictionary.Add("onOrAfter", onOrAfter.Value.ToString("yyyy-MM-dd"));
        }
        
        if (onOrBefore != null)
        {
            parametersDictionary.Add("onOrBefore", onOrBefore.Value.ToString("yyyy-MM-dd"));
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
    
    
    
    
    
    
    
    [Function(nameof(GetScoresAsync))]
    public async Task<HttpResponseData> GetScoresAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scouts/scores")] HttpRequestData httpRequestData)
    { 
        return await RequiresAuthentication(httpRequestData, ["admin","scouts-reader"], async (_, _) =>
        {
            var scoutingScores = (await _scoutingRepository.GetScoutingScoresAsync(new GetScoutingScoresCriteria())).Items.ToList();
            
            var getScoutingScoresResponse = new GetScoutingScoresResponse()
            {
                Items = scoutingScores.Select(q => new ScoutingScoreDto()
                {
                    Id = q.Id,
                    Name = q.Name,
                    CreatedAt = q.CreatedAt,
                    IsDynamicallyCalculated = q.IsDynamicallyCalculated,
                    Ordinal = q.Ordinal,
                    Score = q.Score,
                    UpdatedAt = q.UpdatedAt
                }).ToList()
            };
            
            return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, getScoutingScoresResponse);
        });
    }

    
    
    
    [Function(nameof(CreateScore))]
    public async Task<HttpResponseData> CreateScore(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "scouts/scores/points")]
        HttpRequestData httpRequestData)
    {
        return await RequiresAuthentication(httpRequestData,["admin","scout-writer"],  async (_, _) =>
        {
            var createScoresRequest =
                await HttpBodyUtilities.GetDeserializedJsonFromHttpRequestDataBodyAsync<CreateScoreRequest>(httpRequestData);
            if (createScoresRequest == null) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,"Invalid request body");

            var scoutingScore = new ScoutingScore()
            {
                Id = Guid.NewGuid().ToString(),
                Name = createScoresRequest.Name,
                Score = createScoresRequest.Score
            };
            await _scoutingRepository.CreateScoreAsync(scoutingScore);

            return await HttpResponseDataFactory.CreateForCreated(httpRequestData, scoutingScore, "score", scoutingScore.Id.ToString());    
        });        
    }
    //
    // [Function(nameof(AddScore))]
    // public async Task<HttpResponseData> AddScore(
    //     [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "scouts/scores/{ledgerId?}")]
    //     HttpRequestData httpRequestData,
    //     string ledgerId = null)
    // {
    //     return await RequiresAuthentication(httpRequestData,["admin","scout-writer"],  async (_, _) =>
    //     {
    //         var setScoresRequest =
    //             await HttpBodyUtilities.GetDeserializedJsonFromHttpRequestDataBodyAsync<SetScoreRequest>(httpRequestData);
    //         if (setScoresRequest == null) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,"Invalid request body");
    //
    //         // get term - current if not specified
    //         var osmTerms = (await _osmClient.GetTermsAsync(new GetTermsCriteria())).ToList();
    //         var osmTerm = osmTerms.SingleOrDefault(q => (q.IsCurrent && string.IsNullOrWhiteSpace(ledgerId)) ||
    //                                                     (q.OsmTermId.ToString() == ledgerId && !string.IsNullOrWhiteSpace(ledgerId)));
    //         if (osmTerm == null) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,"Invalid term");
    //
    //         var scoutingScore = new ScoutingScoreItem()
    //         {
    //             id = Guid.NewGuid().ToString(),
    //             date = setScoresRequest.DateOfMeeting,
    //             osmMeetingId = setScoresRequest.OsmMeetingId,
    //             osmMemberId = setScoresRequest.OsmMemberId,
    //             osmTermId = osmTerm.OsmTermId,
    //             notesMarkdown = setScoresRequest.Notes,
    //             scoreId = setScoresRequest.ScoreId,
    //             score = setScoresRequest.Score
    //         };
    //         
    //         await _scoutingRepository.AddScoreItemAsync(scoutingScore);
    //
    //         return await HttpResponseDataFactory.CreateForCreated(httpRequestData, scoutingScore, "score", scoutingScore.id);    
    //     });        
    // }
    
    
    
}