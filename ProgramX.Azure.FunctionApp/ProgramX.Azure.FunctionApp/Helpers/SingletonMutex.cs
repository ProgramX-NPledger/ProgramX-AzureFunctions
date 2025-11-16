using System.Web;
using ProgramX.Azure.FunctionApp.Contract;

namespace ProgramX.Azure.FunctionApp.Helpers;

public class SingletonMutex : ISingletonMutex
{
    public const int DefaultSecondsTimeout = 60;
    
    public int SecondsTimeout { get; }
    private IDictionary<string, DateTime> _mutexes = new Dictionary<string, DateTime>();

    public SingletonMutex(int secondsTimeout = DefaultSecondsTimeout)
    {
        if (secondsTimeout <= 0) throw new ArgumentOutOfRangeException(nameof(secondsTimeout));
        
        SecondsTimeout = secondsTimeout;
    }

    public IEnumerable<string> GetMutexes()
    {
        return _mutexes.Keys.Select(q => q);
    }
    
    public bool IsRequestWithinSecondsOfMostRecentRequestOfSameType(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) name=string.Empty;
        if (_mutexes.ContainsKey(name))
        {
            return _mutexes[name] > DateTime.UtcNow.AddSeconds(-SecondsTimeout);    
        }

        return false;
    }

    public void RegisterHealthCheckForType(string name)
    {
        if (_mutexes.ContainsKey(name))
        {
            _mutexes[name] = DateTime.UtcNow;
        }
        else
        {
            _mutexes.Add(name, DateTime.UtcNow);
        }
        

        
    }
}