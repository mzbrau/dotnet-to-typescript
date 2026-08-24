---
id: overview
title: Overview
sidebar_position: 1
description: What dotnet-to-typescript does and when to use it.
---

`dotnet-to-typescript` is a .NET CLI tool that generates TypeScript definitions and instance stubs from C# types.

## Why it exists

When a .NET application executes JavaScript or TypeScript scripts, script authors often lose static typing and editor assistance. This project closes that gap by generating `.d.ts` and `.ts` files from your compiled assemblies.

## Core capabilities

- Generate TypeScript class and enum definitions from decorated .NET types
- Create runtime instance files for script contexts
- Handle async methods (`Task`, `Task<T>`) and map to `Promise`
- Preserve nullability semantics in generated output
- Resolve inheritance and referenced framework types
- Process multiple assemblies in one command

## Typical use cases

- Scripting with engines like Jint while keeping IntelliSense
- Sharing a contract between C# application code and TypeScript scripts
- Reducing runtime script errors from mismatched type signatures

## Output artifacts

Each generation run creates:

- `*.d.ts` for type definitions
- `*.ts` for object instance stubs (or `*.js` when using `--js`)

With `-t` / `--test`, the tool also emits a Vitest harness (`package.json`, mocks, `executeScript`, factories, and sample tests) so standalone Jint-style scripts can be tested with little setup.

These files are designed to be committed into your scripting project or generated during CI.
