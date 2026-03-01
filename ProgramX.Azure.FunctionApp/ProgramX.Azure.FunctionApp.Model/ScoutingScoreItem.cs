using System.Collections.Specialized;

namespace ProgramX.Azure.FunctionApp.Model;

public class ScoutingScoreItem
{
    public string id { get; set; }
    public int osmMemberId { get; set; }
    public int? osmMeetingId { get; set; }
    public DateOnly meetingDate { get; set; }
    public int osmTermId { get; set; }
    public string? notesMarkdown { get; set; }
    public string scoreId { get; set; }
    public int score { get; set; }
}