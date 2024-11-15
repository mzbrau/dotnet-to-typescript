using System.Reflection;
using System.Text;
using System.IO;
using System.Linq;

namespace DotnetToTypescript;

public class TypeScriptDefinitionGenerator : IDefinitionGenerator
{
    private readonly TypeScriptTypeMapper _typeMapper = new TypeScriptTypeMapper();

    public string GenerateDefinitions(List<Type> scriptClasses)
    {
        var sb = new StringBuilder();
        var processedTypes = new HashSet<Type>();

        foreach (var type in scriptClasses)
        {
            ProcessClass(type, sb, processedTypes);
        }

        return sb.ToString();
    }

    public string GenerateInstances(Dictionary<Type, string> scriptCreateNames, string definitionPath)
    {
        if (!scriptCreateNames.Any()) return string.Empty;
        
        var sb = new StringBuilder();
        
        // Add reference to definition file
        var definitionFile = Path.GetFileName(definitionPath);
        sb.AppendLine($"/// <reference path=\"{Path.GetFileName(definitionFile)}\" />");
        sb.AppendLine();

        foreach (var entry in scriptCreateNames)
        {
            var typeName = entry.Key.Name;
            var instanceName = entry.Value;
            sb.AppendLine($"let {instanceName} = new {typeName}();");
        }
        
        sb.AppendLine();
        sb.AppendLine("// Insert your script below");
        sb.AppendLine("// -------------------------");
        sb.AppendLine();

        return sb.ToString();
    }

    private void ProcessClass(Type type, StringBuilder sb, HashSet<Type> processedTypes)
    {
        if (processedTypes.Contains(type)) return;
        processedTypes.Add(type);

        sb.AppendLine($"declare class {type.Name} {{");

        var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | BindingFlags.DeclaredOnly);
        foreach (var prop in properties)
        {
            sb.AppendLine($"    {prop.Name}: {_typeMapper.MapToTypeScriptType(prop.PropertyType)};");
        }

        var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName);
        foreach (var method in methods)
        {
            var parameters = method.GetParameters()
                .Select(p => $"{p.Name}: {_typeMapper.MapToTypeScriptType(p.ParameterType)}")
                .ToArray();

            sb.AppendLine($"    {method.Name}({string.Join(", ", parameters)}): {_typeMapper.MapToTypeScriptType(method.ReturnType)};");
        }

        sb.AppendLine("}");
    }
}