using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.HttpTriggers;

namespace ProgramX.Azure.FunctionApp.Tests.Mocks;

public class UsersHttpTriggerBuilder
{
    private Mock<ILogger<UsersHttpTrigger>>? _mockedLogger;
    private Mock<IStorageClient>? _mockedStorageClient;
    private Mock<IEmailSender>? _mockedEmailSender;
    private Mock<IUserRepository>? _mockedUserRepository;
    private IConfiguration? _configuration;


    public UsersHttpTriggerBuilder WithLogger(Mock<ILogger<UsersHttpTrigger>> mockLogger)
    {
        _mockedLogger = mockLogger;
        return this;
    }

    public UsersHttpTriggerBuilder WithStorageClient(Mock<IStorageClient> mockStorageClient)
    {
        _mockedStorageClient = mockStorageClient;
        return this;
    }

    public UsersHttpTriggerBuilder WithEmailSender(Mock<IEmailSender> mockEmailSender)
    {
        _mockedEmailSender = mockEmailSender;
        return this;
    }
    
    public UsersHttpTriggerBuilder WithUserRepository(Mock<IUserRepository> mockUserRepository)
    {
        _mockedUserRepository = mockUserRepository;
        return this;
    }
    
    public UsersHttpTriggerBuilder WithConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
        return this;
    }
    


    public UsersHttpTrigger Build()
    {
        CreateDefaultMocksWhereNotSet();
        
        if (_mockedLogger == null || _mockedEmailSender== null || _mockedUserRepository==null || _mockedStorageClient == null || _configuration == null)
        {
            throw new InvalidOperationException("All dependencies must be set before building");
        }

        return new UsersHttpTrigger(
            _mockedLogger.Object,
            _mockedStorageClient.Object,
            _configuration,
            _mockedEmailSender.Object,
            _mockedUserRepository.Object);
    }

    private void CreateDefaultMocksWhereNotSet()
    {
        
        RepositoryFactory repositoryFactory = new RepositoryFactory();
        if (_mockedUserRepository == null)
        {
            _mockedUserRepository = repositoryFactory.CreateUserRepository();       
        }

        if (_mockedEmailSender == null)
        {
            _mockedEmailSender = new Mock<IEmailSender>();      
        }
        
        if (_mockedStorageClient == null)
        {
            _mockedStorageClient = new Mock<IStorageClient>();       
        }

        if (_configuration == null)
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();
        }
        
        if (_mockedLogger == null)
        {
            _mockedLogger = new Mock<ILogger<UsersHttpTrigger>>();
        }
        
    }
}
