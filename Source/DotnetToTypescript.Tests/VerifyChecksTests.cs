namespace DotnetToTypescript.IntegrationTests;

[TestFixture]
public class VerifyChecksTests
{
    [Test]
    public Task Run() =>
        VerifyChecks.Run();
}