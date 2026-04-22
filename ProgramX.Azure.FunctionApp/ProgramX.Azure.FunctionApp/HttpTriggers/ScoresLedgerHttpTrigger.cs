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
using ProgramX.Azure.FunctionApp.Model.DTOs;
using ProgramX.Azure.FunctionApp.Model.DTOs.Osm.Response;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Model.Responses;
using ProgramX.Azure.FunctionApp.Osm;
using ProgramX.Azure.FunctionApp.Osm.Model.Criteria;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using EmailMessage = ProgramX.Azure.FunctionApp.Model.EmailMessage;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class ScoresLedgerHttpTrigger : AuthorisedHttpTriggerBase
{
    private readonly ILogger<ScoresLedgerHttpTrigger> _logger;
    private readonly IStorageClient? _storageClient;
    private readonly IOsmClient _osmClient;
    private readonly IScoutingRepository _scoutingRepository;

 
    public ScoresLedgerHttpTrigger(ILogger<ScoresLedgerHttpTrigger> logger,
        IStorageClient? storageClient,
        IConfiguration configuration,
        IOsmClient osmClient,
        IScoutingRepository scoutingRepository) : base(configuration,logger)
    {
        _logger = logger;
        _storageClient = storageClient;
        _osmClient = osmClient;
        _scoutingRepository = scoutingRepository;
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
            
            var newScoutingScoreItem = new ScoutingScoreItem()
            {
                id = Guid.NewGuid().ToString("N"),
                score = createScoutingScoreItemRequest.Score,
                date = DateOnly.FromDateTime(createScoutingScoreItemRequest.Date),
                osmMemberId = createScoutingScoreItemRequest.OsmScoutId,
                patrolName = createScoutingScoreItemRequest.PatrolName,
                createdAt = DateTime.Now,
                updatedAt = null,
                schemaVersionNumber = 1,
                notes = createScoutingScoreItemRequest.Notes,
                scoreName = createScoutingScoreItemRequest.ScoreName
            };
            
            await _scoutingRepository.CreateScoutingScoreItemAsync(newScoutingScoreItem);
            
            return await HttpResponseDataFactory.CreateForCreated(httpRequestData, newScoutingScoreItem, "scoutingScore", newScoutingScoreItem.id);    
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
                var continuationToken = httpRequestData.Query["continuationToken"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["continuationToken"]!);
                var patrolName = httpRequestData.Query["patrolName"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["patrolName"]!).Split(new [] {','});
                var scoreName = httpRequestData.Query["scoreName"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["scoreName"]!).Split(new [] {','});
                var onOrAfter = httpRequestData.Query["onOrAfter"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["onOrAfter"]!);
                var onOrBefore = httpRequestData.Query["onOrBefore"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["onOrBefore"]!);

                var sortByColumn = httpRequestData.Query["sortBy"]==null ? null : Uri.UnescapeDataString(httpRequestData.Query["sortBy"]!);
                var offset = UrlUtilities.GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["offset"]) ?? 0;
                var itemsPerPage = UrlUtilities.GetValidIntegerQueryStringParameterOrNull(httpRequestData.Query["itemsPerPage"]) ?? PagingConstants.ItemsPerPage;

                var criteria = new GetScoutingScoreItemsCriteria()
                {
                    PatrolNames = patrolName,
                    ScoreNames = scoreName,
                    OnOrAfter = string.IsNullOrWhiteSpace(onOrAfter) ? null : DateOnly.Parse(onOrAfter),
                    OnOrBefore = string.IsNullOrWhiteSpace(onOrBefore) ? null : DateOnly.Parse(onOrBefore)
                };
                var scoutingScoreItems = await _scoutingRepository.GetScoutingScoreItemsAsync(criteria, new PagedCriteria()
                {
                    ItemsPerPage = itemsPerPage,
                    Offset = offset
                });
                
                var baseUrl =
                    $"{httpRequestData.Url.Scheme}://{httpRequestData.Url.Authority}{httpRequestData.Url.AbsolutePath}";
                
                var pageUrls = CalculateScoutingScoreItemPageUrls((IPagedResult<ScoutingScoreItemDto>)scoutingScoreItems,
                    baseUrl,
                    criteria.PatrolNames,
                    criteria.ScoreNames,
                    criteria.OnOrAfter,
                    criteria.OnOrBefore,
                    continuationToken, 
                    offset,
                    itemsPerPage);
                
                return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new PagedResponse<ScoutingScoreItem>((IPagedResult<ScoutingScoreItem>)scoutingScoreItems,pageUrls));
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

    
    
    private IEnumerable<UrlAccessiblePage> CalculateScoutingScoreItemPageUrls(IPagedResult<ScoutingScoreItemDto> pagedResults, 
        string baseUrl, 
        IEnumerable<string>? patrolNames, 
        IEnumerable<string>? scoreNames, 
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
                Url = BuildScoutingScoreItemPageUrl(baseUrl, patrolNames, scoreNames, onOrAfter, onOrBefore, continuationToken, (pageNumber * itemsPerPage)-itemsPerPage, itemsPerPage),
                PageNumber = pageNumber,
                IsCurrentPage = pageNumber == currentPageNumber,
            });
        }
        return pageUrls;
    }
    
    
    
    private string BuildScoutingScoreItemPageUrl(string baseUrl, 
        IEnumerable<string>? patrolNames, 
        IEnumerable<string>? scoreNames, 
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

        if (scoreNames != null && scoreNames.Any())
        {
            parametersDictionary.Add("scoreNames", Uri.EscapeDataString(string.Join(",", scoreNames)));
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
                    Name = q.name,
                    CreatedAt = q.createdAt,
                    Id = q.id,
                    IsDynamicallyCalculated = q.isDynamicallyCalculated,
                    Ordinal = q.ordinal,
                    Score = q.score,
                    SchemaVersionNumber = q.schemaVersionNumber,
                    UpdatedAt = q.updatedAt
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
                id = Guid.NewGuid().ToString(),
                name = createScoresRequest.Name,
                score = createScoresRequest.Score
            };
            await _scoutingRepository.CreateScoreAsync(scoutingScore);

            return await HttpResponseDataFactory.CreateForCreated(httpRequestData, scoutingScore, "score", scoutingScore.id.ToString());    
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