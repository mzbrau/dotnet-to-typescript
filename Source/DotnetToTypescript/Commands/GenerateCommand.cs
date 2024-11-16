using DotnetToTypescript.AssemblyHandling;
using DotnetToTypescript.IO;
using DotnetToTypescript.Typescript;
using Microsoft.Extensions.Logging;

namespace DotnetToTypescript.Commands;

public class GenerateCommand
{
    private readonly IAssemblyLoader _assemblyLoader;
    private readonly IScriptTypeExtractor _scriptTypeExtractor;
    private readonly IDefinitionGenerator _definitionGenerator;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<GenerateCommand> _logger;

    public GenerateCommand(
        IAssemblyLoader assemblyLoader,
        IScriptTypeExtractor scriptTypeExtractor,
        IDefinitionGenerator definitionGenerator,
        IFileSystem fileSystem,
        ILogger<GenerateCommand> logger)
    {
        _assemblyLoader = assemblyLoader;
        _scriptTypeExtractor = scriptTypeExtractor;
        _definitionGenerator = definitionGenerator;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public async Task ExecuteAsync(string[] dllPaths, string? outputDirectory = null)
    {
        if (dllPaths.Length == 0)
        {
            _logger.LogWarning("No dll files specified. Exiting.");
            return;
        }

        _logger.LogInformation("Starting TypeScript definition generation for {Count} assemblies", dllPaths.Length);

        try
        {
            _logger.LogInformation("Loading assemblies and extracting script classes");
            var scriptClasses = dllPaths.Select(path =>
            {
                _logger.LogDebug("Loading assembly: {Path}", path);
                return _assemblyLoader.LoadAssembly(path);
            })
            .SelectMany(assembly =>
            {
                _logger.LogDebug("Extracting script classes from assembly: {Name}", assembly.GetName().Name);
                return _scriptTypeExtractor.ExtractScriptClasses(assembly);
            })
            .ToList();

            _logger.LogInformation("Found {Count} script classes to process", scriptClasses.Count);

            var basePath = outputDirectory != null 
                ? _fileSystem.Combine(outputDirectory, _fileSystem.GetFileNameWithoutExtension(dllPaths[0]))
                : dllPaths[0];

            if (outputDirectory != null)
            {
                _logger.LogInformation("Creating output directory: {Path}", outputDirectory);
                _fileSystem.CreateDirectory(outputDirectory);
            }

            await GenerateDefinitionFile(basePath, scriptClasses);
            await GenerateInstanceFile(basePath);
            
            _logger.LogInformation("TypeScript generation completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating TypeScript definitions");
            throw;
        }
    }

    private async Task GenerateDefinitionFile(string basePath, List<Type> scriptClasses)
    {
        var outputPathDts = _fileSystem.ChangeExtension(basePath, ".d.ts");
        _logger.LogInformation("Generating TypeScript definition file: {Path}", outputPathDts);
        
        var typeScriptDefinition = _definitionGenerator.GenerateDefinitions(scriptClasses);
        await _fileSystem.WriteAllTextAsync(outputPathDts, typeScriptDefinition);
        
        _logger.LogInformation("Successfully generated TypeScript definition file");
    }

    private async Task GenerateInstanceFile(string basePath)
    {
        var outputPathDts = _fileSystem.ChangeExtension(basePath, ".d.ts");
        _logger.LogInformation("Generating TypeScript instances file");
        
        var typeScriptInstances = _definitionGenerator.GenerateInstances(
            _scriptTypeExtractor.ScriptCreateNames,
            outputPathDts);

        if (!string.IsNullOrEmpty(typeScriptInstances))
        {
            var outputPathTs = _fileSystem.ChangeExtension(basePath, ".ts");
            _logger.LogInformation("Writing TypeScript instances file: {Path}", outputPathTs);
            await _fileSystem.WriteAllTextAsync(outputPathTs, typeScriptInstances);
            _logger.LogInformation("Successfully generated TypeScript instances file");
        }
        else
        {
            _logger.LogInformation("No TypeScript instances to generate");
        }
    }
} 