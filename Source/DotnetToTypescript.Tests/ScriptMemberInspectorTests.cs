using DotnetToTypescript.Typescript;

namespace DotnetToTypescript.IntegrationTests;

[TestFixture]
public class ScriptMemberInspectorTests
{
    [Test]
    public void GetMethods_OrdersOverloadsDeterministicallyByParameterSignature()
    {
        var methods = ScriptMemberInspector.GetMethods(typeof(OverloadedSample));

        var overloads = methods
            .Where(m => m.Name == nameof(OverloadedSample.DoWork))
            .Select(m => m.GetParameters()[0].ParameterType)
            .ToList();

        Assert.That(overloads, Is.EqualTo(new[]
        {
            typeof(DateTime),
            typeof(int),
            typeof(string),
        }));
    }

    private sealed class OverloadedSample
    {
        public void DoWork(string value) { }

        public void DoWork(DateTime value) { }

        public void DoWork(int value) { }
    }
}
