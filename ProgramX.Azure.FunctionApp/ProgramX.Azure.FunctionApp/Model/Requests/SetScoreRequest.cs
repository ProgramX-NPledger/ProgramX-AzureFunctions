using System.Text.Json.Serialization;

namespace ProgramX.Azure.FunctionApp.Model.Requests;

public class SetScoreRequest
{
    public string? Notes { get; set; }

    public DateOnly DateOfMeeting { get; set; }

    public int OsmMemberId { get; set; }

    public int OsmTermId { get; set; }

    public int OsmMeetingId { get; set; }
    
    public int Score { get; set; }

    public string ScoreId { get; set; }
    
    
}