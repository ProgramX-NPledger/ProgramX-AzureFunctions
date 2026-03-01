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
    private readonly IUserRepository _userRepository;

 
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
    
    [Function(nameof(AddScore))]
    public async Task<HttpResponseData> AddScore(
        [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "scouts/scores/{ledgerId?}")]
        HttpRequestData httpRequestData,
        string ledgerId = null)
    {
        return await RequiresAuthentication(httpRequestData,["admin","scout-writer"],  async (_, _) =>
        {
            var setScoresRequest =
                await HttpBodyUtilities.GetDeserializedJsonFromHttpRequestDataBodyAsync<SetScoreRequest>(httpRequestData);
            if (setScoresRequest == null) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,"Invalid request body");

            // get term - current if not specified
            var osmTerms = (await _osmClient.GetTermsAsync(new GetTermsCriteria())).ToList();
            var osmTerm = osmTerms.SingleOrDefault(q => (q.IsCurrent && string.IsNullOrWhiteSpace(ledgerId)) ||
                                                        (q.OsmTermId.ToString() == ledgerId && !string.IsNullOrWhiteSpace(ledgerId)));
            if (osmTerm == null) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,"Invalid term");

            var scoutingScore = new ScoutingScoreItem()
            {
                id = Guid.NewGuid().ToString(),
                meetingDate = setScoresRequest.DateOfMeeting,
                osmMeetingId = setScoresRequest.OsmMeetingId,
                osmMemberId = setScoresRequest.OsmMemberId,
                osmTermId = osmTerm.OsmTermId,
                notesMarkdown = setScoresRequest.Notes,
                scoreId = setScoresRequest.ScoreId,
                score = setScoresRequest.Score
            };
            
            await _scoutingRepository.AddScoreItemAsync(scoutingScore);

            return await HttpResponseDataFactory.CreateForCreated(httpRequestData, scoutingScore, "score", scoutingScore.id);    
        });        
    }
    
    
    [Function(nameof(GetScoresLedger))]
    public async Task<HttpResponseData> GetScoresLedger(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scouts/scores/{ledgerId}")] HttpRequestData httpRequestData,
        string ledgerId)
    {
        return await RequiresAuthentication(httpRequestData, null, async (userName, _) =>
        {
            // get ledger
            var user = await _userRepository.GetUserByIdAsync(ledgerId);
            if (user==null)
            {
                return await HttpResponseDataFactory.CreateForNotFound(httpRequestData, "User");
            }
            
            List<Application> applications = user.roles.SelectMany(q=>q.applications).GroupBy(g=>g.name).Select(q=>q.First()).ToList();
            
            return await HttpResponseDataFactory.CreateForSuccess(httpRequestData, new
            {
                user,
                applications,
                profilePhotoBase64 = string.Empty
            });
        });
    }
    
    
    //
    // [Function(nameof(CreateScoresLedger))]
    // public async Task<HttpResponseData> CreateScoresLedger(
    //     [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "scouts/scores")] HttpRequestData httpRequestData
    // )
    // {
    //     return await RequiresAuthentication(httpRequestData, null,  async (_, _) =>
    //     {
    //         var createScoresLedgerRequest =
    //             await HttpBodyUtilities.GetDeserializedJsonFromHttpRequestDataBodyAsync<CreateScoresLedgerRequest>(httpRequestData);
    //         if (createScoresLedgerRequest == null) return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData,"Invalid request body");
    //
    //         // get all scores by term
    //         
    //         // get all meetings in term
    //         
    //         // get all attendances in term
    //         
    //         // er patrol with attendance
    //         
    //         // create report
    //         
    //
    //         
    //
    //         return await HttpResponseDataFactory.CreateForCreated(httpRequestData, newUser, "user", newUser.id);    
    //     });
    //  }
    
    
    
}