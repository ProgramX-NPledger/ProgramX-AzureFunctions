using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Requests;

public class CreateScoresLedgerRequest
{
    public required DateTime meetingDate { get; set; }

    public int OsmEveningId { get; set; } // maps to eveningid
    
    public int OsmFlexiRecordsId { get; set; } // maps to extraid

    public string? Name { get; set; }

    
}