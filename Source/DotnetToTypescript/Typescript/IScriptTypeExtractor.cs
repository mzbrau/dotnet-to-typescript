using System.Reflection;

namespace DotnetToTypescript.Typescript;

public interface IScriptTypeExtractor
{
    Type? TypeAttribute { get; }
    
    void InitializeAttributes(IEnumerable<Assembly> assemblies);
    
    List<Type> ExtractScriptClasses(Assembly assembly);
    
    Dictionary<(Type Type, string InstanceName), string> ScriptCreateNames { get; }
    
    Dictionary<(Type Type, string PropertyName), string> ScriptPropertyNames { get; }
}