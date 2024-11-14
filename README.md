# dotnet-to-typescript
Converts dotnet classes to typescript definitions

## Usage

1. Define 2 attributes in your assembly:

```csharp
public class JavascriptTypeAttribute : Attribute
{
}

public class JavascriptObjectAttribute : Attribute
{
    public string Name { get; }

    public JavascriptObjectAttribute(string name)
    {
        Name = name;
    }
}
```
2. Decorate any classes that are part of the javascript interface with the `[JavascriptType]` attribute.
3. Decorate any classes that are added into the javascript engine context with the `[JavascriptObject]` attribute, including the object name.

4. Build your application

5. Run the tool:

```bash
dotnet-to-typescript.exe path/to/your/assembly.dll
```

The tool will create 2 files:

- `assembly.d.ts` - TypeScript definitions
- `assembly.ts` - TypeScript instances

For example, if you have an assembly `MyAssembly.dll`, the tool will create `MyAssembly.d.ts` and `MyAssembly.ts`.

### MyAssembly.d.ts

```typescript
declare class Car {
    Model: string;
    AddUser(user: User): boolean;
}
declare class User {
    Name: string;
    Address: string;
    Call(): void;
}
```

### MyAssembly.ts

```typescript
/// <reference path="SampleLibrary.d.ts" />

let mike = new User();

// Insert your script below
// -------------------------

```

## Use Cases

This is useful when writing scripts that will run within your dotnet application, for example when using Jint or other javascript engines. The scripts can be written in vscode with intellisense and typescript type checking before being copied into your application.
