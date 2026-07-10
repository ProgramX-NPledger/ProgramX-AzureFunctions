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
    private Mock<IRoleRepository>? _mockedRoleRepository;
    private IConfiguration? _configuration;

    public UsersHttpTriggerBuilder WithIUserRepository(Action<Mock<IUserRepository>>? configure)
    {
        _mockedUserRepository = new Mock<IUserRepository>();
        configure?.Invoke(_mockedUserRepository);
        return this;
    }

    public UsersHttpTriggerBuilder WithIRoleRepository(Action<Mock<IRoleRepository>>? configure)
    {
        _mockedRoleRepository = new Mock<IRoleRepository>();
        configure?.Invoke(_mockedRoleRepository);
        return this;
    }

    public UsersHttpTriggerBuilder WithLogger(Mock<ILogger<UsersHttpTrigger>> mockLogger)
    {
        _mockedLogger = mockLogger;
        return this;
    }

    public UsersHttpTriggerBuilder WithStorageClient(Action<Mock<IStorageClient>> configure)
    {
        _mockedStorageClient = new Mock<IStorageClient>();
        configure(_mockedStorageClient);
        return this;
    }

    public UsersHttpTriggerBuilder WithEmailSender(Action<Mock<IEmailSender>> configure)
    {
        _mockedEmailSender = new Mock<IEmailSender>();
        configure(_mockedEmailSender);
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

        return new UsersHttpTrigger(
            _mockedLogger!.Object,
            _mockedStorageClient?.Object,
            _configuration!,
            _mockedEmailSender!.Object,
            _mockedUserRepository!.Object,
            _mockedRoleRepository!.Object);
    }

    private void CreateDefaultMocksWhereNotSet()
    {
        _mockedUserRepository ??= new Mock<IUserRepository>();
        _mockedRoleRepository ??= new Mock<IRoleRepository>();
        _mockedEmailSender ??= new Mock<IEmailSender>();
        _mockedLogger ??= new Mock<ILogger<UsersHttpTrigger>>();
        _configuration ??= new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json")
            .Build();
    }
}
