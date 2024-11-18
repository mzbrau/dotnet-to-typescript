using Cocona;
using DotnetToTypescript.AssemblyHandling;
using DotnetToTypescript.Commands;
using DotnetToTypescript.IO;
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
builder.Services.AddSingleton<IFileSystem, FileSystem>();
builder.Services.AddSingleton<GenerateCommand>();

var app = builder.Build();

app.AddCommand("generate", (
    [Argument(Description = "Paths to DLL files")] string[] dllPaths,
    [Option('o', Description = "Output directory")] string? outputDirectory,
    [Option('p', Description = "Preserve original casing")] bool preserveCase,
    [Option('n', Description = "Output filename (without extension)")] string? outputName,
    GenerateCommand command) => command.ExecuteAsync(dllPaths, outputDirectory, preserveCase, outputName));

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