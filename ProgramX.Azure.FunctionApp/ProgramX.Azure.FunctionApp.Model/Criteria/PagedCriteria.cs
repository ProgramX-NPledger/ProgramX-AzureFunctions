using ProgramX.Azure.FunctionApp.Model.Constants;

namespace ProgramX.Azure.FunctionApp.Model.Criteria;

/// <summary>
/// Criteria for paging.
/// </summary>
public class PagedCriteria
{
    /// <summary>
    /// Ordinal of first item to return.
    /// </summary>
    public int Offset { get; set; } = 0;

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int ItemsPerPage { get; set; } = PagingConstants.ItemsPerPage;
}