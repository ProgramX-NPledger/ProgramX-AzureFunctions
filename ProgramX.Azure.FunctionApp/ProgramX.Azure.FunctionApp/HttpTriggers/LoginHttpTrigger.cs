using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.ApplicationDefinitions;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Requests;
using ProgramX.Azure.FunctionApp.Model.Responses;

namespace ProgramX.Azure.FunctionApp.HttpTriggers;

public class LoginHttpTrigger
{
    private readonly ILogger<LoginHttpTrigger> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly JwtTokenIssuer _jwtTokenIssuer;
    private readonly IUserRepository _userRepository;

    public LoginHttpTrigger(ILogger<LoginHttpTrigger> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        JwtTokenIssuer jwtTokenIssuer,
        IUserRepository userRepository)
    {
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _jwtTokenIssuer = jwtTokenIssuer;
        _userRepository = userRepository;
    }

    [Function(nameof(Login))]
    public async Task<HttpResponseData> Login([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "login")] HttpRequestData httpRequestData)
    {
        var credentials = await httpRequestData.ReadFromJsonAsync<Credentials>();
        if (credentials == null)
        {
            _logger.LogError($"Invalid request body. Should by of type {nameof(Credentials)}, but was null");
            return await HttpResponseDataFactory.CreateForBadRequest(httpRequestData, "Invalid request body");
        }
        
        // get the user password from the database
        var userPassword = await _userRepository.GetUserPasswordByUserNameAsync(credentials.UserName);
        if (userPassword==null)
        {
            _logger.LogError("User {UserName} not found when getting password", credentials.UserName);
            return await HttpResponseDataFactory.CreateForUnauthorised(httpRequestData);
        }

        using var hmac = new HMACSHA512(userPassword.passwordSalt);
        var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(credentials.Password));

        for (var i = 0; i < computedHash.Length; i++)
            if (computedHash[i] != userPassword.passwordHash[i])
            {
                _logger.LogError("Password for user {UserName} is incorrect", credentials.UserName);
                return await HttpResponseDataFactory.CreateForUnauthorised(httpRequestData);
            }

        // password okay, get user to create JWT token
        var user = await _userRepository.GetUserByUserNameAsync(credentials.UserName);
        if (user==null)
        {
            _logger.LogError("User {UserName} not found", credentials.UserName);
            throw new Exception("User not found");
        }
        
        string token = _jwtTokenIssuer.IssueTokenForUser(credentials,user.roles.Select(q=>q.name));
 
        var applicationLoader = new ApplicationLoader(_configuration, _serviceProvider);
        
        _logger.LogInformation("User {UserName} logged in", credentials.UserName);
        
        var httpResponse = httpRequestData.CreateResponse(System.Net.HttpStatusCode.OK);
        await httpResponse.WriteAsJsonAsync(new
        {
            token,
            user.userName,
            user.emailAddress,
            roles = user.roles.Select(q=>q.name),
            // TODO: simplify this
            applications = user.roles.SelectMany(q=>q.applications).GroupBy(g=>g.name).Select(q=> 
                new FullyQualifiedApplication()
                {
                    application = q.First(),
                    applicationMetaData = applicationLoader.LoadApplication(q.First().name).GetApplicationMetaData()
                }
                ).ToList(),
            profilePhotoBase64 = string.Empty,
            user.firstName,
            user.lastName,
            initials = GetInitials(user.firstName, user.lastName),
            user.profilePhotographSmall
        });
        return httpResponse;


    }


    private string GetInitials(string? firstName, string? lastName)
    {
        StringBuilder sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(firstName))
        {
            sb.Append(firstName[0]);
        }
        if (!string.IsNullOrWhiteSpace(lastName))
        {
            sb.Append(lastName[0]);
        }
        if (sb.Length == 0) sb.Append("?");
        return sb.ToString();
    }
}