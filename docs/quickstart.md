---
id: quickstart
title: Quickstart
sidebar_position: 3
description: End-to-end setup from C# attributes to generated TypeScript.
---

## 1) Define marker attributes

Add attributes in one of the assemblies you pass to the generator:

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Struct)]
public class JavascriptTypeAttribute : Attribute
{
    public JavascriptTypeAttribute(bool isSkipped = false)
    {
        SkipDefinition = isSkipped;
    }

    public bool SkipDefinition { get; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class JavascriptObjectAttribute : Attribute
{
    public JavascriptObjectAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
```

## 2) Decorate script-facing types

```csharp
[JavascriptType]
[JavascriptObject("car")]
public class Car
{
    public string Make { get; set; }
    public int Year { get; set; }

    public bool AddUser(User user) => true;
}
```

## 3) Build your assemblies

```bash
dotnet build
```

## 4) Generate TypeScript output

```bash
dotnet-to-typescript generate path/to/YourAssembly.dll
```

## 5) Use the generated files

- Reference `YourAssembly.d.ts` for typing
- Load `YourAssembly.ts` to create script objects
