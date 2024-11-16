using System.Reflection;

namespace DotnetToTypescript.Typescript;

public interface IScriptTypeExtractor
{
    List<Type> ExtractScriptClasses(Assembly assembly);
    
    Dictionary<Type, string> ScriptCreateNames { get; }
}