using System.Reflection;
using System.Text;
using DotnetToTypescript.Typescript;

namespace DotnetToTypescript.Testing;

public static class MockCodeBuilder
{
    public static string BuildMockModule(string globalName, Type type, bool preserveCase)
    {
        var sb = new StringBuilder();
        sb.AppendLine("import { vi } from \"vitest\";");
        sb.AppendLine();
        sb.AppendLine("/**");
        sb.AppendLine($" * Mock builder for global `{globalName}` ({type.Name}).");
        sb.AppendLine(" * Methods are strict Vitest mocks — configure with mockReturnValue / mockImplementation.");
        sb.AppendLine(" */");
        sb.AppendLine($"export function create{ToPascalCase(globalName)}Mock() {{");
        sb.AppendLine("  return " + BuildObjectLiteral(type, globalName, preserveCase, [], 1) + ";");
        sb.AppendLine("}");
        sb.AppendLine();
        return sb.ToString();
    }

    public static string BuildMocksIndex(
        IReadOnlyList<(string GlobalName, Type Type)> globals,
        IReadOnlyList<(string GlobalName, string DefaultExpression)> propertyGlobals)
    {
        var sb = new StringBuilder();
        sb.AppendLine("/**");
        sb.AppendLine(" * Installs all generated mocks onto a target object (globalThis or a vm context).");
        sb.AppendLine(" */");

        foreach (var (globalName, _) in globals.OrderBy(g => g.GlobalName, StringComparer.Ordinal))
        {
            sb.AppendLine($"import {{ create{ToPascalCase(globalName)}Mock }} from \"./{globalName}.js\";");
        }

        sb.AppendLine();
        sb.AppendLine("export function createAllMocks() {");
        sb.AppendLine("  return {");

        foreach (var (globalName, _) in globals.OrderBy(g => g.GlobalName, StringComparer.Ordinal))
        {
            sb.AppendLine($"    {globalName}: create{ToPascalCase(globalName)}Mock(),");
        }

        foreach (var (globalName, defaultExpression) in propertyGlobals.OrderBy(g => g.GlobalName, StringComparer.Ordinal))
        {
            sb.AppendLine($"    {globalName}: {defaultExpression},");
        }

        sb.AppendLine("  };");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("export function installMocks(target) {");
        sb.AppendLine("  const mocks = createAllMocks();");
        sb.AppendLine("  Object.assign(target, mocks);");
        sb.AppendLine("  return mocks;");
        sb.AppendLine("}");
        sb.AppendLine();
        return sb.ToString();
    }

    private static string BuildObjectLiteral(
        Type type,
        string path,
        bool preserveCase,
        HashSet<Type> visited,
        int indentLevel)
    {
        if (!visited.Add(type))
            return "null";

        var indent = new string(' ', indentLevel * 2);
        var innerIndent = new string(' ', (indentLevel + 1) * 2);
        var sb = new StringBuilder();
        sb.AppendLine("{");

        var properties = ScriptMemberInspector.GetProperties(type);
        var methods = ScriptMemberInspector.GetMethods(type);

        foreach (var prop in properties)
        {
            var name = ScriptMemberInspector.FormatName(prop.Name, preserveCase);
            var propPath = $"{path}.{name}";
            var value = BuildPropertyValue(prop, propPath, preserveCase, visited, indentLevel + 1);
            sb.AppendLine($"{innerIndent}{name}: {value},");
        }

        foreach (var method in methods)
        {
            var name = ScriptMemberInspector.FormatName(method.Name, preserveCase);
            var methodPath = $"{path}.{name}";
            sb.AppendLine($"{innerIndent}{name}: {BuildStrictMock(methodPath)},");
        }

        sb.Append($"{indent}}}");
        visited.Remove(type);
        return sb.ToString();
    }

    private static string BuildPropertyValue(
        PropertyInfo prop,
        string path,
        bool preserveCase,
        HashSet<Type> visited,
        int indentLevel)
    {
        var propType = prop.PropertyType;
        var underlying = Nullable.GetUnderlyingType(propType) ?? propType;

        if (Nullable.GetUnderlyingType(propType) != null)
            return "null";

        if (ScriptMemberInspector.IsComplexObjectType(underlying) && HasMethods(underlying))
            return BuildObjectLiteral(underlying, path, preserveCase, visited, indentLevel);

        return DefaultValueFactory.GetRecursiveDefaultExpression(propType, preserveCase, visited);
    }

    private static bool HasMethods(Type type) =>
        ScriptMemberInspector.GetMethods(type).Count > 0;

    private static string BuildStrictMock(string path)
    {
        // Keep as a single expression so object literals stay valid.
        return $"vi.fn(function (...args) {{ const formattedArgs = args.map((a) => {{ try {{ return JSON.stringify(a); }} catch {{ return String(a); }} }}).join(\", \"); throw new Error(`{path}(${{formattedArgs}}) was called during script execution.\\n\\nNo mock behaviour has been configured.\\n\\nConfigure it using:\\n{path}.mockReturnValue(...)`); }})";
    }

    public static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        return char.ToUpperInvariant(name[0]) + name[1..];
    }

    public static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }
}
