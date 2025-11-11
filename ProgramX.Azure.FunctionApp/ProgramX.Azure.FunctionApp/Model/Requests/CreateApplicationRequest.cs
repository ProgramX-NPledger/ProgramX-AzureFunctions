namespace ProgramX.Azure.FunctionApp.Model.Requests;

public class CreateApplicationRequest
{
    public required string name { get; set; }
    public string? description { get; set; }
    public string? imageUrl { get; set; }
    public required string targetUrl { get; set; }

    public bool isDefaultApplicationOnLogin { get; set; } = false;

    public int ordinal { get; set; }

    public IEnumerable<string> addToRoles { get; set; }
    
}