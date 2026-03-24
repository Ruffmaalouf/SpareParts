using SpareParts.Infrastructure.Services;

namespace SpareParts.ArchitectureTests;

public class ArchitectureTests
{
    [Fact]
    public void Domain_ShouldNotReferenceInfrastructure()
    {
        var domainAssembly = typeof(SpareParts.Domain.Common.Entity).Assembly;
        var infraName = typeof(CreateSaleHandler).Assembly.GetName().Name!;

        Assert.DoesNotContain(domainAssembly.GetReferencedAssemblies(), x => x.Name == infraName);
    }

    [Fact]
    public void Handlers_ShouldStayInInfrastructure()
    {
        var handlerNamespace = typeof(CreateSaleHandler).Namespace;
        Assert.StartsWith("SpareParts.Infrastructure", handlerNamespace);
    }
}
