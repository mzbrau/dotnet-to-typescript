using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using DotnetToTypescript.Commands;
using DotnetToTypescript.AssemblyHandling;
using DotnetToTypescript.IO;
using DotnetToTypescript.Typescript;
using Microsoft.Extensions.Logging;
using Serilog;

namespace DotnetToTypescript.IntegrationTests;

[TestFixture]
public class GenerateCommandTests
{
    private GenerateCommand? _command;
    private readonly string _outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        // Add logging
        services.AddLogging(loggingBuilder =>
            loggingBuilder.ClearProviders()
                .AddSerilog(Log.Logger, dispose: true));
                
        services.AddSingleton<IAssemblyLoader, AssemblyLoader>();
        services.AddSingleton<IScriptTypeExtractor, ScriptTypeExtractor>();
        services.AddSingleton<IDefinitionGenerator, TypeScriptDefinitionGenerator>();
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<GenerateCommand>();
        
        var serviceProvider = services.BuildServiceProvider();
        _command = serviceProvider.GetRequiredService<GenerateCommand>();
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            _command = null;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    [Test]
    public async Task Generate_WithSampleLibraries_GeneratesExpectedOutput_WithArgumentsReversed()
    {
        // Arrange
        var sampleLibraryPath = GetAssemblyPath(typeof(SampleLibrary.User).Assembly);
        var sampleLibrary2Path = GetAssemblyPath(typeof(SampleLibrary2.Truck).Assembly);
        var outputDirectory = Path.GetDirectoryName(_outputPath);

        // Act
        await _command?.ExecuteAsync(
            [sampleLibrary2Path, sampleLibraryPath], 
            outputDirectory,
            outputName: null)!;

        // Assert
        var outputBaseName = Path.GetFileNameWithoutExtension(sampleLibrary2Path);
        var dtsPath = Path.Combine(outputDirectory!, outputBaseName + ".d.ts");
        var tsPath = Path.Combine(outputDirectory!, outputBaseName + ".ts");

        var dtsContent = await File.ReadAllTextAsync(dtsPath);
        var tsContent = await File.ReadAllTextAsync(tsPath);

        var settings = new VerifySettings();
        settings.UseDirectory("Snapshots");
        
        await Verify(new
        {
            TypeScriptDefinitions = dtsContent,
            TypeScriptInstances = tsContent
        }, settings);
    }

    [Test]
    public async Task Generate_WithSampleLibraries_GeneratesExpectedOutput()
    {
        // Arrange
        var sampleLibraryPath = GetAssemblyPath(typeof(SampleLibrary.User).Assembly);
        var sampleLibrary2Path = GetAssemblyPath(typeof(SampleLibrary2.Truck).Assembly);
        var outputDirectory = Path.GetDirectoryName(_outputPath);

        // Act
        await _command?.ExecuteAsync(
            [sampleLibraryPath, sampleLibrary2Path], 
            outputDirectory,
            outputName: null)!;

        // Assert
        var outputBaseName = Path.GetFileNameWithoutExtension(sampleLibrary2Path);
        var dtsPath = Path.Combine(outputDirectory!, outputBaseName + ".d.ts");
        var tsPath = Path.Combine(outputDirectory!, outputBaseName + ".ts");

        var dtsContent = await File.ReadAllTextAsync(dtsPath);
        var tsContent = await File.ReadAllTextAsync(tsPath);

        var settings = new VerifySettings();
        settings.UseDirectory("Snapshots");
        
        await Verify(new
        {
            TypeScriptDefinitions = dtsContent,
            TypeScriptInstances = tsContent
        }, settings);
    }
    
