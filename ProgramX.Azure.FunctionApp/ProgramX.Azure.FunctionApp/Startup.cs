// using Azure.Core.Serialization;
// using Microsoft.Azure.Functions.Extensions.DependencyInjection;
// using Microsoft.Extensions.DependencyInjection;
// using ProgramX.Azure.FunctionApp.Tests;
//
// [assembly: FunctionsStartup(typeof(ProgramX.Azure.FunctionApp.Startup))]
//
// namespace ProgramX.Azure.FunctionApp;
//
// public class Startup : FunctionsStartup
// {
//     public override void Configure(IFunctionsHostBuilder builder)
//     {
//         builder.Services.Add(new ServiceDescriptor(typeof(ObjectSerializer),new JsonObjectSerializer(),ServiceLifetime.Singleton));
//     }
// }