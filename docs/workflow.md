---
id: workflow
title: Recommended Workflow
sidebar_position: 7
description: Practical workflow for keeping generated TypeScript current.
---

## Local developer workflow

1. Build the .NET solution
2. Run `dotnet-to-typescript generate` against target assemblies
3. Validate generated files in your TypeScript editor
4. Commit generator inputs and outputs together

## CI workflow recommendation

- Build application assemblies
- Generate TypeScript files in a deterministic output directory
- Fail CI when checked-in artifacts drift from generated output

## Multi-assembly strategy

When scripts rely on types from multiple projects, pass all relevant assemblies in one command so inheritance and cross-type references resolve consistently.
