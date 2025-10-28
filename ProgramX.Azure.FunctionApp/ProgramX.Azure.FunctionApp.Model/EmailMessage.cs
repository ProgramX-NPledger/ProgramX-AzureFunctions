namespace ProgramX.Azure.FunctionApp.Model;

public class EmailMessage
{
    public required EmailRecipient From { get; set; } 
    public required IEnumerable<EmailRecipient> To { get; set; }
    public IEnumerable<EmailRecipient>? Cc { get; set; }
    public IEnumerable<EmailRecipient>? Bcc { get; set; }
    public required string Subject { get; set; }
    public required string PlainTextBody { get; set; }
    public required string HtmlBody { get; set; }
    
}