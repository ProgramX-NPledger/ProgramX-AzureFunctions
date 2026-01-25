namespace ProgramX.Azure.FunctionApp.Osm.Model;

[Flags]
public enum FlexiRecordInclude
{
    None=0,
    DateOfBirth=1,
    Age=2,
    Patrol=4,
}