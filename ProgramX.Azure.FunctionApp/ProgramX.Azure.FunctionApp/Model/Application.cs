namespace ProgramX.Azure.FunctionApp.Model;

public class Application
{
    public required string id { get; set; }
    public required string name { get; set; }
    public string? imageUrl { get; set; }
    public required string targetUrl { get; set; }
    
}