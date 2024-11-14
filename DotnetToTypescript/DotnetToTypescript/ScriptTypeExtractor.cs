using System.Reflection;

namespace DotnetToTypescript;

public class ScriptTypeExtractor : IScriptTypeExtractor
{
    public Dictionary<Type, string> ScriptCreateNames { get; } = new();
    
    public List<Type> ExtractScriptClasses(Assembly assembly)
    {
        var javascriptTypeAttribute = assembly.GetTypes().FirstOrDefault(t => t.Name == "JavascriptTypeAttribute");
        var javascriptObjectAttribute = assembly.GetTypes().FirstOrDefault(t => t.Name == "JavascriptObjectAttribute");

        if (javascriptTypeAttribute == null)
            throw new Exception("No attribute named 'Script' found.");
        
        if (javascriptObjectAttribute == null)
            throw new Exception("No attribute named 'ScriptCreate' found.");

        var scriptClasses = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsPublic: true } && t.GetCustomAttributes(javascriptTypeAttribute, false).Any())
            .ToList();

        foreach (var type in scriptClasses)
        {
            // Check for ScriptCreate attribute
            var createAttribute = type.GetCustomAttributes(javascriptObjectAttribute, false).FirstOrDefault();
            if (createAttribute != null)
            {
                var createName = createAttribute.GetType().GetProperty("Name")?.GetValue(createAttribute)?.ToString();
                if (!string.IsNullOrEmpty(createName))
                {
                    ScriptCreateNames[type] = createName;
                }
            }
        }

        return scriptClasses;
    }
}