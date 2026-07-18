using Cocona;
using DotnetToTypescript.AssemblyHandling;
using DotnetToTypescript.Commands;
using DotnetToTypescript.IO;
using DotnetToTypescript.Testing;
using DotnetToTypescript.Typescript;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

var builder = CoconaApp.CreateBuilder();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

// Add logging
builder.Services.AddLogging(loggingBuilder =>
    loggingBuilder.ClearProviders()
        .AddSerilog(Log.Logger, dispose: true));

builder.Services.AddSingleton<IAssemblyLoader, AssemblyLoader>();
builder.Services.AddSingleton<IScriptTypeExtractor, ScriptTypeExtractor>();
builder.Services.AddSingleton<IDefinitionGenerator, TypeScriptDefinitionGenerator>();
builder.Services.AddSingleton<ITestEnvironmentGenerator, TestEnvironmentGenerator>();
builder.Services.AddSingleton<IFileSystem, FileSystem>();
builder.Services.AddSingleton<GenerateCommand>();

var app = builder.Build();

app.AddCommand("generate", (
    [Argument(Description = "Paths to DLL files")] string[] dllPaths,
    [Option('o', Description = "Output directory")] string? outputDirectory,
    [Option('p', Description = "Preserve original casing")] bool preserveCase,
    [Option('n', Description = "Output filename (without extension)")] string? outputName,
    [Option("js", Description = "Generate instance stubs as .js instead of .ts")] bool js,
    [Option("javascript", Description = "Generate instance stubs as .js instead of .ts")] bool javascript,
    [Option('t', Description = "Generate Vitest testing environment")] bool test,
    GenerateCommand command) => command.ExecuteAsync(
        dllPaths, outputDirectory, preserveCase, outputName, js || javascript, test));

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
