using Azure.Core.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace ProgramX.Azure.FunctionApp.Core;

public class DependencyInjectionConfiguration
{

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ObjectSerializer, JsonObjectSerializer>();

    }
    
}