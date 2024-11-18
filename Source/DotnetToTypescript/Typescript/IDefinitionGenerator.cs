namespace DotnetToTypescript.Typescript;

public interface IDefinitionGenerator
{
    string GenerateDefinitions(IEnumerable<Type> scriptClasses);
    
    string GenerateInstances(Dictionary<Type, string> scriptCreateNames, 
        Dictionary<(Type Type, string PropertyName), string> scriptPropertyNames,
        string definitionPath);
}