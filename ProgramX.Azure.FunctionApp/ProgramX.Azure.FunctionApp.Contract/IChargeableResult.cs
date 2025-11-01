namespace ProgramX.Azure.FunctionApp.Contract;

/// <summary>
/// Represents an item that costs per request.
/// </summary>
public interface IChargeableResult
{
    /// <summary>
    /// The Request Charge that will be applied to the request.
    /// </summary>
    double RequestCharge { get; }
}