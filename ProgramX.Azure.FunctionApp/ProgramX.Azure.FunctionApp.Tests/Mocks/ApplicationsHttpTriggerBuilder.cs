using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.HttpTriggers;

namespace ProgramX.Azure.FunctionApp.Tests.Mocks;

public class ApplicationsHttpTriggerBuilder
{
    private Mock<ILogger<ApplicationsHttpTrigger>>? _mockedLogger;
    private Mock<IUserRepository>? _mockedUserRepository;
    private IConfiguration? _configuration;

    public ApplicationsHttpTriggerBuilder WithIUserRepository(Action<Mock<IUserRepository>>? mockUserRepository)
    {
        _mockedUserRepository = UserRepositoryFactory.CreateUserRepository();
        
        if (mockUserRepository!=null) mockUserRepository(_mockedUserRepository!);
        
        return this;
    }

    public ApplicationsHttpTriggerBuilder WithLogger(Mock<ILogger<ApplicationsHttpTrigger>> mockLogger)
    {
        _mockedLogger = mockLogger;
        return this;
    }
    
    public ApplicationsHttpTriggerBuilder WithConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
        return this;
    }
    


    public ApplicationsHttpTrigger Build()
    {
        CreateDefaultMocksWhereNotSet();
        
        if (_mockedLogger == null || _mockedUserRepository==null || _configuration == null)
        {
            throw new InvalidOperationException("All dependencies must be set before building");
        }

        return new ApplicationsHttpTrigger(
            _mockedLogger.Object,
            _configuration,
            _mockedUserRepository.Object);
    }

    private void CreateDefaultMocksWhereNotSet()
    {
        if (_mockedUserRepository == null)
        {
            WithIUserRepository(null);
        }

        if (_configuration == null)
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();
        }
        
        if (_mockedLogger == null)
        {
            _mockedLogger = new Mock<ILogger<ApplicationsHttpTrigger>>();
        }
        
    }
}
