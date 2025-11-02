// using FluentAssertions;
// using Microsoft.Azure.Functions.Worker.Http;
// using Microsoft.Extensions.Logging;
// using Moq;
// using NUnit.Framework;
// using ProgramX.Azure.FunctionApp.HttpTriggers;
// using ProgramX.Azure.FunctionApp.Model;
// using ProgramX.Azure.FunctionApp.Tests.TestData;
// using System.Collections.Specialized;
// using System.Net;
// using System.Text;
// using System.Text.Json;
// using ProgramX.Azure.FunctionApp.Tests.Mocks;
//
// namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers;
//
// [TestFixture]
// [Category("Integration")]
// public class UsersHttpTriggerGetUserIntegrationTests : TestBase
// {
//     private Mock<HttpRequestData> _mockHttpRequestData = null!;
//     private TestHttpRequestData _testHttpRequestData = null!;
//     private NameValueCollection _mockQuery = null!;
//
//     [SetUp]
//     public override void SetUp()
//     {
//         base.SetUp();
//         
//         _mockHttpRequestData = new Mock<HttpRequestData>();
//         _mockQuery = new NameValueCollection();
//         
//         _testHttpRequestData = new TestHttpRequestData();
//         _testHttpRequestData.SetQuery(_mockQuery);
//         _testHttpRequestData.SetUrl(new Uri("https://localhost:7071/api/user"));
//
//         _mockHttpRequestData.Setup(x => x.Query).Returns(_mockQuery);
//         _mockHttpRequestData.Setup(x => x.Url).Returns(new Uri("https://localhost:7071/api/user"));
//     }
//
//     [Test]
//     [Ignore("Requires authentication system to be mocked")]
//     public async Task GetUser_WithNullId_ShouldReturnPagedUsers()
//     {
//         // This test would require mocking the entire authentication system
//         // and HTTP response creation, which is complex but possible
//         
//         // Arrange
//         var usersHttpTrigger = new UsersHttpTriggerBuilder()
//             .WithDefaultMocks()
//             .WithConfiguration(Configuration)
//             .Build();
//
//         // Act & Assert
//         // Implementation would require extensive mocking of:
//         // - Authentication system
//         // - HttpResponseDataFactory
//         // - HTTP context and headers
//         
//         Assert.Pass("Integration test placeholder - requires full HTTP context mocking");
//     }
//
//     [Test]
//     public void GetUser_QueryParameterParsing_ShouldHandleUrlEncodedValues()
//     {
//         // Arrange
//         var encodedText = Uri.EscapeDataString("test@domain.com");
//         var encodedRoles = Uri.EscapeDataString("Admin,PowerUser");
//         
//         _mockQuery["containsText"] = encodedText;
//         _mockQuery["withRoles"] = encodedRoles;
//         _mockQuery["offset"] = "10";
//         _mockQuery["itemsPerPage"] = "25";
//
//         // Act
//         var containsText = _mockQuery["containsText"] == null 
//             ? null 
//             : Uri.UnescapeDataString(_mockQuery["containsText"]);
//         
//         var withRoles = _mockQuery["withRoles"] == null 
//             ? null 
//             : Uri.UnescapeDataString(_mockQuery["withRoles"]).Split(',');
//
//         // Assert
//         containsText.Should().Be("test@domain.com");
//         withRoles.Should().BeEquivalentTo(new[] { "Admin", "PowerUser" });
//     }
//
//     [Test]
//     public void GetUser_UrlBuilding_ShouldCreateValidUrls()
//     {
//         // Arrange
//         var baseUrl = "https://api.example.com/users";
//         var scheme = "https";
//         var authority = "api.example.com";
//         var path = "/users";
//
//         _testHttpRequestData.SetUrl(new Uri($"{scheme}://{authority}{path}"));
//
//         // Act
//         var constructedUrl = $"{_testHttpRequestData.Url.Scheme}://{_testHttpRequestData.Url.Authority}{_testHttpRequestData.Url.AbsolutePath}";
//
//         // Assert
//         constructedUrl.Should().Be($"{scheme}://{authority}{path}");
//
//     }
// }
