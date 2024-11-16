namespace DotnetToTypescript.Typescript;

public interface IDefinitionGenerator
{
    string GenerateDefinitions(IEnumerable<Type> scriptClasses);
    
    string GenerateInstances(Dictionary<Type, string> scriptCreateNames, string definitionPath);
}