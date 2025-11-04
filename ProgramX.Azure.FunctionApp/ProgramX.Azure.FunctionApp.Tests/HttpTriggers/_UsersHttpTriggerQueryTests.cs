// using FluentAssertions;
// using Microsoft.Azure.Cosmos;
// using NUnit.Framework;
// using ProgramX.Azure.FunctionApp.HttpTriggers;
// using ProgramX.Azure.FunctionApp.Tests.TestData;
// using System.Reflection;
// using ProgramX.Azure.FunctionApp.Tests.Mocks;
//
// namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers;
//
// [TestFixture]
// public class UsersHttpTriggerQueryTests : TestBase
// {
//     private UsersHttpTrigger _usersHttpTrigger = null!;
//
//     [SetUp]
//     public override void SetUp()
//     {
//         base.SetUp();
//         _usersHttpTrigger = new UsersHttpTriggerBuilder()
//             .WithConfiguration(Configuration)
//             .Build();
//     }
//
//     [Test]
//     public void BuildQueryDefinition_WithIdOnly_ShouldCreateCorrectQuery()
//     {
//         // Arrange
//         var id = "user123";
//
//         // Act
//         var queryDefinition = InvokeBuildQueryDefinition(id, null, null, null);
//
//         // Assert
//         queryDefinition.Should().NotBeNull();
//         queryDefinition.QueryText.Should().Contain("c.id=@id OR c.userName=@id");
//     }
//
//     [Test]
//     public void BuildQueryDefinition_WithContainsText_ShouldIncludeTextFilter()
//     {
//         // Arrange
//         var containsText = "john";
//
//         // Act
//         var queryDefinition = InvokeBuildQueryDefinition(null, containsText, null, null);
//
//         // Assert
//         queryDefinition.Should().NotBeNull();
//         queryDefinition.QueryText.Should().Contain("CONTAINS(UPPER(c.userName), @containsText)");
//         queryDefinition.QueryText.Should().Contain("CONTAINS(UPPER(c.emailAddress), @containsText)");
//         queryDefinition.QueryText.Should().Contain("CONTAINS(UPPER(c.firstName), @containsText)");
//         queryDefinition.QueryText.Should().Contain("CONTAINS(UPPER(c.lastName), @containsText)");
//     }
//
//     [Test]
//     public void BuildQueryDefinition_WithRoles_ShouldIncludeRoleFilter()
//     {
//         // Arrange
//         var withRoles = new[] { "Admin", "PowerUser" };
//
//         // Act
//         var queryDefinition = InvokeBuildQueryDefinition(null, null, withRoles, null);
//
//         // Assert
//         queryDefinition.Should().NotBeNull();
//         queryDefinition.QueryText.Should().Contain("EXISTS(SELECT VALUE r FROM r IN c.roles WHERE r.name = @role0)");
//         queryDefinition.QueryText.Should().Contain("EXISTS(SELECT VALUE r FROM r IN c.roles WHERE r.name = @role1)");
//     }
//
//     [Test]
//     public void BuildQueryDefinition_WithApplications_ShouldIncludeApplicationFilter()
//     {
//         // Arrange
//         var hasAccessToApplications = new[] { "Dashboard", "Reports" };
//
//         // Act
//         var queryDefinition = InvokeBuildQueryDefinition(null, null, null, hasAccessToApplications);
//
//         // Assert
//         queryDefinition.Should().NotBeNull();
//         queryDefinition.QueryText.Should().Contain("EXISTS(SELECT VALUE r FROM r IN c.roles JOIN a IN r.applications WHERE a.name = @appname0)");
//         queryDefinition.QueryText.Should().Contain("EXISTS(SELECT VALUE r FROM r IN c.roles JOIN a IN r.applications WHERE a.name = @appname1)");
//     }
//
//     [Test]
//     public void BuildQueryDefinition_WithAllFilters_ShouldIncludeAllConditions()
//     {
//         // Arrange
//         var containsText = "john";
//         var withRoles = new[] { "Admin" };
//         var hasAccessToApplications = new[] { "Dashboard" };
//
//         // Act
//         var queryDefinition = InvokeBuildQueryDefinition(null, containsText, withRoles, hasAccessToApplications);
//
//         // Assert
//         queryDefinition.Should().NotBeNull();
//         queryDefinition.QueryText.Should().Contain("CONTAINS(UPPER(c.userName), @containsText)");
//         queryDefinition.QueryText.Should().Contain("EXISTS(SELECT VALUE r FROM r IN c.roles WHERE r.name = @role0)");
//         queryDefinition.QueryText.Should().Contain(@"EXISTS(SELECT VALUE r FROM r IN c.roles JOIN a IN r.applications WHERE a.name = @appname0)");
//     }
//
//     [TestCase("", "", new string[0], new string[0])]
//     public void BuildQueryDefinition_WithEmptyFilters_ShouldCreateBasicQuery(
//         string id, string containsText, string[] withRoles, string[] hasAccessToApplications)
//     {
//         // Act
//         var queryDefinition = InvokeBuildQueryDefinition(id, containsText, withRoles, hasAccessToApplications);
//
//         // Assert
//         queryDefinition.Should().NotBeNull();
//         if (string.IsNullOrWhiteSpace(id))
//         {
//             
//         }
//     }
//
//     [Test]
//     public void BuildQueryDefinition_WithSpecialCharactersInText_ShouldHandleCorrectly()
//     {
//         // Arrange
//         var containsText = "john@example.com";
//
//         // Act
//         var queryDefinition = InvokeBuildQueryDefinition(null, containsText, null, null);
//
//         // Assert
//         queryDefinition.Should().NotBeNull();
//         queryDefinition.QueryText.Should().Contain("@containsText");
//     }
//
//     private QueryDefinition InvokeBuildQueryDefinition(string? id, string? containsText, 
//         IEnumerable<string>? withRoles, IEnumerable<string>? hasAccessToApplications)
//     {
//         var method = typeof(UsersHttpTrigger).GetMethod("BuildQueryDefinition", 
//             BindingFlags.NonPublic | BindingFlags.Instance);
//         
//         return (QueryDefinition)method!.Invoke(_usersHttpTrigger, 
//             new object?[] { id, containsText, withRoles, hasAccessToApplications })!;
//     }
// }
