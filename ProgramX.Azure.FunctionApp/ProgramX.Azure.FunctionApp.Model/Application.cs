using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model;

public class Application
{
    public required string name { get; set; }
    public required string metaDataDotNetAssembly { get; set; }
    public required string metaDataDotNetType { get; set; }
    
    
    public string type { get; } = "application";

    public int schemaVersionNumber { get; set;  } = 2;

    public bool isDefaultApplicationOnLogin { get; set; } = false;

    public int ordinal { get; set; }

    public DateTime? createdAt { get; set; }
    public DateTime? updatedAt { get; set; }
}