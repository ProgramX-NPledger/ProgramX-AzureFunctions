using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

public class Application
{
    public required string name { get; set; }
    public string? description { get; set; }
    public string? imageUrl { get; set; }
    public required string targetUrl { get; set; }
    public string type { get; } = "application";

    public int versionNumber { get; } = 1;

    public bool isDefaultApplicationOnLogin { get; set; } = false;

    public int ordinal { get; set; }
    

}