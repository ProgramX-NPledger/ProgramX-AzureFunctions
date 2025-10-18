using Microsoft.Azure.Functions.Worker;

namespace ProgramX.Azure.FunctionApp.Tests.HttpTriggers;


// Minimal test implementation of FunctionContext
public class TestFunctionContext : FunctionContext
{
    public override string InvocationId { get; }
    public override string FunctionId { get; }
    public override TraceContext TraceContext { get; }
    public override BindingContext BindingContext { get; }
    public override RetryContext RetryContext { get; }
    public override IServiceProvider InstanceServices { get; set; }
    public override FunctionDefinition FunctionDefinition { get; }
    public override IDictionary<object, object> Items { get; set; }
    public override IInvocationFeatures Features { get; }

    public TestFunctionContext()
    {
        InvocationId = Guid.NewGuid().ToString();
        FunctionId = "test-function";
        // TraceContext = new DefaultTraceContext(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        // BindingContext = new DefaultBindingContext();
        // RetryContext = new DefaultRetryContext();
        Items = new Dictionary<object, object>();
        //Features = new InvocationFeatures(new Dictionary<Type, object>());
        InstanceServices = new TestServiceProvider();
    }
}
