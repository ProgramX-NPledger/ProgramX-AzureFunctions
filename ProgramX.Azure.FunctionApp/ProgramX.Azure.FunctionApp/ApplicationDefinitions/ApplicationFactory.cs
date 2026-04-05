using System.Reflection;
using System.Runtime.CompilerServices;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp.ApplicationDefinitions;

public static class ApplicationFactory
{
    /// <summary>
    /// Returns the <see cref="IApplication"/> for the given Application.
    /// </summary>
    /// <param name="dotNetAssemblyName">The formal .NET Assembly name for the implementation details for the Application</param>
    /// <param name="dotNetTypeName">The formal .NET Type name for the implementation details for the Application</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">Thrown if the meta-data cannot be instantiated.</exception>
    public static IApplication GetApplicationForApplicationName(string dotNetAssemblyName, string dotNetTypeName)
    {
        Assembly assembly;
        try
        {
            assembly = Assembly.Load(dotNetAssemblyName);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Could not load assembly {dotNetAssemblyName}",e);
        }

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
    
    /// <summary>
    /// Returns all defined applications.
    /// </summary>
    /// <returns></returns>
    /// <remarks>The Application must implement IApplication.</remarks>
    public static IEnumerable<IApplication> GetAllDefinedApplicationsWithinExecutingAssembly()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        var allTypes = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && typeof(IApplication).IsAssignableFrom(t));

        List<IApplication> allApplications = new List<IApplication>();
        foreach (var type in allTypes)
        {
            var o = Activator.CreateInstance(type);
            if (o is IApplication application)
            {
                allApplications.Add(application);
            }
        }
        return allApplications;
    }
}