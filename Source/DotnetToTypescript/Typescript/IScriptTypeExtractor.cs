using System.Reflection;

namespace DotnetToTypescript.Typescript;

public interface IScriptTypeExtractor
{
    void InitializeAttributes(IEnumerable<Assembly> assemblies);
    
    List<Type> ExtractScriptClasses(Assembly assembly);
    
    Dictionary<Type, string> ScriptCreateNames { get; }
    
    Dictionary<(Type Type, string PropertyName), string> ScriptPropertyNames { get; }
}