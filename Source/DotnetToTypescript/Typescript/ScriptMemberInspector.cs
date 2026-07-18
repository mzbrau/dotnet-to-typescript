using System.Reflection;

namespace DotnetToTypescript.Typescript;

public static class ScriptMemberInspector
{
    public static IReadOnlyList<PropertyInfo> GetProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyList<MethodInfo> GetMethods(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .OrderBy(m => m.Name, StringComparer.Ordinal)
            .ThenBy(m => m.GetParameters().Length)
            .ToList();

    public static string FormatName(string name, bool preserveCase)
    {
        if (preserveCase || string.IsNullOrEmpty(name))
            return name;

        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    public static bool IsComplexObjectType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(string) ||
            underlying == typeof(DateTime) ||
            underlying.IsPrimitive ||
            underlying.IsEnum ||
            underlying == typeof(decimal) ||
            underlying == typeof(void))
        {
            return false;
        }

        if (underlying.IsArray)
            return false;

        if (underlying.IsGenericType)
        {
            var def = underlying.GetGenericTypeDefinition();
            if (def == typeof(List<>) ||
                def == typeof(IList<>) ||
                def == typeof(IEnumerable<>) ||
                def == typeof(Dictionary<,>) ||
                def == typeof(IDictionary<,>) ||
                def == typeof(Task<>) ||
                def.FullName?.StartsWith("System.Func`", StringComparison.Ordinal) == true)
            {
                return false;
            }
        }

        if (underlying == typeof(Task))
            return false;

        return underlying.IsClass || (underlying.IsValueType && !underlying.IsPrimitive && !underlying.IsEnum);
    }
}
