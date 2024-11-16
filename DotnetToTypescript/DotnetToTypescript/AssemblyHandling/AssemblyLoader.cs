using System.Reflection;

namespace DotnetToTypescript.AssemblyHandling;

public class AssemblyLoader : IAssemblyLoader
{
    public Assembly LoadAssembly(string path)
    {
        return Assembly.LoadFrom(path);
    }
}