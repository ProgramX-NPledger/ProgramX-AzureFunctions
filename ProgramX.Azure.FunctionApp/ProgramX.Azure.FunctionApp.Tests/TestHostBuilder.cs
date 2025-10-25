using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using ProgramX.Azure.FunctionApp.Core;


public static class TestHostBuilder
{
    public static IServiceProvider Create()
    {
        var builder = FunctionsApplication.CreateBuilder([]); 
        DependencyInjectionConfiguration.ConfigureServices(builder.Services);
        var app = builder.Build();
        return app.Services;
    }
}