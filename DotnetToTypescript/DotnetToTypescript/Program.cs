using DotnetToTypescript;
using Microsoft.Extensions.DependencyInjection;

if (args.Length == 0)
{
    Console.WriteLine("Usage: DllToTypeScript <path-to-dll>");
    return;
}

var serviceProvider = ConfigureServices();
var assemblyLoader = serviceProvider.GetRequiredService<IAssemblyLoader>();
var scriptTypeExtractor = serviceProvider.GetRequiredService<IScriptTypeExtractor>();
var definitionGenerator = serviceProvider.GetRequiredService<IDefinitionGenerator>();

try
{
    string dllPath = args[0];
    var assembly = assemblyLoader.LoadAssembly(dllPath);
    var scriptClasses = scriptTypeExtractor.ExtractScriptClasses(assembly);

    var typeScriptDefinition = definitionGenerator.GenerateDefinitions(scriptClasses);
    var outputPathDts = System.IO.Path.ChangeExtension(dllPath, ".d.ts");
    System.IO.File.WriteAllText(outputPathDts, typeScriptDefinition);
    Console.WriteLine($"TypeScript definition file created: {outputPathDts}");

    var typeScriptInstances = definitionGenerator.GenerateInstances(scriptTypeExtractor.ScriptCreateNames, outputPathDts);
    if (!string.IsNullOrEmpty(typeScriptInstances))
    {
        var outputPathTs = System.IO.Path.ChangeExtension(dllPath, ".ts");
        System.IO.File.WriteAllText(outputPathTs, typeScriptInstances);
        Console.WriteLine($"TypeScript instance file created: {outputPathTs}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}


ServiceProvider ConfigureServices()
{
    var services = new ServiceCollection();
    services.AddSingleton<IAssemblyLoader, AssemblyLoader>();
    services.AddSingleton<IScriptTypeExtractor, ScriptTypeExtractor>();
    services.AddSingleton<IDefinitionGenerator, TypeScriptDefinitionGenerator>();
    return services.BuildServiceProvider();
}