using System.Web;
using ProgramX.Azure.FunctionApp.Contract;

namespace ProgramX.Azure.FunctionApp.Helpers;

public class SingletonMutex : ISingletonMutex
{
    private IDictionary<string, DateTime> _mutexes = new Dictionary<string, DateTime>();
    
    public bool IsRequestWithinSecondsOfMostRecentRequestOfSameType(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) name=string.Empty;
        if (_mutexes.ContainsKey(name))
        {
            return _mutexes[name] > DateTime.UtcNow.AddSeconds(-60);    
        }

        return false;
    }

    public void RegisterHealthCheckForType(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) name=string.Empty;

        if (_mutexes.ContainsKey(name))
        {
            _mutexes[name] = DateTime.UtcNow;
        }

        _mutexes.Add(name, DateTime.UtcNow);
        

        
    }
}