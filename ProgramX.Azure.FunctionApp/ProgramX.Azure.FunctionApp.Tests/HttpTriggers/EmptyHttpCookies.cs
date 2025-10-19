using Microsoft.Azure.Functions.Worker.Http;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers;

public class EmptyHttpCookies : HttpCookies
{
    public override void Append(string name, string value)
    {
        
    }

    public override void Append(IHttpCookie cookie)
    {
        
    }

    public override IHttpCookie CreateNew()
    {
        return new HttpCookie("","");
    }
}