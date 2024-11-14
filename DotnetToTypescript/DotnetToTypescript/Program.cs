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
    var dllPath = args[0];
    var assembly = assemblyLoader.LoadAssembly(dllPath);
    var scriptClasses = scriptTypeExtractor.ExtractScriptClasses(assembly);
    var typeScriptDefinition = definitionGenerator.GenerateDefinitions(scriptClasses);

    var outputPath = Path.ChangeExtension(dllPath, ".d.ts");
    File.WriteAllText(outputPath, typeScriptDefinition);
    Console.WriteLine($"TypeScript definition file created: {outputPath}");
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