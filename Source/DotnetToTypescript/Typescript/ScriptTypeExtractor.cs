using System.Reflection;
using Microsoft.Extensions.Logging;

namespace DotnetToTypescript.Typescript;

public class ScriptTypeExtractor : IScriptTypeExtractor
{
    private Type? _typeAttribute;
    private Type? _objectAttribute;
    public Dictionary<Type, string> ScriptCreateNames { get; } = new();
    private readonly ILogger<ScriptTypeExtractor> _logger;

    public ScriptTypeExtractor(ILogger<ScriptTypeExtractor> logger)
    {
        _logger = logger;
    }

    public List<Type> ExtractScriptClasses(Assembly assembly)
    {
        InitializeAttributes(assembly);
        
        if (_typeAttribute is null)
            return new List<Type>();

        var scriptClasses = assembly.GetTypes()
            .Where(IsScriptClass)
            .ToList();

        ProcessObjectAttributes(scriptClasses);
        return scriptClasses;
    }

    private void InitializeAttributes(Assembly assembly)
    {
        _typeAttribute ??= assembly.GetTypes()
            .FirstOrDefault(t => t.Name == "JavascriptTypeAttribute");
            
        _objectAttribute ??= assembly.GetTypes()
            .FirstOrDefault(t => t.Name == "JavascriptObjectAttribute");
    }

    private bool IsScriptClass(Type type) =>
        type is { IsClass: true, IsPublic: true } && 
        _typeAttribute is not null &&
        type.GetCustomAttributes(_typeAttribute, false).Any();

    private void ProcessObjectAttributes(List<Type> scriptClasses)
    {
        if (_objectAttribute is null)
            return;

        foreach (var type in scriptClasses)
        {
            var objectAttribute = type.GetCustomAttributes(_objectAttribute, false).FirstOrDefault();
            if (objectAttribute != null)
            {
                var name = objectAttribute.GetType().GetProperty("Name")?.GetValue(objectAttribute) as string;
                if (!string.IsNullOrEmpty(name))
                {
                    if (name == type.Name)
                    {
                        _logger.LogWarning("JavascriptObject name '{Name}' matches class name for type {Type}. This is not supported. Appending '2' to the name.", name, type.Name);
                        name = $"{name}2";
                    }
                    ScriptCreateNames[type] = name;
                }
            }
        }
    }
}