---
id: type-mapping
title: Type Mapping
sidebar_position: 6
description: How .NET types are translated to TypeScript.
---

## Primitive mappings

- `string` → `string`
- numeric types (`int`, `long`, `double`, etc.) → `number`
- `bool` → `boolean`
- `DateTime` → `Date`

## Collection mappings

- `T[]`, `List<T>` → `Array<T>`
- `Dictionary<TKey, TValue>` → indexed/record-style type

## Async mappings

- `Task` → `Promise<void>`
- `Task<T>` → `Promise<T>`

## Optional and nullable values

Nullable metadata is preserved so generated output reflects optional/nullable shape where possible.

## Framework type support

Referenced `System.*` types are tracked and emitted when needed, including exception hierarchies.
