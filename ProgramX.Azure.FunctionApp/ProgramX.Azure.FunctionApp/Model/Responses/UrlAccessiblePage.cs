namespace ProgramX.Azure.FunctionApp.Model.Responses;

public class UrlAccessiblePage
{
    public string Url { get; set; }
    public bool IsCurrentPage { get; set; }
    public bool IsFirstPage { get; set; }
    public bool IsLastPage { get; set; }
    public int PageNumber { get; set; }
}