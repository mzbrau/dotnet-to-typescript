using System.Text;

namespace DotnetToTypescript.Typescript;

public static class DefaultValueFactory
{
    /// <summary>
    /// Simple defaults used by instance stub files (preserves historical behavior).
    /// </summary>
    public static string GetSimpleDefaultExpression(Type type)
    {
        if (type == typeof(string)) return "\"\"";
        if (type == typeof(bool)) return "false";
        if (type == typeof(DateTime)) return "new Date()";
        if (type.IsValueType) return "0";
        return "null";
    }

    /// <summary>
    /// Recursive JavaScript object literal defaults for mocks and factories.
    /// </summary>
    public static string GetRecursiveDefaultExpression(Type type, bool preserveCase, HashSet<Type>? visited = null)
    {
        visited ??= [];

        if (Nullable.GetUnderlyingType(type) is { } underlying)
            return "null";

        if (type == typeof(string)) return "\"\"";
        if (type == typeof(bool)) return "false";
        if (type == typeof(DateTime)) return "new Date(0)";
        if (type == typeof(void)) return "undefined";

        if (type.IsEnum) return "0";

        if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) ||
            type == typeof(float) || type == typeof(double) || type == typeof(decimal) ||
            type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) || type == typeof(sbyte))
        {
            return "0";
        }

        if (type.IsArray) return "[]";

        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            if (def == typeof(List<>) || def == typeof(IList<>) || def == typeof(IEnumerable<>))
                return "[]";

            if (def == typeof(Dictionary<,>) || def == typeof(IDictionary<,>))
                return "{}";

            if (def == typeof(Task<>) || type == typeof(Task))
                return "undefined";
        }

        if (type == typeof(Task))
            return "undefined";

        if (!ScriptMemberInspector.IsComplexObjectType(type))
            return "null";

        if (!visited.Add(type))
            return "null";

        try
        {
            var sb = new StringBuilder();
            sb.Append("{ ");

            var properties = ScriptMemberInspector.GetProperties(type);
            for (var i = 0; i < properties.Count; i++)
            {
                var prop = properties[i];
                var name = ScriptMemberInspector.FormatName(prop.Name, preserveCase);
                var value = GetRecursiveDefaultExpression(prop.PropertyType, preserveCase, visited);
                sb.Append($"{name}: {value}");
                if (i < properties.Count - 1)
                    sb.Append(", ");
            }

            sb.Append(" }");
            return sb.ToString();
        }
        finally
        {
            visited.Remove(type);
        }
    }

    /// <summary>
    /// Defaults used when configuring mock return values for robustness baseline scenarios.
    /// Async methods are wrapped in Promise.resolve(...).
    /// </summary>
    public static string GetBaselineReturnExpression(Type type, bool preserveCase)
    {
        if (type == typeof(void))
            return "undefined";

        if (type == typeof(Task))
            return "Promise.resolve()";

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var inner = type.GetGenericArguments()[0];
            var innerDefault = GetRecursiveDefaultExpression(inner, preserveCase);
            return $"Promise.resolve({innerDefault})";
        }

        return GetRecursiveDefaultExpression(type, preserveCase);
    }
}
