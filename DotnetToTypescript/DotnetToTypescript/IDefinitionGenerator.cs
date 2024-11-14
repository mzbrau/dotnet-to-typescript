namespace DotnetToTypescript;

public interface IDefinitionGenerator
{
    string GenerateDefinitions(List<Type> scriptClasses);
    
    string GenerateInstances(Dictionary<Type, string> scriptCreateNames, string definitionPath);
}