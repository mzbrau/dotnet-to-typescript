using System.Reflection;
using System.Text;
using DotnetToTypescript.IO;
using DotnetToTypescript.Typescript;
using Microsoft.Extensions.Logging;

namespace DotnetToTypescript.Testing;

public class TestEnvironmentGenerator : ITestEnvironmentGenerator
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<TestEnvironmentGenerator> _logger;

    public TestEnvironmentGenerator(IFileSystem fileSystem, ILogger<TestEnvironmentGenerator> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public async Task GenerateAsync(
        string outputDirectory,
        string definitionFileName,
        IReadOnlyList<Type> scriptClasses,
        Dictionary<(Type Type, string InstanceName), string> scriptCreateNames,
        Dictionary<(Type Type, string PropertyName), string> scriptPropertyNames,
        bool preserveCase)
    {
        _logger.LogInformation("Generating Vitest testing environment in {Directory}", outputDirectory);

        _fileSystem.CreateDirectory(outputDirectory);
        var testDir = _fileSystem.Combine(outputDirectory, "test");
        var mocksDir = _fileSystem.Combine(testDir, "mocks");
        var samplesDir = _fileSystem.Combine(testDir, "samples");
        var scriptsDir = _fileSystem.Combine(testDir, "scripts");
        _fileSystem.CreateDirectory(testDir);
        _fileSystem.CreateDirectory(mocksDir);
        _fileSystem.CreateDirectory(samplesDir);
        _fileSystem.CreateDirectory(scriptsDir);

        var globals = scriptCreateNames
            .Select(e => (GlobalName: MockCodeBuilder.SanitizeFileName(e.Value), Type: e.Key.Type))
            .OrderBy(g => g.GlobalName, StringComparer.Ordinal)
            .ToList();

        var propertyGlobals = BuildPropertyGlobals(scriptPropertyNames, preserveCase);

        await WriteFileAsync(_fileSystem.Combine(outputDirectory, "package.json"), BuildPackageJson());
        await WriteFileAsync(_fileSystem.Combine(outputDirectory, "tsconfig.json"), BuildTsConfig(definitionFileName));
        await WriteFileAsync(_fileSystem.Combine(outputDirectory, "vitest.config.js"), BuildVitestConfig());
        await WriteFileAsync(_fileSystem.Combine(outputDirectory, "README.md"), BuildReadme(globals, definitionFileName));

        foreach (var (globalName, type) in globals)
        {
            var content = MockCodeBuilder.BuildMockModule(globalName, type, preserveCase);
            await WriteFileAsync(_fileSystem.Combine(mocksDir, $"{globalName}.js"), content);
        }

        await WriteFileAsync(
            _fileSystem.Combine(mocksDir, "index.js"),
            MockCodeBuilder.BuildMocksIndex(globals, propertyGlobals));

        await WriteFileAsync(_fileSystem.Combine(testDir, "factories.js"),
            FactoryCodeBuilder.BuildFactoriesModule(scriptClasses, preserveCase));

        await WriteFileAsync(_fileSystem.Combine(testDir, "executeScript.js"), BuildExecuteScript());
        await WriteFileAsync(_fileSystem.Combine(testDir, "resetMocks.js"), BuildResetMocks());
        await WriteFileAsync(_fileSystem.Combine(testDir, "setup.js"), BuildSetup(globals));

        var sampleScriptName = "sample-script.js";
        await WriteFileAsync(
            _fileSystem.Combine(scriptsDir, sampleScriptName),
            BuildSampleScript(globals));

        await WriteFileAsync(
            _fileSystem.Combine(samplesDir, "sample.test.js"),
            BuildSampleTest(globals, sampleScriptName));

        _logger.LogInformation("Vitest testing environment generated successfully");
    }

    private static List<(string GlobalName, string DefaultExpression)> BuildPropertyGlobals(
        Dictionary<(Type Type, string PropertyName), string> scriptPropertyNames,
        bool preserveCase)
    {
        var result = new List<(string, string)>();

        foreach (var entry in scriptPropertyNames.OrderBy(e => e.Value, StringComparer.Ordinal))
        {
            var propertyInfo = entry.Key.Type.GetProperty(
                entry.Key.PropertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            if (propertyInfo == null)
                continue;

            var expression = DefaultValueFactory.GetRecursiveDefaultExpression(
                propertyInfo.PropertyType,
                preserveCase);

            result.Add((MockCodeBuilder.SanitizeFileName(entry.Value), expression));
        }

        return result;
    }

    private async Task WriteFileAsync(string path, string content)
    {
        _logger.LogDebug("Writing {Path}", path);
        await _fileSystem.WriteAllTextAsync(path, content);
    }

    private static string BuildPackageJson() =>
        """
        {
          "name": "dotnet-to-typescript-tests",
          "private": true,
          "type": "module",
          "scripts": {
            "test": "vitest run",
            "test:watch": "vitest",
            "test:coverage": "vitest run --coverage"
          },
          "devDependencies": {
            "@vitest/coverage-v8": "^3.2.4",
            "vitest": "^3.2.4"
          }
        }
        """;

    private static string BuildTsConfig(string definitionFileName) =>
        $$"""
        {
          "compilerOptions": {
            "allowJs": true,
            "checkJs": true,
            "noEmit": true,
            "strict": true,
            "target": "ES2022",
            "module": "ESNext",
            "moduleResolution": "bundler",
            "types": ["vitest/globals"],
            "lib": ["ES2022"]
          },
          "include": [
            "{{definitionFileName}}",
            "**/*.js",
            "**/*.ts"
          ]
        }
        """;

    private static string BuildVitestConfig() =>
        """
        import { defineConfig } from "vitest/config";

        export default defineConfig({
          test: {
            globals: true,
            setupFiles: ["./test/setup.js"],
            include: ["**/*.test.js"],
            coverage: {
              provider: "v8",
              reporter: ["text", "html"],
            },
          },
        });
        """;

    private static string BuildExecuteScript() =>
        """
        import fs from "node:fs";
        import path from "node:path";
        import vm from "node:vm";

        /**
         * Executes a standalone script in a fresh vm context with the current mocks injected.
         * Uses the same mock object references so mockReturnValue / spies apply.
         *
         * @param {string} scriptPath
         * @param {Record<string, unknown>} mockGlobals
         * @returns {Record<string, unknown>}
         */
        export function executeScript(scriptPath, mockGlobals) {
          const absolutePath = path.resolve(scriptPath);
          const code = fs.readFileSync(absolutePath, "utf8");

          const context = {
            ...mockGlobals,
            console,
            globalThis: undefined,
          };

          // Allow scripts to see the injected globals as both bare identifiers and on globalThis.
          context.globalThis = context;

          vm.createContext(context);
          vm.runInContext(code, context, { filename: absolutePath });
          return context;
        }
        """;

    private static string BuildResetMocks() =>
        """
        import { installMocks } from "./mocks/index.js";

        /**
         * Recreates all mocks with default property values and strict method implementations.
         * @param {object} target
         * @returns {Record<string, unknown>}
         */
        export function resetMocks(target = globalThis) {
          return installMocks(target);
        }
        """;

    private static string BuildSetup(IReadOnlyList<(string GlobalName, Type Type)> globals)
    {
        var sb = new StringBuilder();
        sb.AppendLine("import { beforeEach } from \"vitest\";");
        sb.AppendLine("import { executeScript as runScript } from \"./executeScript.js\";");
        sb.AppendLine("import { resetMocks as doResetMocks } from \"./resetMocks.js\";");
        sb.AppendLine("import * as factories from \"./factories.js\";");
        sb.AppendLine();
        sb.AppendLine("/** @type {Record<string, unknown>} */");
        sb.AppendLine("let currentMocks = {};");
        sb.AppendLine();
        sb.AppendLine("export function resetMocks() {");
        sb.AppendLine("  currentMocks = doResetMocks(globalThis);");
        sb.AppendLine("  return currentMocks;");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("/**");
        sb.AppendLine(" * @param {string} scriptPath");
        sb.AppendLine(" */");
        sb.AppendLine("export function executeScript(scriptPath) {");
        sb.AppendLine("  return runScript(scriptPath, currentMocks);");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("Object.assign(globalThis, factories);");
        sb.AppendLine("globalThis.executeScript = executeScript;");
        sb.AppendLine("globalThis.resetMocks = resetMocks;");
        sb.AppendLine();
        sb.AppendLine("beforeEach(() => {");
        sb.AppendLine("  resetMocks();");
        sb.AppendLine("});");
        sb.AppendLine();

        return sb.ToString();
    }

    private static (string GlobalName, Type Type) PickSampleGlobal(
        IReadOnlyList<(string GlobalName, Type Type)> globals) =>
        globals.FirstOrDefault(g => ScriptMemberInspector.GetMethods(g.Type).Count > 0) is { Type: not null } withMethods
            ? withMethods
            : globals[0];

    private static string BuildSampleScript(IReadOnlyList<(string GlobalName, Type Type)> globals)
    {
        if (globals.Count == 0)
        {
            return """
                // Sample script — no JavascriptObject globals were found.
                // Add [JavascriptObject("name")] to types you want mocked.
                """;
        }

        var primary = PickSampleGlobal(globals);
        var methods = ScriptMemberInspector.GetMethods(primary.Type);
        var properties = ScriptMemberInspector.GetProperties(primary.Type);

        var sb = new StringBuilder();
        sb.AppendLine("// @ts-check");
        sb.AppendLine("// Sample standalone script (no imports/exports) — same style as Jint.");
        sb.AppendLine();

        if (properties.Count > 0)
        {
            var propName = ScriptMemberInspector.FormatName(properties[0].Name, preserveCase: false);
            sb.AppendLine($"var value = {primary.GlobalName}.{propName};");
        }

        if (methods.Count > 0)
        {
            var methodName = ScriptMemberInspector.FormatName(methods[0].Name, preserveCase: false);
            sb.AppendLine($"{primary.GlobalName}.{methodName}();");
        }
        else if (properties.Count == 0)
        {
            sb.AppendLine($"var root = {primary.GlobalName};");
        }

        return sb.ToString();
    }

    private static string BuildSampleTest(
        IReadOnlyList<(string GlobalName, Type Type)> globals,
        string sampleScriptName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("import path from \"node:path\";");
        sb.AppendLine("import { fileURLToPath } from \"node:url\";");
        sb.AppendLine();
        sb.AppendLine("const __dirname = path.dirname(fileURLToPath(import.meta.url));");
        sb.AppendLine($"const sampleScript = path.join(__dirname, \"..\", \"scripts\", \"{sampleScriptName}\");");
        sb.AppendLine();
        sb.AppendLine("describe(\"generated mock environment\", () => {");

        if (globals.Count == 0)
        {
            sb.AppendLine("  test(\"setup installs helpers\", () => {");
            sb.AppendLine("    expect(typeof globalThis.executeScript).toBe(\"function\");");
            sb.AppendLine("    expect(typeof globalThis.resetMocks).toBe(\"function\");");
            sb.AppendLine("  });");
            sb.AppendLine("});");
            sb.AppendLine();
            return sb.ToString();
        }

        var primary = PickSampleGlobal(globals);
        var methods = ScriptMemberInspector.GetMethods(primary.Type);
        var properties = ScriptMemberInspector.GetProperties(primary.Type);
        var factoryName = $"create{primary.Type.Name}";
        var root = $"globalThis.{primary.GlobalName}";

        sb.AppendLine("  test(\"configures mocks, executes a script, and verifies calls\", () => {");

        if (methods.Count > 0)
        {
            var methodName = ScriptMemberInspector.FormatName(methods[0].Name, preserveCase: false);
            sb.AppendLine($"    {root}.{methodName}.mockReturnValue(undefined);");
            sb.AppendLine();
            sb.AppendLine("    globalThis.executeScript(sampleScript);");
            sb.AppendLine();
            sb.AppendLine($"    expect({root}.{methodName}).toHaveBeenCalled();");
        }
        else
        {
            sb.AppendLine("    globalThis.executeScript(sampleScript);");
            sb.AppendLine($"    expect({root}).toBeDefined();");
        }

        sb.AppendLine("  });");
        sb.AppendLine();

        if (methods.Count > 0)
        {
            var methodName = ScriptMemberInspector.FormatName(methods[0].Name, preserveCase: false);
            sb.AppendLine("  test(\"throws when a method is called without mock configuration\", () => {");
            sb.AppendLine($"    expect(() => {root}.{methodName}()).toThrow(/No mock behaviour has been configured/);");
            sb.AppendLine("  });");
            sb.AppendLine();
        }

        if (properties.Count > 0)
        {
            var propName = ScriptMemberInspector.FormatName(properties[0].Name, preserveCase: false);
            sb.AppendLine("  test(\"resets property values between tests\", () => {");
            sb.AppendLine($"    const original = {root}.{propName};");
            sb.AppendLine($"    {root}.{propName} = \"changed-in-test\";");
            sb.AppendLine("    globalThis.resetMocks();");
            sb.AppendLine($"    expect(globalThis.{primary.GlobalName}.{propName}).toEqual(original);");
            sb.AppendLine("  });");
            sb.AppendLine();
        }

        sb.AppendLine("  test(\"builds model objects with factories\", () => {");
        sb.AppendLine($"    const model = globalThis.{factoryName}({{ }});");
        sb.AppendLine("    expect(model).toBeTypeOf(\"object\");");
        sb.AppendLine("  });");
        sb.AppendLine("});");
        sb.AppendLine();
        return sb.ToString();
    }

    private static string BuildReadme(
        IReadOnlyList<(string GlobalName, Type Type)> globals,
        string definitionFileName)
    {
        var sampleGlobal = globals.Count > 0 ? PickSampleGlobal(globals) : default;
        var exampleGlobal = sampleGlobal.GlobalName ?? "myApi";
        var exampleType = sampleGlobal.Type;
        var exampleMethod = "someMethod";
        if (exampleType != null)
        {
            var methods = ScriptMemberInspector.GetMethods(exampleType);
            if (methods.Count > 0)
                exampleMethod = ScriptMemberInspector.FormatName(methods[0].Name, preserveCase: false);
        }

        return $$"""
            # Generated Vitest environment

            This folder was generated by `dotnet-to-typescript` with `--test`.

            ## Quick start

            ```bash
            npm install
            npm test
            ```

            Coverage:

            ```bash
            npm run test:coverage
            ```

            ## Write a test

            Scripts are standalone (no imports). Mocks are installed as globals.

            ```javascript
            {{exampleGlobal}}.{{exampleMethod}}.mockReturnValue(/* ... */);

            executeScript("path/to/your-script.js");

            expect({{exampleGlobal}}.{{exampleMethod}}).toHaveBeenCalled();
            ```

            Add `// @ts-check` at the top of scripts to type-check against `{{definitionFileName}}`.

            ## More documentation

            See the project docs for Vitest setup details, mock configuration, factories, and recommended layout:
            https://github.com/mzbrau/dotnet-to-typescript

            """;
    }
}
