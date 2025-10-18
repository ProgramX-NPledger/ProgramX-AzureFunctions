using ProgramX.Azure.FunctionApp.Helpers;
using System.Reflection;

namespace ProgramX.Azure.FunctionApp.Tests.TestData;

public static class PagedCosmosDbResultExtensions
{
    /// <summary>
    /// Helper method to set TotalItems on PagedCosmosDbResult for testing
    /// </summary>
    public static void SetTotalItems<T>(this PagedCosmosDbResult<T> result, int totalItems)
    {
        var field = typeof(PagedCosmosDbResult<T>).GetProperty("TotalItems", 
            BindingFlags.Public | BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(result, totalItems);
        }
    }
}
