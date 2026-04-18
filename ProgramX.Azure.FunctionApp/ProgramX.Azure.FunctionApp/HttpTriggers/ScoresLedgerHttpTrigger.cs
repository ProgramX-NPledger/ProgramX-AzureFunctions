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