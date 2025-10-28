namespace ProgramX.Azure.FunctionApp.Model;

public class EmailRecipient
{
    public string EmailAddress { get; set; }
    public string Name { get; set; }

    public EmailRecipient(string emailAddress, string name)
    {
        EmailAddress = emailAddress;
        Name = name;
    }
}