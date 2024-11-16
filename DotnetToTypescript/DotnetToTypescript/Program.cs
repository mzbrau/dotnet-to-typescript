using Cocona;
using DotnetToTypescript;
using DotnetToTypescript.AssemblyHandling;
using DotnetToTypescript.Commands;
using DotnetToTypescript.IO;
using DotnetToTypescript.Typescript;
using Microsoft.Extensions.DependencyInjection;

var builder = CoconaApp.CreateBuilder();

builder.Services.AddSingleton<IAssemblyLoader, AssemblyLoader>();
builder.Services.AddSingleton<IScriptTypeExtractor, ScriptTypeExtractor>();
builder.Services.AddSingleton<IDefinitionGenerator, TypeScriptDefinitionGenerator>();
builder.Services.AddSingleton<IFileSystem, FileSystem>();
builder.Services.AddSingleton<GenerateCommand>();

var app = builder.Build();

app.AddCommand("generate", (
    [Argument(Description = "Paths to DLL files")] string[] dllPaths,
    [Option('o', Description = "Output directory")] string? outputDirectory,
    GenerateCommand command) => command.ExecuteAsync(dllPaths, outputDirectory));

await app.RunAsync();