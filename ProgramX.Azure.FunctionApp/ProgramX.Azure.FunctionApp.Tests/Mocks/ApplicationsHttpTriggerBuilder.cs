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
    private Mock<IRoleRepository>? _mockedRoleRepository;
    private Mock<IApplicationProvider>? _mockedApplicationProvider;
    private IConfiguration? _configuration;

    public ApplicationsHttpTriggerBuilder WithIUserRepository(Action<Mock<IUserRepository>>? configure)
    {
        _mockedUserRepository = new Mock<IUserRepository>();
        configure?.Invoke(_mockedUserRepository);
        return this;
    }

    public ApplicationsHttpTriggerBuilder WithIRoleRepository(Action<Mock<IRoleRepository>>? configure)
    {
        _mockedRoleRepository = new Mock<IRoleRepository>();
        configure?.Invoke(_mockedRoleRepository);
        return this;
    }

    public ApplicationsHttpTriggerBuilder WithIApplicationProvider(Action<Mock<IApplicationProvider>>? configure)
    {
        _mockedApplicationProvider = new Mock<IApplicationProvider>();
        configure?.Invoke(_mockedApplicationProvider);
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

        var mockedServiceProvider = new Mock<IServiceProvider>();

        return new ApplicationsHttpTrigger(
            _mockedLogger!.Object,
            _configuration!,
            _mockedUserRepository!.Object,
            _mockedRoleRepository!.Object,
            _mockedApplicationProvider!.Object,
            mockedServiceProvider.Object);
    }

    private void CreateDefaultMocksWhereNotSet()
    {
        _mockedUserRepository ??= new Mock<IUserRepository>();
        _mockedRoleRepository ??= new Mock<IRoleRepository>();
        _mockedApplicationProvider ??= new Mock<IApplicationProvider>();
        _mockedLogger ??= new Mock<ILogger<ApplicationsHttpTrigger>>();
        _configuration ??= new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json")
            .Build();
    }
}
