using System.Reflection;

namespace DotnetToTypescript.AssemblyHandling;

public interface IAssemblyLoader
{
    Assembly LoadAssembly(string path);
}