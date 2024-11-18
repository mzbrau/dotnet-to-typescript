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

    public async Task ExecuteAsync(string[] dllPaths, string? outputDirectory = null, bool preserveCase = false, string? outputName = null)
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
            
            // First load all assemblies
            var assemblies = dllPaths.Select(path =>
            {
                _logger.LogDebug("Loading assembly: {Path}", path);
                return _assemblyLoader.LoadAssembly(path);
            }).ToList();

            // Initialize attributes from all assemblies
            _scriptTypeExtractor.InitializeAttributes(assemblies);

            // Then process each assembly
            var scriptClasses = assemblies
                .SelectMany(assembly =>
                {
                    _logger.LogDebug("Extracting script classes from assembly: {Name}", assembly.GetName().Name);
                    return _scriptTypeExtractor.ExtractScriptClasses(assembly);
                })
                .OrderBy(type => type.FullName)
                .ToList();

            _logger.LogInformation("Found {Count} script classes to process", scriptClasses.Count);

            // Always use the assembly with the most types as the base for naming
            var primaryAssembly = assemblies
                .OrderByDescending(a => scriptClasses.Count(c => c.Assembly == a))
                .First();
            
            var basePath = outputDirectory != null 
                ? _fileSystem.Combine(outputDirectory, outputName ?? _fileSystem.GetFileNameWithoutExtension(dllPaths[Array.IndexOf(dllPaths, primaryAssembly.Location)]))
                : outputName != null 
                    ? _fileSystem.Combine(_fileSystem.GetDirectoryName(dllPaths[0]), outputName)
                    : dllPaths[0];

            if (outputDirectory != null)
            {
                _logger.LogInformation("Creating output directory: {Path}", outputDirectory);
                _fileSystem.CreateDirectory(outputDirectory);
            }

            await GenerateDefinitionFile(basePath, scriptClasses, preserveCase);
            await GenerateInstanceFile(basePath);
            
            _logger.LogInformation("TypeScript generation completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating TypeScript definitions");
            throw;
        }
    }

    private async Task GenerateDefinitionFile(string basePath, List<Type> scriptClasses, bool preserveCase)
    {
        var outputPathDts = _fileSystem.ChangeExtension(basePath, ".d.ts");
        _logger.LogInformation("Generating TypeScript definition file: {Path}", outputPathDts);
        
        // Initialize the generator with the type attribute
        _definitionGenerator.Initialize(_scriptTypeExtractor.TypeAttribute);
        
        var typeScriptDefinition = _definitionGenerator.GenerateDefinitions(scriptClasses, preserveCase);
        await _fileSystem.WriteAllTextAsync(outputPathDts, typeScriptDefinition);
        
        _logger.LogInformation("Successfully generated TypeScript definition file");
    }

    private async Task GenerateInstanceFile(string basePath)
    {
        var outputPathDts = _fileSystem.ChangeExtension(basePath, ".d.ts");
        _logger.LogInformation("Generating TypeScript instances file");
        
        var typeScriptInstances = _definitionGenerator.GenerateInstances(
            _scriptTypeExtractor.ScriptCreateNames,
            _scriptTypeExtractor.ScriptPropertyNames,
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