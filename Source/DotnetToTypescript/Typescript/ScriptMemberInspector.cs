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

    public static Type UnwrapTask(Type type)
    {
        if (type == typeof(Task))
            return typeof(void);

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            return type.GetGenericArguments()[0];

        return type;
    }

    public static bool IsAsyncType(Type type) =>
        type == typeof(Task) ||
        (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>));

    public static bool IsVoidType(Type type)
    {
        var unwrapped = UnwrapTask(Nullable.GetUnderlyingType(type) ?? type);
        return unwrapped == typeof(void);
    }

    public static bool IsBooleanType(Type type)
    {
        var unwrapped = UnwrapTask(Nullable.GetUnderlyingType(type) ?? type);
        return unwrapped == typeof(bool);
    }

    public static bool IsStringType(Type type)
    {
        var unwrapped = UnwrapTask(Nullable.GetUnderlyingType(type) ?? type);
        return unwrapped == typeof(string);
    }

    public static bool IsNumericType(Type type)
    {
        var unwrapped = UnwrapTask(Nullable.GetUnderlyingType(type) ?? type);
        return unwrapped == typeof(int) || unwrapped == typeof(long) || unwrapped == typeof(short) ||
               unwrapped == typeof(byte) || unwrapped == typeof(float) || unwrapped == typeof(double) ||
               unwrapped == typeof(decimal) || unwrapped == typeof(uint) || unwrapped == typeof(ulong) ||
               unwrapped == typeof(ushort) || unwrapped == typeof(sbyte);
    }

    public static bool IsCollectionType(Type type)
    {
        var unwrapped = UnwrapTask(Nullable.GetUnderlyingType(type) ?? type);

        if (unwrapped.IsArray)
            return true;

        if (!unwrapped.IsGenericType)
            return false;

        var def = unwrapped.GetGenericTypeDefinition();
        return def == typeof(List<>) ||
               def == typeof(IList<>) ||
               def == typeof(IEnumerable<>);
    }

    public static bool IsDictionaryType(Type type)
    {
        var unwrapped = UnwrapTask(Nullable.GetUnderlyingType(type) ?? type);

        if (!unwrapped.IsGenericType)
            return false;

        var def = unwrapped.GetGenericTypeDefinition();
        return def == typeof(Dictionary<,>) || def == typeof(IDictionary<,>);
    }

    /// <summary>
    /// True for CLR reference returns that can meaningfully be null/undefined in JS
    /// (includes string, collections, and object graphs; excludes void and non-nullable value types).
    /// </summary>
    public static bool IsReferenceReturnType(Type type)
    {
        var unwrapped = UnwrapTask(Nullable.GetUnderlyingType(type) ?? type);

        if (unwrapped == typeof(void))
            return false;

        if (Nullable.GetUnderlyingType(type) != null ||
            (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>) &&
             Nullable.GetUnderlyingType(type.GetGenericArguments()[0]) != null))
        {
            return true;
        }

        return !unwrapped.IsValueType;
    }

    public static string GetReturnKind(Type type)
    {
        var unwrapped = UnwrapTask(Nullable.GetUnderlyingType(type) ?? type);

        if (unwrapped == typeof(void)) return "void";
        if (unwrapped == typeof(bool)) return "boolean";
        if (unwrapped == typeof(string)) return "string";
        if (unwrapped == typeof(int) || unwrapped == typeof(long) || unwrapped == typeof(short) ||
            unwrapped == typeof(byte) || unwrapped == typeof(float) || unwrapped == typeof(double) ||
            unwrapped == typeof(decimal) || unwrapped == typeof(uint) || unwrapped == typeof(ulong) ||
            unwrapped == typeof(ushort) || unwrapped == typeof(sbyte))
        {
            return "number";
        }

        if (unwrapped == typeof(DateTime)) return "date";
        if (unwrapped.IsEnum) return "enum";
        if (IsDictionaryType(unwrapped)) return "dictionary";
        if (IsCollectionType(unwrapped)) return "collection";
        if (IsComplexObjectType(unwrapped)) return "object";
        return "reference";
    }
}
