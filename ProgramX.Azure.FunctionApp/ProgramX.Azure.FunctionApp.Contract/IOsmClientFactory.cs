namespace ProgramX.Azure.FunctionApp.Contract;

public interface IOsmClientFactory
{
    IOsmClient CreateClient();
    
}