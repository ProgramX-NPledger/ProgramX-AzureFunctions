using System.Collections.Specialized;

namespace ProgramX.Azure.FunctionApp.Model;

public class ScoutingScoreItem
{
    public string id { get; set; }
    public int osmMemberId { get; set; }
    public DateOnly date { get; set; }
    public string? notes { get; set; }
    public string scoreName { get; set; }
    public string? patrolName  { get; set; }
    public int score { get; set; }
    public int schemaVersionNumber { get; set; }
    public DateTime createdAt { get; set; }
    public DateTime? updatedAt { get; set; }
}