using System.Reflection;
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