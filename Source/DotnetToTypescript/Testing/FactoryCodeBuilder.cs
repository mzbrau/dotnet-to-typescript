using System.Text;
using DotnetToTypescript.Typescript;

namespace DotnetToTypescript.Testing;

public static class FactoryCodeBuilder
{
    public static string BuildFactoriesModule(IReadOnlyList<Type> scriptClasses, bool preserveCase)
    {
        var sb = new StringBuilder();
        sb.AppendLine("/**");
        sb.AppendLine(" * Factory helpers for building model objects in tests.");
        sb.AppendLine(" * Overrides are deep-merged onto generated defaults.");
        sb.AppendLine(" */");
        sb.AppendLine();
        sb.AppendLine("function deepMerge(defaults, overrides) {");
        sb.AppendLine("  if (overrides === undefined || overrides === null) return defaults;");
        sb.AppendLine("  if (Array.isArray(defaults) || Array.isArray(overrides)) return overrides;");
        sb.AppendLine("  if (typeof defaults !== \"object\" || defaults === null || typeof overrides !== \"object\") {");
        sb.AppendLine("    return overrides;");
        sb.AppendLine("  }");
        sb.AppendLine("  const result = { ...defaults };");
        sb.AppendLine("  for (const [key, value] of Object.entries(overrides)) {");
        sb.AppendLine("    result[key] = key in defaults ? deepMerge(defaults[key], value) : value;");
        sb.AppendLine("  }");
        sb.AppendLine("  return result;");
        sb.AppendLine("}");
        sb.AppendLine();

        var types = scriptClasses
            .Where(t => t.Namespace?.StartsWith("System", StringComparison.Ordinal) != true)
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

        foreach (var type in types)
        {
            var functionName = $"create{type.Name}";
            var defaults = DefaultValueFactory.GetRecursiveDefaultExpression(type, preserveCase);

            sb.AppendLine($"export function {functionName}(overrides = {{}}) {{");
            sb.AppendLine($"  return deepMerge({defaults}, overrides);");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
