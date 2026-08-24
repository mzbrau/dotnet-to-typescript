namespace DotnetToTypescript.Testing;

public interface ITestEnvironmentGenerator
{
    Task GenerateAsync(
        string outputDirectory,
        string definitionFileName,
        IReadOnlyList<Type> scriptClasses,
        Dictionary<(Type Type, string InstanceName), string> scriptCreateNames,
        Dictionary<(Type Type, string PropertyName), string> scriptPropertyNames,
        bool preserveCase);
}
