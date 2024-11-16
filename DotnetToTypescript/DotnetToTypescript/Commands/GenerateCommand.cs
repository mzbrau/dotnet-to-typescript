using Cocona;
using DotnetToTypescript.AssemblyHandling;
using DotnetToTypescript.Typescript;

namespace DotnetToTypescript.Commands;

public class GenerateCommand
{
    private readonly IAssemblyLoader _assemblyLoader;
    private readonly IScriptTypeExtractor _scriptTypeExtractor;
    private readonly IDefinitionGenerator _definitionGenerator;

    public GenerateCommand(
        IAssemblyLoader assemblyLoader,
        IScriptTypeExtractor scriptTypeExtractor,
        IDefinitionGenerator definitionGenerator)
    {
        _assemblyLoader = assemblyLoader;
        _scriptTypeExtractor = scriptTypeExtractor;
        _definitionGenerator = definitionGenerator;
    }

    public async Task ExecuteAsync(
        [Argument(Description = "Paths to DLL files")] 
        string[] dllPaths)
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

            await GenerateDefinitionFile(dllPaths[0], scriptClasses);
            await GenerateInstanceFile(dllPaths[0]);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private async Task GenerateDefinitionFile(string basePath, List<Type> scriptClasses)
    {
        var outputPathDts = Path.ChangeExtension(basePath, ".d.ts");
        var typeScriptDefinition = _definitionGenerator.GenerateDefinitions(scriptClasses);
        await File.WriteAllTextAsync(outputPathDts, typeScriptDefinition);
        Console.WriteLine($"TypeScript definition file created: {outputPathDts}");
    }

    private async Task GenerateInstanceFile(string basePath)
    {
        var outputPathDts = Path.ChangeExtension(basePath, ".d.ts");
        var typeScriptInstances = _definitionGenerator.GenerateInstances(
            _scriptTypeExtractor.ScriptCreateNames,
            outputPathDts);

        if (!string.IsNullOrEmpty(typeScriptInstances))
        {
            var outputPathTs = Path.ChangeExtension(basePath, ".ts");
            await File.WriteAllTextAsync(outputPathTs, typeScriptInstances);
            Console.WriteLine($"TypeScript instance file created: {outputPathTs}");
        }
    }
} 