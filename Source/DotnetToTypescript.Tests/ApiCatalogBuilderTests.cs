using DotnetToTypescript.Testing.Robustness;
using SampleLibrary;

namespace DotnetToTypescript.IntegrationTests;

[TestFixture]
public class ApiCatalogBuilderTests
{
    [Test]
    public void BuildCatalogModule_IncludesGlobalsMethodsAndFlags()
    {
        var globals = new List<(string GlobalName, Type Type)>
        {
            ("mike", typeof(User)),
            ("carApi", typeof(Car)),
        };

        var catalog = ApiCatalogBuilder.BuildCatalogModule(globals, preserveCase: false);

        Assert.That(catalog, Does.Contain("export const apiCatalog"));
        Assert.That(catalog, Does.Contain("name: \"carApi\""));
        Assert.That(catalog, Does.Contain("name: \"mike\""));
        Assert.That(catalog, Does.Contain("path: \"mike.hasPermission\""));
        Assert.That(catalog, Does.Contain("path: \"carApi.getMaintenanceHistory\""));
        Assert.That(catalog, Does.Contain("isBoolean: true"));
        Assert.That(catalog, Does.Contain("isCollection: true"));
        Assert.That(catalog, Does.Contain("isAsync: true"));
        Assert.That(catalog, Does.Contain("defaultReturnExpression:"));
        Assert.That(catalog, Does.Contain("returnGraph:"));
    }

    [Test]
    public void BuildCatalogModule_OrdersGlobalsOrdinal()
    {
        var globals = new List<(string GlobalName, Type Type)>
        {
            ("mike", typeof(User)),
            ("dave", typeof(User)),
            ("jim", typeof(User)),
        };

        var catalog = ApiCatalogBuilder.BuildCatalogModule(globals, preserveCase: false);
        var dave = catalog.IndexOf("name: \"dave\"", StringComparison.Ordinal);
        var jim = catalog.IndexOf("name: \"jim\"", StringComparison.Ordinal);
        var mike = catalog.IndexOf("name: \"mike\"", StringComparison.Ordinal);

        Assert.That(dave, Is.GreaterThan(-1));
        Assert.That(jim, Is.GreaterThan(dave));
        Assert.That(mike, Is.GreaterThan(jim));
    }

    [Test]
    public void BuildCatalogModule_MarksReferenceReturns()
    {
        var globals = new List<(string GlobalName, Type Type)>
        {
            ("carApi", typeof(Car)),
        };

        var catalog = ApiCatalogBuilder.BuildCatalogModule(globals, preserveCase: false);

        Assert.That(catalog, Does.Contain("path: \"carApi.getMaintenanceHistory\""));
        Assert.That(catalog, Does.Contain("isReferenceReturn: true"));
        Assert.That(catalog, Does.Contain("returnKind: \"collection\""));
    }
}
