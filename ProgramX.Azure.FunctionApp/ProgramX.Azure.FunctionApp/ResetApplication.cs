using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Contract;

namespace ProgramX.Azure.FunctionApp;

public class ResetApplication(
    IUserRepository userRepository,
    ILoggerFactory loggerFactory)
    : IResetApplication
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ILogger<ResetApplication> _logger = loggerFactory.CreateLogger<ResetApplication>();
    
    public async Task Reset()
    {
        using (_logger.BeginScope("Reset Application"))
        {
            // to be able to reset, the application must satisfy the following:
            // 1. there can be no users or userPasswords containers

            _logger.LogInformation("Resetting using {userRepository}",nameof(IUserRepository));
            await _userRepository.ResetApplicationAsync();
            
            _logger.LogInformation("Reset complete");
        }
        
    }
}