---
id: cli-reference
title: CLI Reference
sidebar_position: 5
description: Commands and flags for dotnet-to-typescript.
---

## Command format

```bash
dotnet-to-typescript generate <assembly1> [assembly2 ...] [options]
```

## Required arguments

- One or more assembly paths (`.dll`)

## Options

- `-o, --output-directory <path>`: target directory for generated files
- `-n, --output-name <name>`: custom output filename prefix (without extension)
- `-p, --preserve-case`: preserve original C# member casing
- `--js, --javascript`: generate instance stubs as `.js` instead of `.ts` (still emits `.d.ts`)
- `-t, --test`: generate a Vitest testing environment (`package.json`, mocks, helpers, sample tests, and robustness engine)

Omitting `--js` / `-t` keeps the historical output (`.d.ts` + `.ts` only).

## Examples

Generate using default output name:

```bash
dotnet-to-typescript generate ./bin/Debug/net9.0/MyApp.dll
```

Generate from multiple assemblies into one output set:

```bash
dotnet-to-typescript generate ./A.dll ./B.dll -o ./generated -n combined-types
```

Preserve C# casing:

```bash
dotnet-to-typescript generate ./MyApp.dll --preserve-case
```

Generate Vitest harness and JavaScript instance stubs:

```bash
dotnet-to-typescript generate ./MyApp.dll -o ./generated -t --js
```

See [Testing with Vitest](./testing.md) for the generated project layout and usage.
