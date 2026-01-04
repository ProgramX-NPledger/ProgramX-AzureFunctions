namespace ProgramX.Azure.FunctionApp.Cosmos;

public static class ContainerNames
{
    public const string Users = "users";
    public const string UserNamePartitionKey = "/userName";
    public const string UserPasswords = "userPasswords";
    public const string UserPasswordsPartitionKey = "/userName";
        
    public const string HealthChecks = "healthChecks";
    public const string HealthCheckNamePartitionKey = "/id";
}
 