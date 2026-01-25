namespace ProgramX.Azure.FunctionApp.Osm.Model;

public class DateRange
{
    public DateOnly? OnOrAfter { get; set; }
    public DateOnly? OnOrBefore { get; set; }
}