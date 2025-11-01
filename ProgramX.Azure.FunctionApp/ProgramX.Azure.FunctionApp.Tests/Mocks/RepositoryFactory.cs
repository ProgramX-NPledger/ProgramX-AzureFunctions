using Microsoft.Azure.Cosmos;
using Moq;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model.Criteria;

namespace ProgramX.Azure.FunctionApp.Tests.Mocks;

public class RepositoryFactory
{
    
    public Mock<IUserRepository> CreateUserRepository()
    {
        var mockedUserRepository = new Mock<IUserRepository>();
        return mockedUserRepository;
    }
}