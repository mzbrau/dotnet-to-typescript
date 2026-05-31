---
id: troubleshooting
title: Troubleshooting
sidebar_position: 8
description: Common issues and how to fix them.
---

## No output files generated

- Verify the assembly path points to an existing `.dll`
- Ensure target types are decorated with required attributes
- Confirm attributes are defined in one of the supplied assemblies

## Missing expected type definitions

- Check whether `JavascriptTypeAttribute` has `SkipDefinition = true`
- Ensure the type is public and reachable from loaded assembly metadata

## Name collisions in generated instances

If object name and class name are identical, the tool appends a suffix to avoid invalid output.

## Unexpected casing

Use `--preserve-case` to keep original C# member naming.
