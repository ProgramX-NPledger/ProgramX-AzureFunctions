using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using ProgramX.Azure.FunctionApp.Helpers;
using ProgramX.Azure.FunctionApp.Model.Requests;

namespace ProgramX.Azure.FunctionApp.Tests.Helpers;

[TestFixture]
public class JwtTokenIssuerTests
{
    [Test]
    public void CtorTest()
    {
        var mockConfiguration = new Mock<IConfiguration>();
        
        var target = new JwtTokenIssuer(mockConfiguration.Object);

        Assert.That(target, Is.Not.Null);
        Assert.That(target, Is.InstanceOf<JwtTokenIssuer>());
        Assert.That(target.JwtEncoder, Is.Not.Null);
    }
    
    [Test]
    public void IssueTokenForUserTest()
    {
        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(q => q["JwtKey"]).Returns(
            Enumerable.Range(65, 26)
                .Union(Enumerable.Range(97, 26))
                    .Select(q => (char)q).Aggregate(string.Empty, (current, c) => current + c));
        
        var target = new JwtTokenIssuer(mockConfiguration.Object);
        var result = target.IssueTokenForUser(new Credentials(),new string[] { "role1", "role2" });
        Assert.That(result, Is.Not.Null);
        Assert.That(result,
            Is.EqualTo(
                "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJ1c2VybmFtZSI6bnVsbCwicm9sZXMiOlsicm9sZTEiLCJyb2xlMiJdfQ.pDeVYv5X4HSIMsL64g7JgVBsPZFuJCePNPamboadPsQ"));
    }
}
