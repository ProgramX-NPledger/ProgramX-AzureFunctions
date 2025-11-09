using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.HttpTriggers;

namespace ProgramX.Azure.FunctionApp.Tests.Mocks;

public class RolesHttpTriggerBuilder
{
    private Mock<ILogger<RolesHttpTrigger>>? _mockedLogger;
    private Mock<IUserRepository>? _mockedUserRepository;
    private IConfiguration? _configuration;

    public RolesHttpTriggerBuilder WithIUserRepository(Action<Mock<IUserRepository>>? mockUserRepository)
    {
        _mockedUserRepository = UserRepositoryFactory.CreateUserRepository();
        
        if (mockUserRepository!=null) mockUserRepository(_mockedUserRepository!);
        
        return this;
    }

    public RolesHttpTriggerBuilder WithLogger(Mock<ILogger<RolesHttpTrigger>> mockLogger)
    {
        _mockedLogger = mockLogger;
        return this;
    }
    
    public RolesHttpTriggerBuilder WithConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
        return this;
    }
    


    public RolesHttpTrigger Build()
    {
        CreateDefaultMocksWhereNotSet();
        
        if (_mockedLogger == null ||_mockedUserRepository==null || _configuration == null)
        {
            throw new InvalidOperationException("All dependencies must be set before building");
        }

        return new RolesHttpTrigger(
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

        // Mock for IStorageClient must be explicitly prepared

        if (_configuration == null)
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();
        }
        
        if (_mockedLogger == null)
        {
            _mockedLogger = new Mock<ILogger<RolesHttpTrigger>>();
        }
        
    }
}
