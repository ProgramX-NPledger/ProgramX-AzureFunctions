using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp.Tests.TestData;

public class ApplicationBuilder
{
    private Application _application;

    public ApplicationBuilder()
    {
        _application = new Application
        {
            name = "DefaultApp",
            metaDataDotNetAssembly = string.Empty,
            metaDataDotNetType = string.Empty
        };
    }

    public ApplicationBuilder WithName(string name)
    {
        _application.name = name;
        return this;
    }
    

    public ApplicationBuilder IsDefaultApplication(bool isDefault = true)
    {
        _application.isDefaultApplicationOnLogin = isDefault;
        return this;
    }

    public ApplicationBuilder WithOrdinal(int ordinal)
    {
        _application.ordinal = ordinal;
        return this;
    }

    public ApplicationBuilder CreatedAt(DateTime createdAt)
    {
        _application.createdAt = createdAt;
        return this;
    }

    public ApplicationBuilder UpdatedAt(DateTime updatedAt)
    {
        _application.updatedAt = updatedAt;
        return this;
    }

    public Application Build() => _application;

    public static implicit operator Application(ApplicationBuilder builder) => builder.Build();
}
