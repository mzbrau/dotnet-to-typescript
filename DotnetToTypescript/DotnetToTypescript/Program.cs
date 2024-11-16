using DotnetToTypescript;
using Microsoft.Extensions.DependencyInjection;

if (args.Length == 0)
{
    Console.WriteLine("Usage: DllToTypeScript <path-to-dll1> [path-to-dll2] ...");
    return;
}

var serviceProvider = ConfigureServices();
var assemblyLoader = serviceProvider.GetRequiredService<IAssemblyLoader>();
var scriptTypeExtractor = serviceProvider.GetRequiredService<IScriptTypeExtractor>();
var definitionGenerator = serviceProvider.GetRequiredService<IDefinitionGenerator>();

try
{
    var scriptClasses = args.Select(a => assemblyLoader.LoadAssembly(a))
        .SelectMany(assembly => scriptTypeExtractor.ExtractScriptClasses(assembly))
        .ToList();

    // Generate one combined definition file
    var outputPathDts = Path.ChangeExtension(args[0], ".d.ts");
    var typeScriptDefinition = definitionGenerator.GenerateDefinitions(scriptClasses);
    File.WriteAllText(outputPathDts, typeScriptDefinition);
    Console.WriteLine($"TypeScript definition file created: {outputPathDts}");

    // Generate one combined instance file
    var typeScriptInstances = definitionGenerator.GenerateInstances(scriptTypeExtractor.ScriptCreateNames, outputPathDts);
    if (!string.IsNullOrEmpty(typeScriptInstances))
    {
        var outputPathTs = Path.ChangeExtension(args[0], ".ts");
        File.WriteAllText(outputPathTs, typeScriptInstances);
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