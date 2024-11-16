using System.Reflection;

namespace DotnetToTypescript;

public interface IScriptTypeExtractor
{
    List<Type> ExtractScriptClasses(Assembly assembly);
    Dictionary<Type, string> ScriptCreateNames { get; }
}