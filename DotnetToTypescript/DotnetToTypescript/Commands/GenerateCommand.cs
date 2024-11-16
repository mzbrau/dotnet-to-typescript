using Cocona;
using DotnetToTypescript.AssemblyHandling;
using DotnetToTypescript.IO;
using DotnetToTypescript.Typescript;

namespace DotnetToTypescript.Commands;

public class GenerateCommand
{
    private readonly IAssemblyLoader _assemblyLoader;
    private readonly IScriptTypeExtractor _scriptTypeExtractor;
    private readonly IDefinitionGenerator _definitionGenerator;
    private readonly IFileSystem _fileSystem;

    public GenerateCommand(
        IAssemblyLoader assemblyLoader,
        IScriptTypeExtractor scriptTypeExtractor,
        IDefinitionGenerator definitionGenerator,
        IFileSystem fileSystem)
    {
        _assemblyLoader = assemblyLoader;
        _scriptTypeExtractor = scriptTypeExtractor;
        _definitionGenerator = definitionGenerator;
        _fileSystem = fileSystem;
    }

    public async Task ExecuteAsync(string[] dllPaths, string? outputDirectory = null)
    {
        if (dllPaths.Length == 0)
        {
            Console.WriteLine("No dll files specified. Exiting.");
            return;
        }

        try
        {
            var scriptClasses = dllPaths.Select(_assemblyLoader.LoadAssembly)
                .SelectMany(assembly => _scriptTypeExtractor.ExtractScriptClasses(assembly))
                .ToList();

            var basePath = outputDirectory != null 
                ? _fileSystem.Combine(outputDirectory, _fileSystem.GetFileNameWithoutExtension(dllPaths[0]))
                : dllPaths[0];

            if (outputDirectory != null)
            {
                _fileSystem.CreateDirectory(outputDirectory);
            }

            await GenerateDefinitionFile(basePath, scriptClasses);
            await GenerateInstanceFile(basePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            throw;
        }
    }

    private async Task GenerateDefinitionFile(string basePath, List<Type> scriptClasses)
    {
        var outputPathDts = _fileSystem.ChangeExtension(basePath, ".d.ts");
        var typeScriptDefinition = _definitionGenerator.GenerateDefinitions(scriptClasses);
        await _fileSystem.WriteAllTextAsync(outputPathDts, typeScriptDefinition);
    }

    private async Task GenerateInstanceFile(string basePath)
    {
        var outputPathDts = _fileSystem.ChangeExtension(basePath, ".d.ts");
        var typeScriptInstances = _definitionGenerator.GenerateInstances(
            _scriptTypeExtractor.ScriptCreateNames,
            outputPathDts);

        if (!string.IsNullOrEmpty(typeScriptInstances))
        {
            var outputPathTs = _fileSystem.ChangeExtension(basePath, ".ts");
            await _fileSystem.WriteAllTextAsync(outputPathTs, typeScriptInstances);
        }
    }
} 