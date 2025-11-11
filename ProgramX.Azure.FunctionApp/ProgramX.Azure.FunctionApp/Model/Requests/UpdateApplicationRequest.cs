using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Requests;

public class UpdateApplicationRequest
{
    public string? name { get; set; }
    
    public string? decription { get; set; }
    
    public string? imageUrl { get; set; }
    public required string targetUrl { get; set; }

    public bool isDefaultApplicationOnLogin { get; set; } = false;

    public int ordinal { get; set; }


    
}