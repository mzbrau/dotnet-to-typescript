namespace DotnetToTypescript.Testing.Robustness;

/// <summary>
/// Emits the static robustness testing engine (DLL-agnostic JS).
/// </summary>
public static class RobustnessCodeBuilder
{
    public static IReadOnlyDictionary<string, string> BuildEngineFiles() =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["helpers.js"] = BuildHelpers(),
            ["applyBaseline.js"] = BuildApplyBaseline(),
            ["report.js"] = BuildReport(),
            ["runRobustnessTests.js"] = BuildRunner(),
            ["mutations/index.js"] = BuildMutationsIndex(),
            ["mutations/nullReturn.js"] = BuildNullReturn(),
            ["mutations/undefinedReturn.js"] = BuildUndefinedReturn(),
            ["mutations/emptyObject.js"] = BuildEmptyObject(),
            ["mutations/recursiveNull.js"] = BuildRecursiveNull(),
            ["mutations/emptyCollection.js"] = BuildEmptyCollection(),
            ["mutations/nullCollection.js"] = BuildNullCollection(),
            ["mutations/emptyString.js"] = BuildEmptyString(),
            ["mutations/whitespaceString.js"] = BuildWhitespaceString(),
            ["mutations/numericBoundary.js"] = BuildNumericBoundary(),
            ["mutations/booleanBranch.js"] = BuildBooleanBranch(),
            ["mutations/exception.js"] = BuildException(),
            ["mutations/missingOptional.js"] = BuildMissingOptional(),
        };

    private static string BuildHelpers() =>
        """
        /** Shared helpers for robustness mutations. */

        export function evaluateExpression(expression) {
          return new Function(`return (${expression});`)();
        }

        export function cloneJson(value) {
          return structuredClone(value);
        }

        /**
         * Resolve a mock function at a dotted path like "mike.validateAsync".
         * @param {Record<string, unknown>} mocks
         * @param {string} path
         */
        export function getMockAtPath(mocks, path) {
          const parts = path.split(".");
          let current = mocks;
          for (const part of parts) {
            if (current == null || typeof current !== "object") {
              throw new Error(`Cannot resolve mock path "${path}" (failed at "${part}")`);
            }
            current = current[part];
          }
          return current;
        }

        /**
         * Configure a method mock to return a value (wrapping Promise when async).
         */
        export function setMethodReturn(mocks, method, value) {
          const fn = getMockAtPath(mocks, method.path);
          if (typeof fn?.mockReturnValue !== "function") {
            throw new Error(`Expected a Vitest mock at ${method.path}`);
          }
          const returnValue = method.isAsync ? Promise.resolve(value) : value;
          fn.mockReturnValue(returnValue);
        }

        export function setMethodImplementation(mocks, method, implementation) {
          const fn = getMockAtPath(mocks, method.path);
          if (typeof fn?.mockImplementation !== "function") {
            throw new Error(`Expected a Vitest mock at ${method.path}`);
          }
          fn.mockImplementation(implementation);
        }

        export function setByPath(target, pathParts, value) {
          if (pathParts.length === 0) return value;
          const clone = cloneJson(target);
          let cursor = clone;
          for (let i = 0; i < pathParts.length - 1; i++) {
            const key = pathParts[i];
            if (cursor[key] == null || typeof cursor[key] !== "object") {
              cursor[key] = {};
            } else {
              cursor[key] = cloneJson(cursor[key]);
            }
            cursor = cursor[key];
          }
          cursor[pathParts[pathParts.length - 1]] = value;
          return clone;
        }

        export function omitByPath(target, pathParts) {
          if (pathParts.length === 0) return target;
          const clone = cloneJson(target);
          let cursor = clone;
          for (let i = 0; i < pathParts.length - 1; i++) {
            const key = pathParts[i];
            if (cursor[key] == null || typeof cursor[key] !== "object") {
              return clone;
            }
            cursor[key] = cloneJson(cursor[key]);
            cursor = cursor[key];
          }
          delete cursor[pathParts[pathParts.length - 1]];
          return clone;
        }

        /**
         * Walk a returnGraph and yield property paths (arrays of names), depth-capped by catalog.
         */
        export function* walkGraphPaths(graph, prefix = []) {
          if (!graph?.properties?.length) return;
          for (const prop of graph.properties) {
            const path = [...prefix, prop.name];
            yield { path, prop };
            if (prop.graph?.properties?.length) {
              yield* walkGraphPaths(prop.graph, path);
            }
          }
        }

        export function methodDefaultValue(method) {
          return evaluateExpression(method.defaultReturnExpression);
        }

        export function unwrapAsyncDefault(method) {
          const value = methodDefaultValue(method);
          // Baseline expressions for async methods are already Promise.resolve(...).
          // For mutations we want the inner object graph when available.
          if (method.isAsync && value != null && typeof value.then === "function") {
            // Cannot await here synchronously — re-evaluate inner by stripping Promise.resolve wrapper.
            const expr = method.defaultReturnExpression;
            const match = /^Promise\.resolve\(([\s\S]*)\)$/.exec(expr.trim());
            if (match) {
              const inner = match[1].trim();
              if (inner.length === 0) return undefined;
              return evaluateExpression(inner);
            }
          }
          return value;
        }
        """;

    private static string BuildApplyBaseline() =>
        """
        import { getMockAtPath, evaluateExpression } from "./helpers.js";

        /**
         * Configure every reflected method with its deterministic default return value.
         * @param {Record<string, unknown>} mocks
         * @param {{ globals: Array<{ methods: Array<{ path: string, defaultReturnExpression: string }> }> }} catalog
         */
        export function applyBaseline(mocks, catalog) {
          for (const global of catalog.globals) {
            for (const method of global.methods) {
              const fn = getMockAtPath(mocks, method.path);
              if (typeof fn?.mockReturnValue !== "function") continue;
              fn.mockReturnValue(evaluateExpression(method.defaultReturnExpression));
            }
          }
          return mocks;
        }
        """;

    private static string BuildReport() =>
        """
        /**
         * @param {object} scenario
         * @param {unknown} error
         */
        export function formatFailure(scenario, error) {
          const message = error instanceof Error ? error.message : String(error);
          const stack = error instanceof Error && error.stack ? `\n${error.stack}` : "";
          const hint = scenario.hint
            ? `${scenario.hint}\n\n`
            : "";

          return (
            `${hint}` +
            `Scenario: ${scenario.title}\n` +
            `Error: ${message}` +
            stack
          );
        }

        /**
         * @param {string} scriptPath
         * @param {Array<{ title: string, passed: boolean, error?: unknown }>} results
         */
        export function formatReport(scriptPath, results) {
          const lines = [
            "Robustness Report",
            "",
            "Script:",
            scriptPath,
            "",
            "Executed Scenarios:",
            "",
          ];

          for (const result of results) {
            if (result.passed) {
              lines.push(`✔ ${result.title}`);
            } else {
              lines.push(`✘ ${result.title}`);
              if (result.error) {
                const message = result.error instanceof Error ? result.error.message : String(result.error);
                lines.push("");
                lines.push("Reason:");
                lines.push("");
                lines.push(message);
              }
            }
            lines.push("");
          }

          const passed = results.filter((r) => r.passed).length;
          const failed = results.length - passed;
          lines.push("Summary");
          lines.push("");
          lines.push(`${passed} passed`);
          lines.push("");
          lines.push(`${failed} failed`);
          return lines.join("\n");
        }
        """;

    private static string BuildRunner() =>
        """
        import path from "node:path";
        import { describe, it, afterAll } from "vitest";
        import { createAllMocks } from "../mocks/index.js";
        import { executeScript as runInVm } from "../executeScript.js";
        import { apiCatalog } from "./apiCatalog.js";
        import { applyBaseline } from "./applyBaseline.js";
        import { mutationPlugins } from "./mutations/index.js";
        import { formatFailure, formatReport } from "./report.js";

        const defaultOptions = {
          nulls: true,
          undefineds: true,
          emptyObjects: true,
          recursiveNulls: true,
          emptyCollections: true,
          nullCollections: true,
          emptyStrings: true,
          whitespaceStrings: true,
          numericBoundaries: true,
          booleans: true,
          exceptions: true,
          missingOptionals: true,
        };

        /**
         * Expand and run deterministic robustness scenarios for a standalone script.
         *
         * @param {string} scriptPath
         * @param {Partial<typeof defaultOptions>} [options]
         */
        export function runRobustnessTests(scriptPath, options = {}) {
          const resolvedOptions = { ...defaultOptions, ...options };
          const absoluteScriptPath = path.resolve(scriptPath);

          /** @type {Array<{ id: string, title: string, hint?: string, apply: (mocks: Record<string, unknown>) => void }>} */
          const scenarios = [
            {
              id: "default",
              title: "Default execution",
              hint: "Baseline defaults were applied; the script threw without any mutation.",
              apply() {},
            },
          ];

          for (const plugin of mutationPlugins) {
            const enabled = resolvedOptions[plugin.id];
            if (enabled === false) continue;
            if (enabled !== true && plugin.enabledByDefault === false) continue;
            scenarios.push(...plugin.generateScenarios(apiCatalog));
          }

          /** @type {Array<{ title: string, passed: boolean, error?: unknown }>} */
          const results = [];

          describe(`Robustness: ${path.basename(absoluteScriptPath)}`, () => {
            for (const scenario of scenarios) {
              it(scenario.title, () => {
                const mocks = createAllMocks();
                applyBaseline(mocks, apiCatalog);
                scenario.apply(mocks);

                try {
                  runInVm(absoluteScriptPath, mocks);
                  results.push({ title: scenario.title, passed: true });
                } catch (error) {
                  results.push({ title: scenario.title, passed: false, error });
                  throw new Error(formatFailure(scenario, error));
                }
              });
            }

            afterAll(() => {
              if (results.length > 0) {
                console.log(`\n${formatReport(absoluteScriptPath, results)}\n`);
              }
            });
          });
        }
        """;

    private static string BuildMutationsIndex() =>
        """
        import { nullReturnMutation } from "./nullReturn.js";
        import { undefinedReturnMutation } from "./undefinedReturn.js";
        import { emptyObjectMutation } from "./emptyObject.js";
        import { recursiveNullMutation } from "./recursiveNull.js";
        import { emptyCollectionMutation } from "./emptyCollection.js";
        import { nullCollectionMutation } from "./nullCollection.js";
        import { emptyStringMutation } from "./emptyString.js";
        import { whitespaceStringMutation } from "./whitespaceString.js";
        import { numericBoundaryMutation } from "./numericBoundary.js";
        import { booleanBranchMutation } from "./booleanBranch.js";
        import { exceptionMutation } from "./exception.js";
        import { missingOptionalMutation } from "./missingOptional.js";

        /** @type {Array<{ id: string, enabledByDefault: boolean, generateScenarios: (catalog: any) => any[] }>} */
        export const mutationPlugins = [
          nullReturnMutation,
          undefinedReturnMutation,
          emptyObjectMutation,
          recursiveNullMutation,
          emptyCollectionMutation,
          nullCollectionMutation,
          emptyStringMutation,
          whitespaceStringMutation,
          numericBoundaryMutation,
          booleanBranchMutation,
          exceptionMutation,
          missingOptionalMutation,
        ];
        """;

    private static string BuildNullReturn() =>
        """
        import { setMethodReturn } from "../helpers.js";

        export const nullReturnMutation = {
          id: "nulls",
          enabledByDefault: true,
          generateScenarios(catalog) {
            const scenarios = [];
            for (const global of catalog.globals) {
              for (const method of global.methods) {
                if (!method.isReferenceReturn || method.isVoid) continue;
                scenarios.push({
                  id: `null:${method.path}`,
                  title: `${method.path}() returned null`,
                  hint:
                    `Script assumes\n\n${method.path}()\n\nnever returns null.\n` +
                    `Consider adding a null check before using the result.`,
                  apply(mocks) {
                    setMethodReturn(mocks, method, null);
                  },
                });
              }
            }
            return scenarios;
          },
        };
        """;

    private static string BuildUndefinedReturn() =>
        """
        import { setMethodReturn } from "../helpers.js";

        export const undefinedReturnMutation = {
          id: "undefineds",
          enabledByDefault: true,
          generateScenarios(catalog) {
            const scenarios = [];
            for (const global of catalog.globals) {
              for (const method of global.methods) {
                if (!method.isReferenceReturn || method.isVoid) continue;
                scenarios.push({
                  id: `undefined:${method.path}`,
                  title: `${method.path}() returned undefined`,
                  hint:
                    `Script assumes\n\n${method.path}()\n\nnever returns undefined.\n` +
                    `Consider adding a check before using the result.`,
                  apply(mocks) {
                    setMethodReturn(mocks, method, undefined);
                  },
                });
              }
            }
            return scenarios;
          },
        };
        """;

    private static string BuildEmptyObject() =>
        """
        import { setMethodReturn } from "../helpers.js";

        export const emptyObjectMutation = {
          id: "emptyObjects",
          enabledByDefault: true,
          generateScenarios(catalog) {
            const scenarios = [];
            for (const global of catalog.globals) {
              for (const method of global.methods) {
                if (!method.isObject) continue;
                scenarios.push({
                  id: `emptyObject:${method.path}`,
                  title: `${method.path}() returned {}`,
                  hint:
                    `Script assumes\n\n${method.path}()\n\nreturns a fully populated object.\n` +
                    `Consider guarding missing properties.`,
                  apply(mocks) {
                    setMethodReturn(mocks, method, {});
                  },
                });
              }
            }
            return scenarios;
          },
        };
        """;

    private static string BuildRecursiveNull() =>
        """
        import { setMethodReturn, unwrapAsyncDefault, setByPath, walkGraphPaths } from "../helpers.js";

        export const recursiveNullMutation = {
          id: "recursiveNulls",
          enabledByDefault: true,
          generateScenarios(catalog) {
            const scenarios = [];
            for (const global of catalog.globals) {
              for (const method of global.methods) {
                if (!method.isObject || !method.returnGraph) continue;
                for (const { path, prop } of walkGraphPaths(method.returnGraph)) {
                  if (!prop.isReference && prop.kind !== "string" && prop.kind !== "object" &&
                      prop.kind !== "collection" && prop.kind !== "dictionary") {
                    continue;
                  }
                  const dotted = path.join(".");
                  scenarios.push({
                    id: `recursiveNull:${method.path}:${dotted}`,
                    title: `${method.path}() → ${dotted} was null`,
                    hint:
                      `Script assumes\n\n${method.path}().${dotted}\n\nnever returns null.\n` +
                      `Consider adding a null check before accessing nested members.`,
                    apply(mocks) {
                      const defaults = unwrapAsyncDefault(method);
                      if (defaults == null || typeof defaults !== "object") {
                        setMethodReturn(mocks, method, null);
                        return;
                      }
                      const mutated = setByPath(defaults, path, null);
                      setMethodReturn(mocks, method, mutated);
                    },
                  });
                }
              }
            }
            return scenarios;
          },
        };
        """;

    private static string BuildEmptyCollection() =>
        """
        import { setMethodReturn } from "../helpers.js";

        export const emptyCollectionMutation = {
          id: "emptyCollections",
          enabledByDefault: true,
          generateScenarios(catalog) {
            const scenarios = [];
            for (const global of catalog.globals) {
              for (const method of global.methods) {
                if (!method.isCollection && !method.isDictionary) continue;
                const empty = method.isDictionary ? {} : [];
                const label = method.isDictionary ? "{}" : "[]";
                scenarios.push({
                  id: `emptyCollection:${method.path}`,
                  title: `${method.path}() returned ${label}`,
                  hint:
                    `Script assumes\n\n${method.path}()\n\nreturns a non-empty collection.\n` +
                    `Consider handling the empty case.`,
                  apply(mocks) {
                    setMethodReturn(mocks, method, empty);
                  },
                });
              }
            }
            return scenarios;
          },
        };
        """;

    private static string BuildNullCollection() =>
        """
        import { setMethodReturn } from "../helpers.js";

        export const nullCollectionMutation = {
          id: "nullCollections",
          enabledByDefault: true,
          generateScenarios(catalog) {
            const scenarios = [];
            for (const global of catalog.globals) {
              for (const method of global.methods) {
                if (!method.isCollection && !method.isDictionary) continue;
                scenarios.push({
                  id: `nullCollection:${method.path}`,
                  title: `${method.path}() returned null (collection)`,
                  hint:
                    `Script assumes\n\n${method.path}()\n\nnever returns a null collection.\n` +
                    `Consider adding a null check before iterating.`,
                  apply(mocks) {
                    setMethodReturn(mocks, method, null);
                  },
                });
              }
            }
            return scenarios;
          },
        };
        """;

    private static string BuildEmptyString() =>
        """
        import { setMethodReturn, unwrapAsyncDefault, setByPath, walkGraphPaths } from "../helpers.js";

        export const emptyStringMutation = {
          id: "emptyStrings",
          enabledByDefault: true,
          generateScenarios(catalog) {
            const scenarios = [];
            for (const global of catalog.globals) {
              for (const method of global.methods) {
                if (method.isString) {
                  scenarios.push({
                    id: `emptyString:${method.path}`,
                    title: `${method.path}() returned ""`,
                    hint: `Script assumes\n\n${method.path}()\n\nnever returns an empty string.`,
                    apply(mocks) {
                      setMethodReturn(mocks, method, "");
                    },
                  });
                }

                if (!method.isObject || !method.returnGraph) continue;
                for (const { path, prop } of walkGraphPaths(method.returnGraph)) {
                  if (!prop.isString) continue;
                  const dotted = path.join(".");
                  scenarios.push({
                    id: `emptyString:${method.path}:${dotted}`,
                    title: `${method.path}() → ${dotted} was ""`,
                    hint: `Script assumes\n\n${method.path}().${dotted}\n\nnever returns an empty string.`,
                    apply(mocks) {
                      const defaults = unwrapAsyncDefault(method);
                      if (defaults == null || typeof defaults !== "object") return;
                      setMethodReturn(mocks, method, setByPath(defaults, path, ""));
                    },
                  });
                }
              }
            }
            return scenarios;
          },
        };
        """;

    private static string BuildWhitespaceString() =>
        """
        import { setMethodReturn, unwrapAsyncDefault, setByPath, walkGraphPaths } from "../helpers.js";

        export const whitespaceStringMutation = {
          id: "whitespaceStrings",
          enabledByDefault: true,
          generateScenarios(catalog) {
            const scenarios = [];
            for (const global of catalog.globals) {
              for (const method of global.methods) {
                if (method.isString) {
                  scenarios.push({
                    id: `whitespaceString:${method.path}`,
                    title: `${method.path}() returned " "`,
                    hint: `Script assumes\n\n${method.path}()\n\nnever returns whitespace-only strings.`,
                    apply(mocks) {
                      setMethodReturn(mocks, method, " ");
                    },
                  });
                }

                if (!method.isObject || !method.returnGraph) continue;
                for (const { path, prop } of walkGraphPaths(method.returnGraph)) {
                  if (!prop.isString) continue;
                  const dotted = path.join(".");
                  scenarios.push({
                    id: `whitespaceString:${method.path}:${dotted}`,
                    title: `${method.path}() → ${dotted} was " "`,
                    hint: `Script assumes\n\n${method.path}().${dotted}\n\nnever returns whitespace-only strings.`,
                    apply(mocks) {
                      const defaults = unwrapAsyncDefault(method);
                      if (defaults == null || typeof defaults !== "object") return;
                      setMethodReturn(mocks, method, setByPath(defaults, path, " "));
                    },
                  });
                }
              }
            }
            return scenarios;
          },
        };
        """;

    private static string BuildNumericBoundary() =>
        """
        import { setMethodReturn } from "../helpers.js";

        const BOUNDARIES = [
          { label: "0", value: 0 },
          { label: "1", value: 1 },
          { label: "-1", value: -1 },
          { label: "Number.MAX_SAFE_INTEGER", value: Number.MAX_SAFE_INTEGER },
          { label: "Number.MIN_SAFE_INTEGER", value: Number.MIN_SAFE_INTEGER },
          { label: "Infinity", value: Infinity },
          { label: "-Infinity", value: -Infinity },
          { label: "NaN", value: NaN },
        ];

        export const numericBoundaryMutation = {
          id: "numericBoundaries",
          enabledByDefault: true,
          generateScenarios(catalog) {
            const scenarios = [];
            for (const global of catalog.globals) {
              for (const method of global.methods) {
                if (!method.isNumeric) continue;
                for (const boundary of BOUNDARIES) {
                  scenarios.push({
                    id: `numeric:${method.path}:${boundary.label}`,
                    title: `${method.path}() returned ${boundary.label}`,
                    hint: `Script may not handle numeric boundary ${boundary.label} from ${method.path}().`,
                    apply(mocks) {
                      setMethodReturn(mocks, method, boundary.value);
                    },
                  });
                }
              }
            }
            return scenarios;
          },
        };
        """;

    private static string BuildBooleanBranch() =>
        """
        import { setMethodReturn } from "../helpers.js";

        export const booleanBranchMutation = {
          id: "booleans",
          enabledByDefault: true,
          generateScenarios(catalog) {
            const scenarios = [];
            for (const global of catalog.globals) {
              for (const method of global.methods) {
                if (!method.isBoolean) continue;
                for (const value of [true, false]) {
                  scenarios.push({
                    id: `boolean:${method.path}:${value}`,
                    title: `${method.path}() returned ${value}`,
                    hint: `Script may not handle ${method.path}() === ${value}.`,
                    apply(mocks) {
                      setMethodReturn(mocks, method, value);
                    },
                  });
                }
              }
            }
            return scenarios;
          },
        };
        """;

    private static string BuildException() =>
        """
        import { setMethodImplementation } from "../helpers.js";

        export const exceptionMutation = {
          id: "exceptions",
          enabledByDefault: true,
          generateScenarios(catalog) {
            const scenarios = [];
            for (const global of catalog.globals) {
              for (const method of global.methods) {
                scenarios.push({
                  id: `exception:${method.path}`,
                  title: `${method.path}() throws`,
                  hint:
                    `Script does not handle exceptions from\n\n${method.path}()\n\n` +
                    `Consider try/catch, logging, or retry logic.`,
                  apply(mocks) {
                    setMethodImplementation(mocks, method, () => {
                      throw new Error("Injected robustness exception");
                    });
                  },
                });
              }
            }
            return scenarios;
          },
        };
        """;

    private static string BuildMissingOptional() =>
        """
        import { setMethodReturn, unwrapAsyncDefault, omitByPath, walkGraphPaths } from "../helpers.js";

        export const missingOptionalMutation = {
          id: "missingOptionals",
          enabledByDefault: true,
          generateScenarios(catalog) {
            const scenarios = [];
            for (const global of catalog.globals) {
              for (const method of global.methods) {
                if (!method.isObject || !method.returnGraph) continue;
                for (const { path, prop } of walkGraphPaths(method.returnGraph)) {
                  if (!prop.isOptional) continue;
                  const dotted = path.join(".");
                  scenarios.push({
                    id: `missingOptional:${method.path}:${dotted}`,
                    title: `${method.path}() → missing optional ${dotted}`,
                    hint:
                      `Script assumes optional property\n\n${method.path}().${dotted}\n\n` +
                      `is always present. Consider checking before access.`,
                    apply(mocks) {
                      const defaults = unwrapAsyncDefault(method);
                      if (defaults == null || typeof defaults !== "object") return;
                      setMethodReturn(mocks, method, omitByPath(defaults, path));
                    },
                  });
                }
              }
            }
            return scenarios;
          },
        };
        """;
}
