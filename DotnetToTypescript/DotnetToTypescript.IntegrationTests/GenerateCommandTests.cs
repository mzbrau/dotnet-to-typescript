using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using DotnetToTypescript.Commands;
using DotnetToTypescript.AssemblyHandling;
using DotnetToTypescript.IO;
using DotnetToTypescript.Typescript;

namespace DotnetToTypescript.IntegrationTests;

[TestFixture]
public class GenerateCommandTests
{
    private GenerateCommand _command = null!;
    private readonly string _outputPath = Path.GetTempPath();

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAssemblyLoader, AssemblyLoader>();
        services.AddSingleton<IScriptTypeExtractor, ScriptTypeExtractor>();
        services.AddSingleton<IDefinitionGenerator, TypeScriptDefinitionGenerator>();
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<GenerateCommand>();
        services.AddSingleton<IDefinitionGenerator, TypeScriptDefinitionGenerator>();
        var serviceProvider = services.BuildServiceProvider();
        _command = serviceProvider.GetRequiredService<GenerateCommand>();
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(_outputPath))
                Directory.Delete(_outputPath, true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    [Test]
    public async Task Generate_WithSampleLibraries_GeneratesExpectedOutput()
    {
        // Arrange
        var sampleLibraryPath = GetAssemblyPath(typeof(SampleLibrary.User).Assembly);
        var sampleLibrary2Path = GetAssemblyPath(typeof(SampleLibrary2.Truck).Assembly);
        var outputDirectory = Path.GetDirectoryName(_outputPath);

        // Act
        await _command.ExecuteAsync(
            [sampleLibraryPath, sampleLibrary2Path], 
            outputDirectory);

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

    private string GetAssemblyPath(Assembly assembly)
    {
        return assembly.Location;
    }
} 