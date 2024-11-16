using System.Reflection;
using Microsoft.Extensions.Logging;

namespace DotnetToTypescript.AssemblyHandling;

public class AssemblyLoader : IAssemblyLoader
{
    private readonly ILogger<AssemblyLoader> _logger;

    public AssemblyLoader(ILogger<AssemblyLoader> logger)
    {
        _logger = logger;
    }

    public Assembly LoadAssembly(string path)
    {
        _logger.LogDebug("Loading assembly from path: {Path}", path);
        try
        {
            var assembly = Assembly.LoadFrom(path);
            _logger.LogDebug("Successfully loaded assembly: {Name}", assembly.GetName().Name);
            return assembly;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load assembly from path: {Path}", path);
            throw;
        }
    }
}