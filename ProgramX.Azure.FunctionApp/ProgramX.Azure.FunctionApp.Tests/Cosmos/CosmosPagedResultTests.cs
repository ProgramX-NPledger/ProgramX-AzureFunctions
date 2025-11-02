using ProgramX.Azure.FunctionApp.Cosmos;
using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp.Tests.Cosmos;

[Category("Unit")]
[Category("Cosmos")]
[Category("CosmosPagedReader")]
[TestFixture]
public class CosmosPagedResultTests
{
    [Test]
    public void IsFirstPage_WithNoContinuationTokenAndTotalItemsEqualToTotalCount_ShouldReturnTrue()
    {
        var target = new CosmosPagedResult<User>(new List<User>(), null,5,5,0,0);
        
        Assert.That(target.IsFirstPage, Is.True);
    }

    [Test]
    public void IsFirstPage_WithNoContinuationTokenAndTotalItemsLessThanToTotalCount_ShouldReturnTrue()
    {
        var target = new CosmosPagedResult<User>(new List<User>(), null,15,5,0,0);
        
        Assert.That(target.IsFirstPage, Is.True);
    }
    
    [Test]
    public void IsFirstPage_WithContinuationToken_ShouldReturnFalse()
    {
        var target = new CosmosPagedResult<User>(new List<User>(), "continuation-token",5,5,0,0);
        
        Assert.That(target.IsFirstPage, Is.False);
    }

    [Test]
    public void NumberOfPages_WithFiftyItemsTenPerPage_ShouldReturnFive()
    {
        var target = new CosmosPagedResult<User>(new List<User>(), null,10,50,0,0);
        
        Assert.That(target.NumberOfPages, Is.EqualTo(5));
    }

    
    [Test]
    public void NumberOfPages_WithTwoItemsTenPerPage_ShouldReturnOne()
    {
        var target = new CosmosPagedResult<User>(new List<User>(), null,10,2,0,0);
        
        Assert.That(target.NumberOfPages, Is.EqualTo(1));
    }

}