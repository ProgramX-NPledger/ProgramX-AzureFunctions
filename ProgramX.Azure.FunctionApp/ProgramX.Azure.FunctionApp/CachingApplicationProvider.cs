using System.Reflection;
using Microsoft.Extensions.Configuration;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model.Criteria;
using ProgramX.Azure.FunctionApp.Model.Exceptions;

namespace ProgramX.Azure.FunctionApp;

public class CachingApplicationProvider : IApplicationProvider
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private Dictionary<string, IApplication> _applications = new Dictionary<string, IApplication>();
    private DateTime? _lastRefreshed = null;
    private int _cacheAgeSecs = 60 * 10; // 10 minutes

    public CachingApplicationProvider(IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }
    
    /// <inheritdoc/>
    public IEnumerable<IApplication> GetAllApplications(GetAllApplicationsCriteria criteria)
    {
        EnsureAllApplications();

        // we start with everything and remove non-matches
        // this results in a logical AND being applied across the criteria
        var applications = new List<IApplication>(_applications.Values);
        
        if (!string.IsNullOrWhiteSpace(criteria.ApplicationName))
        {
            applications = applications.Where(a => a.GetApplicationMetaData().Name == criteria.ApplicationName).ToList();
        }
        
        if (criteria.ApplicationNames != null && criteria.ApplicationNames.Any())
        {
            applications = applications.Where(a => criteria.ApplicationNames.Contains(a.GetApplicationMetaData().Name)).ToList();
        }
        
        if (!string.IsNullOrWhiteSpace(criteria.ContainingText))
        {
            applications = applications.Where(a => a.GetApplicationMetaData().Name.Contains(criteria.ContainingText, StringComparison.CurrentCultureIgnoreCase)).ToList();
        }
        
        if (criteria.HasAnyOfRoles != null && criteria.HasAnyOfRoles.Any())
        {
            applications = applications.Where(a => a.GetApplicationMetaData().RequiresRoleNames.Any(r => criteria.HasAnyOfRoles.Contains(r))).ToList();
        }

        return applications;
    }

    /// <inheritdoc/>
    public IApplication? GetApplication(string applicationName)
    {
        EnsureAllApplications();
        if (_applications.ContainsKey(applicationName)) return _applications[applicationName];
        return null;
    }

    private void EnsureAllApplications()
    {
        if (ShouldLoadApplications()) LoadAllApplications();    
    }
    
    private bool ShouldLoadApplications()
    {
        return _lastRefreshed == null || _lastRefreshed.Value < DateTime.UtcNow.AddSeconds(_cacheAgeSecs);
    }
    
    private void LoadAllApplications()
    {
        lock (_applications)
        {
            _applications.Clear();

            // uses discovery to load all applications
            var allAssemblies = GetAllAssemblies();
            foreach (var assembly in allAssemblies)
            {
                var allTypesInAssembly = assembly.GetTypes();
                foreach (var type in allTypesInAssembly)
                {
                    if (type.IsClass && type.IsAssignableTo(typeof(IApplication)))
                    {
                        // attempt to instantiate type
                        var ctors = type.GetConstructors();
                        var matchingCtor = GetBestConstructor(ctors);
                        if (matchingCtor == null) throw new InvalidOperationException($"No best constructor found for {type.Name}");
        
                        var o = matchingCtor.Invoke(GetParametersForConstructor(matchingCtor));
                        IApplication iApplication = (IApplication)o;
                        _applications.Add(iApplication.GetApplicationMetaData().Name,iApplication);       
                    }
                }
            }
        }
        
    }

    private List<Assembly> GetAllAssemblies()
    {
        // TODO discover all assemblies
        return new List<Assembly>
        {
            Assembly.GetExecutingAssembly(),
            Assembly.GetCallingAssembly()
        }.Distinct().ToList();
        
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