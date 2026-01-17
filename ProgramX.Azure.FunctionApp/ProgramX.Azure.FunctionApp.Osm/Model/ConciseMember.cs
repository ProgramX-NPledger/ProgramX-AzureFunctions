namespace ProgramX.Azure.FunctionApp.Osm.Model;

public class ConciseMember
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int OsmScoutId { get; set; }
    public Guid? OsmPhotoGuid { get; set; }
    
}