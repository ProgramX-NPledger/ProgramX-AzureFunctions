namespace ProgramX.Azure.FunctionApp.Model;

/// <summary>
/// A precise age, to months accuracy.
/// </summary>
/// <param name="Years">Years of age.</param>
/// <param name="Months">Additional months of age.</param>
public record struct PreciseAge(byte Years, byte Months);
