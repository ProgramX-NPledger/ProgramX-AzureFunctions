namespace ProgramX.Azure.FunctionApp.Contract;

public interface ISingletonMutex
{
    void RegisterHealthCheckForType(string name);
    bool IsRequestWithinSecondsOfMostRecentRequestOfSameType(string? name);
    
}