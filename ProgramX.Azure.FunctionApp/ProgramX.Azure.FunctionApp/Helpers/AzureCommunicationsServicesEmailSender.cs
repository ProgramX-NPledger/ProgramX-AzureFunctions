using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;
using ProgramX.Azure.FunctionApp.Model.Responses;

namespace ProgramX.Azure.FunctionApp.Helpers;

public class AzureCommunicationsServicesEmailSender : Contract.IEmailSender
{
    private readonly EmailClient _emailClient;
    
    public AzureCommunicationsServicesEmailSender(IConfiguration configuration)
    {
        _emailClient = new EmailClient(configuration["AzureCommunicationServicesConnection"]);
    }

    public async Task SendEmailAsync(ProgramX.Azure.FunctionApp.Model.EmailMessage emailMessage)
    {
        var emailContent = new EmailContent(emailMessage.Subject)
        {
            PlainText = emailMessage.PlainTextBody,
            Html = emailMessage.HtmlBody
        };

        var recipients = new EmailRecipients(
            emailMessage.To.Select(q => new EmailAddress(q.EmailAddress, q.Name)),
            emailMessage.Cc!=null && emailMessage.Cc.Any() ? emailMessage.Cc.Select(q => new EmailAddress(q.EmailAddress, q.Name)) : null,
            emailMessage.Bcc!=null && emailMessage.Bcc.Any() ? emailMessage.Bcc.Select(q => new EmailAddress(q.EmailAddress, q.Name)) : null       
            );
        
        var message = new EmailMessage(
            senderAddress: emailMessage.From.EmailAddress,
            content: emailContent,
            recipients: recipients);

        // WaitUntil.Completed waits for the initial send operation to complete (not full delivery).
        EmailSendOperation operation = await _emailClient.SendAsync(WaitUntil.Completed, message);
        EmailSendResult result = operation.Value;
        if (result.Status != EmailSendStatus.Succeeded)
        {
            throw new InvalidOperationException($"Failed to send email: {result.Status}");
        }
    }
}