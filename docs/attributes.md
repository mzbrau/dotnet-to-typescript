---
id: attributes
title: Attributes
sidebar_position: 4
description: Behavior and constraints of JavascriptType and JavascriptObject attributes.
---

## `JavascriptTypeAttribute`

Marks a class, enum, or struct as eligible for TypeScript generation.

### Constructor

- `JavascriptTypeAttribute(bool isSkipped = false)`

### Behavior

- `SkipDefinition = false`: type definition is generated normally
- `SkipDefinition = true`: member properties can be included while omitting direct class definition output

## `JavascriptObjectAttribute`

Requests generation of a named object instance in the generated `.ts` file.

### Constructor

- `JavascriptObjectAttribute(string name)`

### Behavior

- Generates script-level object initialization for the marked type
- Supports multiple attributes on one class

## Naming caveat

If an object name matches the class name, the generator appends a numeric suffix to avoid unsupported collisions.
