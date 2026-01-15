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
using EmailMessage = ProgramX.Azure.FunctionApp.Model.EmailMessage;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class ResetHttpTrigger : AuthorisedHttpTriggerBase
{
    private readonly ILogger<UsersHttpTrigger> _logger;
    private readonly IResetApplication _resetApplication;


    public ResetHttpTrigger(ILogger<UsersHttpTrigger> logger,
        IConfiguration configuration,
        IResetApplication resetApplication) : base(configuration,logger)
    {
        _logger = logger;
        _resetApplication = resetApplication;
    }




    [Function(nameof(Reset))]
    public async Task<HttpResponseData> Reset(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "reset")]
        HttpRequestData httpRequestData
    )
    {
        await _resetApplication.Reset();

        return HttpResponseDataFactory.CreateForSuccessNoContent(httpRequestData);
    }

    
}