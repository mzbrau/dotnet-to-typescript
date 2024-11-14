namespace DotnetToTypescript;

public interface IDefinitionGenerator
{
    string GenerateDefinitions(List<Type> scriptClasses);
}