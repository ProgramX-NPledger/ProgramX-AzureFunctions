using System.Reflection;
using Microsoft.Extensions.Logging;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp.ApplicationDefinitions;

public class ApplicationFactory
{

    /// <summary>
    /// 
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <returns></returns>
    public static IEnumerable<IApplication> GetAllApplications(ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<ApplicationFactory>();
        
        using (logger.BeginScope("Getting all Applications"))
        {
            // get all referenced assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            logger.LogDebug("Assemblies: {assemblies}", assemblies);
            
            var iApplicationInstances = new List<IApplication>();
            foreach (var assembly in assemblies)
            {
                // get all types that implement IApplication
                var types = assembly.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IApplication)));
                foreach (var type in types)
                {
                    if (Activator.CreateInstance(type) is not IApplication iApplication)
                    {
                        logger.LogError("Could not create instance of type {typeName} in assembly {assemblyName}",
                            type.FullName, assembly.FullName);
                    }
                    else
                    {
                        logger.LogDebug("Created instance of type {typeName} in assembly {assemblyName}", type.FullName, assembly.FullName);
                        iApplicationInstances.Add(iApplication);
                    }

                }

            }
            return iApplicationInstances;
        }    
    }
    
    /// <summary>
    /// Returns the <see cref="IApplication"/> for the given Application.
    /// </summary>
    /// <param name="dotNetAssemblyName">The formal .NET Assembly name for the implementation details for the Application</param>
    /// <param name="dotNetTypeName">The formal .NET Type name for the implementation details for the Application</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">Thrown if the meta-data cannot be instantiated.</exception>
    public static IApplication GetApplicationForApplicationName(string dotNetAssemblyName, string dotNetTypeName)
    {
        var assembly = Assembly.Load(dotNetAssemblyName);
        if (assembly==null) throw new InvalidOperationException($"Could not load assembly {dotNetAssemblyName}");

        var type = assembly.GetType(dotNetTypeName);
        if (type==null) throw new InvalidOperationException($"Could not find type {dotNetTypeName} in assembly {dotNetAssemblyName}");
    
        var o = Activator.CreateInstance(type);
        if (o == null) throw new InvalidOperationException($"Could not create instance of type {dotNetTypeName} in assembly {dotNetAssemblyName}");

        if (o is IApplication)
        {
            return (IApplication)o;
        }
        else
        {
            throw new InvalidOperationException($"Type {dotNetTypeName} in assembly {dotNetAssemblyName} does not implement IApplication");
        }
    }
}