using System.Reflection;
using Microsoft.Extensions.Logging;

namespace DotnetToTypescript.Typescript;

public class ScriptTypeExtractor : IScriptTypeExtractor
{
    private Type? _objectAttribute;
    public Dictionary<(Type Type, string InstanceName), string> ScriptCreateNames { get; } = new();
    public Dictionary<(Type Type, string PropertyName), string> ScriptPropertyNames { get; } = new();
    public Type? TypeAttribute { get; private set; }
    private readonly ILogger<ScriptTypeExtractor> _logger;

    public ScriptTypeExtractor(ILogger<ScriptTypeExtractor> logger)
    {
        _logger = logger;
    }

    public void InitializeAttributes(IEnumerable<Assembly> assemblies)
    {
        foreach (var assembly in assemblies)
        {
            TypeAttribute ??= assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "JavascriptTypeAttribute");
                
            _objectAttribute ??= assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "JavascriptObjectAttribute");

            if (TypeAttribute != null && _objectAttribute != null)
                break;
        }
    }

    public List<Type> ExtractScriptClasses(Assembly assembly)
    {
        if (TypeAttribute is null)
            return [];

        var scriptClasses = assembly.GetTypes()
            .Where(IsScriptClass)
            .ToList();

        ProcessObjectAttributes(scriptClasses);
        ProcessPropertyAttributes(scriptClasses);
        return scriptClasses.Where(c => !IsClassSkipped(c)).ToList();
    }

    private bool IsScriptClass(Type type) =>
        type is { IsClass: true } && 
        (type.IsNested ? type.IsNestedPublic : type.IsPublic) &&
        TypeAttribute is not null &&
        type.GetCustomAttributes(TypeAttribute, false).Any();

    private bool IsClassSkipped(Type type)
    {
        if (TypeAttribute is not null)
        {
            var att = type.GetCustomAttributes(TypeAttribute, false).FirstOrDefault();
            if (att is not null)
            {
                var skip = att.GetType().GetProperty("SkipDefinition")?.GetValue(att) as bool?;

                return skip == true;
            }
        }

        return false;
    }

    private void ProcessObjectAttributes(List<Type> scriptClasses)
    {
        if (_objectAttribute is null)
            return;

        foreach (var type in scriptClasses)
        {
            var objectAttributes = type.GetCustomAttributes(_objectAttribute, false);
            foreach (var objectAttribute in objectAttributes)
            {
                var name = objectAttribute.GetType().GetProperty("Name")?.GetValue(objectAttribute) as string;
                if (!string.IsNullOrEmpty(name))
                {
                    if (name == type.Name)
                    {
                        _logger.LogWarning("JavascriptObject name '{Name}' matches class name for type {Type}. This is not supported. Appending '2' to the name", name, type.Name);
                        name = $"{name}2";
                    }
                    ScriptCreateNames[(type, name)] = name;
                }
            }
        }
    }

    private void ProcessPropertyAttributes(List<Type> scriptClasses)
    {
        if (_objectAttribute is null)
            return;

        foreach (var type in scriptClasses)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                var objectAttribute = property.GetCustomAttributes(_objectAttribute, false).FirstOrDefault();
                if (objectAttribute != null)
                {
                    var name = objectAttribute.GetType().GetProperty("Name")?.GetValue(objectAttribute) as string;
                    if (!string.IsNullOrEmpty(name))
                    {
                        ScriptPropertyNames[(type, property.Name)] = name;
                    }
                }
            }
        }
    }
}