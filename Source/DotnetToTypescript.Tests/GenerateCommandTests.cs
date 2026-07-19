using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using DotnetToTypescript.Commands;
using DotnetToTypescript.AssemblyHandling;
using DotnetToTypescript.IO;
using DotnetToTypescript.Testing;
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
        services.AddSingleton<ITestEnvironmentGenerator, TestEnvironmentGenerator>();
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

    [Test]
    public async Task Generate_WithJavascriptFlag_GeneratesJsInstanceFile()
    {
        var sampleLibraryPath = GetAssemblyPath(typeof(SampleLibrary.User).Assembly);
        var outputDirectory = Path.Combine(_outputPath, "js-flag");

        await _command?.ExecuteAsync(
            [sampleLibraryPath],
            outputDirectory,
            outputName: "sample",
            javascript: true)!;

        var dtsPath = Path.Combine(outputDirectory, "sample.d.ts");
        var jsPath = Path.Combine(outputDirectory, "sample.js");
        var tsPath = Path.Combine(outputDirectory, "sample.ts");

        Assert.That(File.Exists(dtsPath), Is.True);
        Assert.That(File.Exists(jsPath), Is.True);
        Assert.That(File.Exists(tsPath), Is.False);

        var settings = new VerifySettings();
        settings.UseDirectory("Snapshots");

        await Verify(new
        {
            TypeScriptDefinitions = await File.ReadAllTextAsync(dtsPath),
            JavaScriptInstances = await File.ReadAllTextAsync(jsPath)
        }, settings);
    }

    [Test]
    public async Task Generate_WithTestFlag_GeneratesVitestEnvironment()
    {
        var sampleLibraryPath = GetAssemblyPath(typeof(SampleLibrary.User).Assembly);
        var outputDirectory = Path.Combine(_outputPath, "test-flag");

        await _command?.ExecuteAsync(
            [sampleLibraryPath],
            outputDirectory,
            outputName: "sample",
            test: true)!;

        var settings = new VerifySettings();
        settings.UseDirectory("Snapshots");

        await Verify(new
        {
            PackageJson = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "package.json")),
            TsConfig = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "tsconfig.json")),
            VitestConfig = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "vitest.config.js")),
            Setup = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "test", "setup.js")),
            ExecuteScript = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "test", "executeScript.js")),
            ResetMocks = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "test", "resetMocks.js")),
            Factories = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "test", "factories.js")),
            MocksIndex = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "test", "mocks", "index.js")),
            MikeMock = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "test", "mocks", "mike.js")),
            SampleTest = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "test", "samples", "sample.test.js")),
            SampleScript = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "test", "scripts", "sample-script.js")),
            RobustnessApiCatalog = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "test", "robustness", "apiCatalog.js")),
            RobustnessRunner = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "test", "robustness", "runRobustnessTests.js")),
            RobustnessMutationsIndex = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "test", "robustness", "mutations", "index.js")),
            RobustnessTest = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "test", "samples", "robustness.test.js")),
            RobustSampleScript = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "test", "scripts", "robust-sample.js")),
            Readme = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "README.md")),
            HasTypeScriptInstances = File.Exists(Path.Combine(outputDirectory, "sample.ts")),
            HasJavaScriptInstances = File.Exists(Path.Combine(outputDirectory, "sample.js"))
        }, settings);
    }

    [Test]
    public async Task Generate_WithTestAndJavascriptFlags_GeneratesBoth()
    {
        var sampleLibraryPath = GetAssemblyPath(typeof(SampleLibrary.User).Assembly);
        var outputDirectory = Path.Combine(_outputPath, "test-and-js");

        await _command?.ExecuteAsync(
            [sampleLibraryPath],
            outputDirectory,
            outputName: "sample",
            javascript: true,
            test: true)!;

        var settings = new VerifySettings();
        settings.UseDirectory("Snapshots");

        await Verify(new
        {
            JavaScriptInstances = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "sample.js")),
            HasTypeScriptInstances = File.Exists(Path.Combine(outputDirectory, "sample.ts")),
            PackageJson = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "package.json")),
            Setup = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "test", "setup.js")),
            MikeMock = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "test", "mocks", "mike.js"))
        }, settings);
    }

    [Test]
    public async Task Generate_WithTestAndPreserveCaseFlags_UsesOriginalMemberNamesInSamples()
    {
        var sampleLibraryPath = GetAssemblyPath(typeof(SampleLibrary.User).Assembly);
        var outputDirectory = Path.Combine(_outputPath, "test-and-preserve-case");

        await _command?.ExecuteAsync(
            [sampleLibraryPath],
            outputDirectory,
            preserveCase: true,
            outputName: "sample",
            test: true)!;

        var sampleScript = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "test", "scripts", "sample-script.js"));
        var sampleTest = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "test", "samples", "sample.test.js"));
        var robustSampleScript = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "test", "scripts", "robust-sample.js"));
        var readme = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "README.md"));

        Assert.Multiple(() =>
        {
            Assert.That(sampleScript, Does.Contain("var value = dave.Address;"));
            Assert.That(sampleScript, Does.Contain("dave.AddPhoneNumber();"));
            Assert.That(sampleTest, Does.Contain("globalThis.dave.AddPhoneNumber.mockReturnValue(undefined);"));
            Assert.That(sampleTest, Does.Contain("expect(globalThis.dave.AddPhoneNumber).toHaveBeenCalled();"));
            Assert.That(sampleTest, Does.Contain("const original = globalThis.dave.Address;"));
            Assert.That(sampleTest, Does.Contain("globalThis.dave.Address = \"changed-in-test\";"));
            Assert.That(robustSampleScript, Does.Contain("var value = root.Address;"));
            Assert.That(robustSampleScript, Does.Contain("if (typeof root.AddPhoneNumber === \"function\") {"));
            Assert.That(readme, Does.Contain("dave.AddPhoneNumber.mockReturnValue"));
            Assert.That(readme, Does.Contain("expect(dave.AddPhoneNumber).toHaveBeenCalled();"));
        });
    }

    private string GetAssemblyPath(Assembly assembly)
    {
        return assembly.Location;
    }
} 