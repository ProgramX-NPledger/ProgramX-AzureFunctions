namespace ProgramX.Azure.FunctionApp.Model;

/// <summary>
/// A fully-qualified Application containing meta-data from the instantiated application.
/// </summary>
public class FullyQualifiedApplication
{
    /// <summary>
    /// The high-level details of the Application.
    /// </summary>
    public Application application { get; set; }
    
    /// <summary>
    /// The lower-level details of the Application.
    /// </summary>
    public ApplicationMetaData applicationMetaData { get; set; }
    
}