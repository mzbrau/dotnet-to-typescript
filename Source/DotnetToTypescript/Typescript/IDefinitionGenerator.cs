namespace DotnetToTypescript.Typescript;

public interface IDefinitionGenerator
{
    void Initialize(Type? typeAttribute);
    string GenerateDefinitions(IEnumerable<Type> scriptClasses, bool preserveCase = false);

    string GenerateInstances(
        Dictionary<(Type Type, string InstanceName), string> scriptCreateNames,
        Dictionary<(Type Type, string PropertyName), string> scriptPropertyNames,
        string definitionPath);
}