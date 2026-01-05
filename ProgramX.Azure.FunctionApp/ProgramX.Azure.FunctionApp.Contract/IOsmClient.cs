namespace ProgramX.Azure.FunctionApp.Contract;

public interface IOsmClient : IDisposable
{
    Task<IEnumerable<object>> GetMeetings();
    Task<IEnumerable<object>> GetFlexiRecords();
    
}