using System.Reflection;
using System.Text;

namespace DotnetToTypescript;

public class TypeScriptDefinitionGenerator : IDefinitionGenerator
{
    private readonly TypeScriptTypeMapper _typeMapper = new();

    public string GenerateDefinitions(List<Type> scriptClasses)
    {
        var sb = new StringBuilder();
        var processedTypes = new HashSet<Type>();

        foreach (var type in scriptClasses) ProcessClass(type, sb, processedTypes);

        return sb.ToString();
    }

    private void ProcessClass(Type type, StringBuilder sb, HashSet<Type> processedTypes)
    {
        if (processedTypes.Contains(type)) return;
        processedTypes.Add(type);

        sb.AppendLine($"declare class {type.Name} {{");

        // Extract public properties
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
            sb.AppendLine($"    {prop.Name}: {_typeMapper.MapToTypeScriptType(prop.PropertyType)};");

        // Extract public methods - updated to exclude built-in Object methods
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName); // Exclude property getters/setters
        foreach (var method in methods)
        {
            var parameters = method.GetParameters()
                .Select(p => $"{p.Name}: {_typeMapper.MapToTypeScriptType(p.ParameterType)}")
                .ToArray();

            sb.AppendLine(
                $"    {method.Name}({string.Join(", ", parameters)}): {_typeMapper.MapToTypeScriptType(method.ReturnType)};");
        }

        sb.AppendLine("}");
        sb.AppendLine();

        // Recursively process referenced types
        var referencedTypes = properties.Select(p => p.PropertyType)
            .Concat(methods.SelectMany(m => m.GetParameters().Select(p => p.ParameterType)))
            .Concat(methods.Select(m => m.ReturnType))
            .Where(t => t.IsClass && t.Assembly == type.Assembly && !processedTypes.Contains(t))
            .Distinct();

        foreach (var refType in referencedTypes) ProcessClass(refType, sb, processedTypes);
    }
}