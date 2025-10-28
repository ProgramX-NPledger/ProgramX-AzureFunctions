using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp.Contract;

public interface IEmailSender
{
    Task SendEmailAsync(EmailMessage message);
}