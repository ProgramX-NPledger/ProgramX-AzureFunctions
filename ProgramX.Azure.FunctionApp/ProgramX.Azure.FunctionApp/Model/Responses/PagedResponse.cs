namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class PagedResponse<T>
{
    public IEnumerable<T> Items { get; set; }
    public string? ContinuationToken { get; set; }
    public int? ItemsPerPage { get; set; }
    public bool IsLastPage { get; set; }
    
}

