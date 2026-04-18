using System.Collections.Specialized;

namespace ProgramX.Azure.FunctionApp.Model;

public class ScoutingScore
{
    public string id { get; set; }
    public string name { get; set; }
    public int score { get; set; }
    public bool isDynamicallyCalculated { get; set; }
    public int ordinal { get; set; }

    public int schemaVersionNumber { get; set; }

    public DateTime createdAt { get; set; }

    public DateTime? updatedAt { get; set; }
}