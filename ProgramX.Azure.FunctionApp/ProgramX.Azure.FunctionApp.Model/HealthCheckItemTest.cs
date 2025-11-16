namespace ProgramX.Azure.FunctionApp.Model;

/// <summary>
/// A model for a health check item. This is written as a part of a Cosmos DB health check.
/// </summary>
public class HealthCheckItemTest
{
    /// <summary>
    /// Unique identifier for the health check item.
    /// </summary>
    public required string id { get; set; }

    /// <summary>
    /// Represents the timestamp indicating when the health check item was created.
    /// </summary>
    public required DateTime? timeStamp { get; set; }

}