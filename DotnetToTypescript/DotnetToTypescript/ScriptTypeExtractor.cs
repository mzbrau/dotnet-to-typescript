using System.Reflection;

namespace DotnetToTypescript;

public class ScriptTypeExtractor : IScriptTypeExtractor
{
    public List<Type> ExtractScriptClasses(Assembly assembly)
    {
        var scriptAttribute = assembly.GetTypes().FirstOrDefault(t => t.Name == "ScriptAttribute");
        if (scriptAttribute == null)
            throw new Exception("No attribute named 'Script' found.");

        return assembly.GetTypes()
            .Where(t => t.IsClass && t.IsPublic && t.GetCustomAttributes(scriptAttribute, false).Any())
            .ToList();
    }
}