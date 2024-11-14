using System.Reflection;

namespace DotnetToTypescript;

public class AssemblyLoader : IAssemblyLoader
{
    public Assembly LoadAssembly(string path)
    {
        return Assembly.LoadFrom(path);
    }
}