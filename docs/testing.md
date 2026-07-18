---
id: testing
title: Testing with Vitest
sidebar_position: 6
description: Generate a Vitest environment for Jint-style standalone scripts.
---

# Testing with Vitest

`dotnet-to-typescript` can generate a complete Vitest harness so you can unit-test the same standalone JavaScript files that run inside Jint.

## Generate the harness

```bash
dotnet-to-typescript generate ./MyApp.dll -o ./generated -t
```

Optional: emit instance stubs as JavaScript instead of TypeScript:

```bash
dotnet-to-typescript generate ./MyApp.dll -o ./generated -t --js
```

Existing generation without `-t` / `--js` is unchanged.

## What gets generated

Under the output directory:

```
package.json
tsconfig.json
vitest.config.js
README.md
*.d.ts
test/
  setup.js
  executeScript.js
  resetMocks.js
  factories.js
  mocks/
  robustness/          # reflection-driven robustness engine
    apiCatalog.js
    runRobustnessTests.js
    mutations/
  samples/
  scripts/
```

## Quick start

```bash
cd generated
npm install
npm test
```

Coverage:

```bash
npm run test:coverage
```

## How Vitest maps to Jint scripts

Your scripts stay standalone:

- no `import` / `export`
- run immediately when loaded
- call into globals injected by the host

Tests should execute those files with `executeScript`, not by importing them as modules.

## Configuring mocks

Every reflected method on a `JavascriptObject` global is a Vitest mock (`vi.fn`) with a **strict** default: calling it without configuration throws.

```javascript
mike.validateAsync.mockReturnValue(Promise.resolve(true));

executeScript("test/scripts/sample-script.js");

expect(mike.validateAsync).toHaveBeenCalled();
```

Properties get sensible defaults (`""`, `0`, `false`, `[]`, `{}`, `new Date(0)`, nested objects) and are reset between tests.

## Resetting mocks

`test/setup.js` registers Vitest `beforeEach` to call `resetMocks()`.

That recreates mock objects, restores property defaults, and restores strict method implementations.

You can also call `resetMocks()` manually inside a test.

## Executing scripts

```javascript
const context = executeScript("path/to/script.js");
```

`executeScript`:

1. Reads the file as text
2. Creates a fresh Node `vm` context
3. Injects the **same** mock object references currently installed for the test
4. Runs the script
5. Returns the context

Because mocks are shared by reference, `mockReturnValue` and `toHaveBeenCalled` work across the test and the script.

## Robustness testing

The harness includes `runRobustnessTests`, which executes a standalone script many times under deterministic mutations derived from reflection (not random fuzzing).

```javascript
import { runRobustnessTests } from "../robustness/runRobustnessTests.js";

runRobustnessTests("scripts/alarm.js");
```

Or inside a suite:

```javascript
describe("Alarm Script", () => {
  runRobustnessTests("scripts/alarm.js");
});
```

Each scenario:

1. Recreates mocks (`createAllMocks`)
2. Applies baseline default return values for every reflected method
3. Applies **one** mutation (null return, thrown exception, empty collection, …)
4. Runs the script in a fresh `vm` context
5. Fails the Vitest test with an actionable message if the script throws

Toggle categories via options (all major categories default to enabled):

```javascript
runRobustnessTests("scripts/alarm.js", {
  nulls: true,
  undefineds: true,
  emptyObjects: true,
  recursiveNulls: true,
  emptyCollections: true,
  nullCollections: true,
  emptyStrings: true,
  whitespaceStrings: true,
  numericBoundaries: false,
  booleans: true,
  exceptions: true,
  missingOptionals: true,
});
```

A generated example lives at `test/samples/robustness.test.js` (uses a defensive `robust-sample.js` so `npm test` stays green). Fragile scripts under test will fail scenarios such as “`api.getUser()` returned null”.

## Factories

For each reflected model type, a factory is generated:

```javascript
const user = createUser({ name: "Michael" });
// { name: "Michael", address: "", dateOfBirth: new Date(0), ... }
```

Factories are for test data, not API wrappers.

## Type-checking scripts

Add this at the top of a script:

```javascript
// @ts-check
```

`tsconfig.json` enables `allowJs` + `checkJs` against the generated `.d.ts`.

When you pass `--js`, the instance stub file is also emitted as `.js` with `// @ts-check` already included.

## Recommended layout

```
generated/          # output of dotnet-to-typescript -t
scripts/            # your standalone Jint scripts
  alarm.js
```

Keep tests next to the generated harness (or point Vitest `include` at your test folder) and call `executeScript` with paths to real scripts.

## Vitest primer

- `describe` / `test` — group and declare tests
- `expect` — assertions
- `vi.fn()` — mock functions (already generated for API methods)
- `.mockReturnValue(value)` — configure a return value
- `.toHaveBeenCalledWith(...)` — verify calls

No custom mock abstraction is added on top of Vitest.
