using ProgramX.Azure.FunctionApp.Helpers;

namespace ProgramX.Azure.FunctionApp.Tests.Helpers;

[TestFixture]
public class SingletonMutexTests
{
    [Test]
    public void Ctor_HasSecondsTimeout()
    {
        var target = new SingletonMutex(10);
        Assert.That(target, Is.Not.Null);
        Assert.That(target.SecondsTimeout, Is.EqualTo(10));
    }
    
    [Test]
    public void Ctor_WithNoSecondsTimeout_HasDefaultTimeout()
    {
        var target = new SingletonMutex();
        Assert.That(target.SecondsTimeout, Is.EqualTo(SingletonMutex.DefaultSecondsTimeout));       
    }
    
    [Test]
    public void Ctor_WithNegativeSecondsTimeout_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SingletonMutex(-1));
    }
    
    [Test]
    public void Ctor_WithZeroSecondsTimeout_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SingletonMutex(0));
    }

    
    [Test]
    public void RegisterHealthCheckForType_WithNewType_ShouldAdd()
    {
        var target = new SingletonMutex();
        target.RegisterHealthCheckForType("test");
        Assert.That(target.GetMutexes().Count, Is.EqualTo(1));
    }
    
    [Test]
    public void RegisterHealthCheckForType_WitExistingType_ShouldUpdate()
    {
        var target = new SingletonMutex();
        target.RegisterHealthCheckForType("test");
        // call it again to update the existing item
        target.RegisterHealthCheckForType("test");
        Assert.That(target.GetMutexes().Count, Is.EqualTo(1));
    }
    
    

    [Test]
    public void IsRequestWithinSecondsOfMostRecentRequestOfSameType_Within2Seconds_ShouldReturnTrue()
    {
        var target = new SingletonMutex(2);
        target.RegisterHealthCheckForType("test");
        Assert.That(target.IsRequestWithinSecondsOfMostRecentRequestOfSameType("test"), Is.True);
    }

    
    [Test]
    public void IsRequestWithinSecondsOfMostRecentRequestOfSameType_Outside2Seconds_ShouldReturnFalse()
    {
        var target = new SingletonMutex(2);
        target.RegisterHealthCheckForType("test");
        Thread.Sleep(3000); // wait 3 seconds
        Assert.That(target.IsRequestWithinSecondsOfMostRecentRequestOfSameType("test"), Is.False);
    }
    
    
    [Test]
    public void IsRequestWithinSecondsOfMostRecentRequestOfSameType_WithPreviouslyUnknownType_ShouldReturnFalse()
    {
        var target = new SingletonMutex(2);
        Assert.That(target.IsRequestWithinSecondsOfMostRecentRequestOfSameType("test"), Is.False);
    }
    
    
    [Test]
    public void IsRequestWithinSecondsOfMostRecentRequestOfSameType_WithNullType_ShouldReturnFalse()
    {
        var target = new SingletonMutex(2);
        Assert.That(target.IsRequestWithinSecondsOfMostRecentRequestOfSameType(null), Is.False);
    }
    
    [Test]
    public void GetMutexes_ShouldReturnEmptyList()
    {
        var target = new SingletonMutex();
        Assert.That(target.GetMutexes(), Is.Empty);
    }
    
    [Test]
    public void GetMutexes_WithRegisteredItems_ShouldReturnItems()
    {
        var target = new SingletonMutex();
        target.RegisterHealthCheckForType("test1");
        target.RegisterHealthCheckForType("test2");
        Assert.That(target.GetMutexes().Count, Is.EqualTo(2));
    }
    
}