    [Test]
    public async Task Generate_WithSingleLibrary_GeneratesExpectedOutput()
    {
        // Arrange
        var sampleLibraryPath = GetAssemblyPath(typeof(SampleLibrary.User).Assembly);
        var outputDirectory = Path.GetDirectoryName(_outputPath);

        // Act
        await _command?.ExecuteAsync(
            [sampleLibraryPath], 
            outputDirectory,
            outputName: null)!;

        // Assert
        var outputBaseName = Path.GetFileNameWithoutExtension(sampleLibraryPath);
        var dtsPath = Path.Combine(outputDirectory!, outputBaseName + ".d.ts");
        var tsPath = Path.Combine(outputDirectory!, outputBaseName + ".ts");

        var dtsContent = await File.ReadAllTextAsync(dtsPath);
        var tsContent = await File.ReadAllTextAsync(tsPath);

        var settings = new VerifySettings();
        settings.UseDirectory("Snapshots");
        
        await Verify(new
        {
            TypeScriptDefinitions = dtsContent,
            TypeScriptInstances = tsContent
        }, settings);
    }

    [Test]
    public async Task Generate_WithCustomOutputName_GeneratesExpectedOutput()
    {
        // Arrange
        var sampleLibraryPath = GetAssemblyPath(typeof(SampleLibrary.User).Assembly);
        var outputDirectory = Path.GetDirectoryName(_outputPath);
        const string customOutputName = "custom-types";

        // Act
        await _command?.ExecuteAsync(
            [sampleLibraryPath], 
            outputDirectory,
            outputName: customOutputName)!;

        // Assert
        var dtsPath = Path.Combine(outputDirectory!, customOutputName + ".d.ts");
        var tsPath = Path.Combine(outputDirectory!, customOutputName + ".ts");

        var dtsContent = await File.ReadAllTextAsync(dtsPath);
        var tsContent = await File.ReadAllTextAsync(tsPath);

        var settings = new VerifySettings();
        settings.UseDirectory("Snapshots");
        
        await Verify(new
        {
            TypeScriptDefinitions = dtsContent,
            TypeScriptInstances = tsContent
        }, settings);
    }

    [Test]
    public async Task Generate_WithCustomOutputName_AndMultipleLibraries_GeneratesExpectedOutput()
    {
        // Arrange
        var sampleLibraryPath = GetAssemblyPath(typeof(SampleLibrary.User).Assembly);
        var sampleLibrary2Path = GetAssemblyPath(typeof(SampleLibrary2.Truck).Assembly);
        var outputDirectory = Path.GetDirectoryName(_outputPath);
        const string customOutputName = "combined-types";

        // Act
        await _command?.ExecuteAsync(
            [sampleLibraryPath, sampleLibrary2Path], 
            outputDirectory,
            outputName: customOutputName)!;

        // Assert
        var dtsPath = Path.Combine(outputDirectory!, customOutputName + ".d.ts");
        var tsPath = Path.Combine(outputDirectory!, customOutputName + ".ts");

        var dtsContent = await File.ReadAllTextAsync(dtsPath);
        var tsContent = await File.ReadAllTextAsync(tsPath);

        var settings = new VerifySettings();
        settings.UseDirectory("Snapshots");
        
        await Verify(new
        {
            TypeScriptDefinitions = dtsContent,
            TypeScriptInstances = tsContent
        }, settings);
    }

    [Test]
    public async Task Generate_WithCustomOutputName_NoOutputDirectory_GeneratesExpectedOutput()
    {
        // Arrange
        var sampleLibraryPath = GetAssemblyPath(typeof(SampleLibrary.User).Assembly);
        const string customOutputName = "types-in-source-dir";

        // Act
        await _command?.ExecuteAsync(
            [sampleLibraryPath], 
            outputName: customOutputName)!;

        // Assert
        var sourceDirectory = Path.GetDirectoryName(sampleLibraryPath)!;
        var dtsPath = Path.Combine(sourceDirectory, customOutputName + ".d.ts");
        var tsPath = Path.Combine(sourceDirectory, customOutputName + ".ts");

        var dtsContent = await File.ReadAllTextAsync(dtsPath);
        var tsContent = await File.ReadAllTextAsync(tsPath);

        var settings = new VerifySettings();
        settings.UseDirectory("Snapshots");
        
        await Verify(new
        {
            TypeScriptDefinitions = dtsContent,
            TypeScriptInstances = tsContent
        }, settings);
    }

    private string GetAssemblyPath(Assembly assembly)
    {
        return assembly.Location;
    }
} 