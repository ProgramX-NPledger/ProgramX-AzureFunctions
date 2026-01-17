using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.HttpTriggers;

namespace ProgramX.Azure.FunctionApp.Tests.Mocks;

public class HealthCheckHttpTriggerBuilder
{
    private Mock<ILoggerFactory>? _mockedLoggerFactory;
    private IConfiguration? _configuration;
    private Mock<ISingletonMutex>? _mockedSingletonMutex;

    public HealthCheckHttpTriggerBuilder WithSingletonMutext(Action<Mock<ISingletonMutex>>? mockSingletonMutex)
    {
        _mockedSingletonMutex = new Mock<ISingletonMutex>();
        
        if (mockSingletonMutex!=null) mockSingletonMutex(_mockedSingletonMutex!);
        
        return this;
    }

    public HealthCheckHttpTriggerBuilder WithLoggerFactory(Mock<ILoggerFactory> mockLoggerFactory)
    {
        _mockedLoggerFactory = mockLoggerFactory;
        return this;
    }
    
    public HealthCheckHttpTriggerBuilder WithConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
        return this;
    }
    


    public HealthCheckHttpTrigger Build()
    {
        CreateDefaultMocksWhereNotSet();
        
        if (_mockedLoggerFactory == null || _configuration == null || _mockedSingletonMutex == null)
        {
            throw new InvalidOperationException("All dependencies must be set before building");
        }

        return new HealthCheckHttpTrigger(
            _mockedLoggerFactory.Object,
            _mockedLoggerFactory.Object.CreateLogger<HealthCheckHttpTrigger>(),
            _configuration,
            _mockedSingletonMutex.Object);
    }

    private void CreateDefaultMocksWhereNotSet()
    {
        if (_mockedLoggerFactory == null)
        {
            _mockedLoggerFactory = new Mock<ILoggerFactory>();
        }

        if (_mockedSingletonMutex == null)
        {
            WithSingletonMutext(null);
        }
        
        if (_configuration == null)
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();
        }
        
    }
}
