using System.Reflection;

namespace DotnetToTypescript;

public interface IAssemblyLoader
{
    Assembly LoadAssembly(string path);
}