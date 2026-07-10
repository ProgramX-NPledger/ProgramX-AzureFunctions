using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.HttpTriggers;
using ProgramX.Azure.FunctionApp.Model;
using ProgramX.Azure.FunctionApp.Model.Criteria;

namespace ProgramX.Azure.FunctionApp.Tests.Mocks;

public class RolesHttpTriggerBuilder
{
    private Mock<ILogger<RolesHttpTrigger>>? _mockedLogger;
    private Mock<IRoleRepository>? _mockedRoleRepository;
    private Mock<IUserRepository>? _mockedUserRepository;
    private Mock<IApplicationProvider>? _mockedApplicationProvider;
    private IConfiguration? _configuration;

    public RolesHttpTriggerBuilder WithIRoleRepository(Action<Mock<IRoleRepository>>? configure)
    {
        _mockedRoleRepository = new Mock<IRoleRepository>();
        configure?.Invoke(_mockedRoleRepository);
        return this;
    }

    public RolesHttpTriggerBuilder WithIUserRepository(Action<Mock<IUserRepository>>? configure)
    {
        _mockedUserRepository = new Mock<IUserRepository>();
        configure?.Invoke(_mockedUserRepository);
        return this;
    }

    public RolesHttpTriggerBuilder WithIApplicationProvider(Action<Mock<IApplicationProvider>>? configure)
    {
        _mockedApplicationProvider = new Mock<IApplicationProvider>();
        configure?.Invoke(_mockedApplicationProvider);
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

        return new RolesHttpTrigger(
            _mockedLogger!.Object,
            _configuration!,
            _mockedRoleRepository!.Object,
            _mockedUserRepository!.Object,
            _mockedApplicationProvider!.Object);
    }

    private void CreateDefaultMocksWhereNotSet()
    {
        if (_mockedRoleRepository == null)
        {
            _mockedRoleRepository = new Mock<IRoleRepository>();
            var emptyRolesResult = new Mock<IResult<Role>>();
            emptyRolesResult.SetupGet(x => x.Items).Returns([]);
            _mockedRoleRepository.Setup(x => x.GetRolesAsync(It.IsAny<GetRolesCriteria>(), It.IsAny<PagedCriteria>()))
                .ReturnsAsync(emptyRolesResult.Object);
        }

        if (_mockedUserRepository == null)
        {
            _mockedUserRepository = new Mock<IUserRepository>();
            var emptyUsersResult = new Mock<IResult<User>>();
            emptyUsersResult.SetupGet(x => x.Items).Returns([]);
            _mockedUserRepository.Setup(x => x.GetUsersAsync(It.IsAny<GetUsersCriteria>(), It.IsAny<PagedCriteria>()))
                .ReturnsAsync(emptyUsersResult.Object);
        }

        if (_mockedApplicationProvider == null)
        {
            _mockedApplicationProvider = new Mock<IApplicationProvider>();
            _mockedApplicationProvider
                .Setup(x => x.GetAllApplications(It.IsAny<GetAllApplicationsCriteria>()))
                .Returns([]);
        }

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
