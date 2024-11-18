# dotnet-to-typescript

A .NET tool that converts C# classes to TypeScript definitions, enabling type-safe JavaScript scripting in .NET applications.

[![Build and Test](https://github.com/mzbrau/dotnet-to-typescript/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/mzbrau/dotnet-to-typescript/actions/workflows/build-and-test.yml)
[![NuGet](https://img.shields.io/nuget/v/dotnet-to-typescript.svg)](https://www.nuget.org/packages/dotnet-to-typescript/)

## Installation

Install as a global .NET tool:

```bash
dotnet tool install dotnet-to-typescript --global
```

## Features

- Converts C# classes to TypeScript definitions
- Supports enums, interfaces, and complex types
- Generates TypeScript instance files for scripting
- Handles async methods (Task/Task<T>)
- Supports collections (List<T>, Dictionary<K,V>)
- Preserves nullable types

## Usage

### 1. Add Required Attributes

Define two attributes in your assembly:

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Struct)]
public class JavascriptTypeAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class JavascriptObjectAttribute : Attribute
{
    public string Name { get; }

    public JavascriptObjectAttribute(string name)
    {
        Name = name;
    }
}
```

### 2. Decorate Your Classes

Mark classes that should be available to JavaScript:

```csharp
[JavascriptType] // Adds a definition for the class
[JavascriptObject("car")] // Adds an instance of the class to the javascript context
public class Car : IVehicle
{
    public string Make { get; set; }
    public int Year { get; set; }
 
    public bool AddUser(User user)
    {
        return true;
    }
}
```

### 3. Build Your Assembly

### 4. Generate TypeScript Files

Run the tool against your assembly:

```bash
dotnet-to-typescript generate path/to/your/assembly.dll
```
Note that you can also specify multiple assemblies to generate definitions for:

```bash
dotnet-to-typescript generate path/to/your/assembly1.dll path/to/your/assembly2.dll
```

The attributes need to be defined in one of the assemblies.

Optional parameters:
- `-o, --output-directory`: Specify output directory for generated files

### 5. Review the sample output

2 files will be created in the same directory as the assembly (or specified output directory):

- `assembly.d.ts` - TypeScript definitions, same name as the assembly
- `assembly.ts` - TypeScript instances, same name as the assembly

```typescript
// assembly.d.ts
declare class Car {
    Make: string;
    Year: number;
    AddUser(user: User): boolean;
}

// assembly.ts
/// <reference path="assembly.d.ts" />
let car = new Car();

// Insert your script below
// -------------------------
```

## Use Cases

This tool is particularly useful when:
- Using JavaScript engines like Jint in your .NET application
- Need type-safe JavaScript/TypeScript scripting
- Want to maintain type consistency between C# and JavaScript
- Developing scripts with full IntelliSense support in VS Code

## Supported Types

- Primitive types (string, number, boolean)
- DateTime → Date
- Arrays and Lists → Array
- Dictionaries → Record/indexed type
- Enums
- Complex types
- Nullable types
- Task<T> → Promise<T>
- Interfaces (mapped to concrete implementations)

## Performance

Using the extension method to get object names is very efficient.

| Method                     | Mean     | Error     | StdDev    |
|--------------------------- |---------:|----------:|----------:|
| GetAttributeValueExtension | 1.035 us | 0.0681 us | 0.1966 us |

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Roadmap

- Add support for individual properties (e.g. int being available in the javascript file)
- It only extracts the attributes correctly when the first dll contains the attributes, otherwise the first file is skipped.
- Does not work for `Func<T>`
- Add option to make the first letter lowercase
- Add optional name parameter
- Doesn't work with nested classes that have attributes
- Should add shells for built in types (e.g. Exception, )

