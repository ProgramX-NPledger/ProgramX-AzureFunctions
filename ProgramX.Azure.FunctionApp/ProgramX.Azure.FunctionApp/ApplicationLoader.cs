using System.Reflection;
using Microsoft.Extensions.Configuration;
using ProgramX.Azure.FunctionApp.Contract;

namespace ProgramX.Azure.FunctionApp;

public class ApplicationLoader
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public ApplicationLoader(IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }
    
    /// <summary>
    /// Returns all names for defined Applications as stored in the Applications configuration array.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetApplicationNames()
    {
        var names = _configuration.GetSection("Applications").GetChildren().Select(q => q.Value);
        return names.Select(q => q.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Last());
    }
    
    /// <summary>
    /// Attempts to load an application by name.
    /// </summary>
    /// <param name="applicationName">The name of the Application to load.</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">Thrown if an invalid operation was attempted.</exception>
    public IApplication LoadApplication(string applicationName)
    {
        // create a tuple of (className, assemblyName, applicationName)
        var allApplications = _configuration.GetSection("Applications").GetChildren().Select(q => q.Value)
            .Select(q => q.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        
        var selectedApplication = allApplications.SingleOrDefault(q => q.Last().Equals(applicationName, StringComparison.CurrentCultureIgnoreCase));
        
        if (selectedApplication == null)
        {
            throw new InvalidOperationException($"No application found with name {applicationName}");
        }   

        if (selectedApplication.Length != 3)
        {
            throw new InvalidOperationException($"Invalid application configuration for {applicationName}. Expected 3 parts, found {selectedApplication.Length}.");
        }
        
        var className = selectedApplication[0];
        var assemblyName = selectedApplication[1];

        Assembly assembly;
        try
        {
            assembly = Assembly.Load(assemblyName);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Could not load assembly {assemblyName}", e);
        }
        
        var t = assembly.GetTypes().SingleOrDefault(q => q.FullName.Equals(className, StringComparison.CurrentCultureIgnoreCase));
        if (t == null)
        {
            throw new InvalidOperationException($"Type {className} not found in assembly {assemblyName}");
        }
        
        var ctors = t.GetConstructors();
        var matchingCtor = GetBestConstructor(ctors);
        if (matchingCtor == null) throw new InvalidOperationException($"No best constructor found for {className}");
        
        var o = matchingCtor.Invoke(GetParametersForConstructor(matchingCtor));
        if (o is IApplication)
        {
            return (IApplication)o;            
        }
        else
        {
            throw new Exception($"Type {className} in assembly {assemblyName} does not implement IApplication");
        }
    }

    private object?[]? GetParametersForConstructor(ConstructorInfo constructorInfo)
    {
        List<object> parameters = new List<object>();
        foreach (var parameter in constructorInfo.GetParameters())
        {
            // try and get parameter from the IServiceProvider
            // (hiding the service locator anti-pattern)
            var service = _serviceProvider.GetService(parameter.ParameterType);
            if (service != null)
            {
                parameters.Add(service);
                continue;
            }
        }
        return parameters.ToArray();
    }

    private ConstructorInfo? GetBestConstructor(ConstructorInfo[] ctors)
    {
        var allCtorsSortedByParametersCount = ctors.OrderByDescending(q => q.GetParameters().Length);
        return allCtorsSortedByParametersCount.FirstOrDefault();
    }
}