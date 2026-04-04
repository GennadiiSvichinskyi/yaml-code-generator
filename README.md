# YAML Code Generator

A **Roslyn Incremental Source Generator** that reads OpenAPI 3.0 YAML files at compile time and emits strongly-typed C# DTOs, enums, and message classes — no manual mapping, no runtime reflection.

## Overview

The generator processes the `components/schemas` and `components/messages` sections of OpenAPI YAML specifications and produces ready-to-use C# classes annotated with `[JsonProperty]` attributes (Newtonsoft.Json).

### Generated Output Examples

Given a YAML schema:

```yaml
components:
  schemas:
    LocalizedText:
      description: Dictionary where the key is language code and the value is localized text.
      type: object
      additionalProperties:
        type: string
    Price:
      type: object
      properties:
        amount:
          type: number
          format: double
        type:
          $ref: '#/components/schemas/PriceType'
      additionalProperties: true
```

The generator produces:

```csharp
/// <summary>
/// Dictionary where the key is language code and the value is localized text.
/// </summary>
public class LocalizedText : Dictionary<String, String>
{
}

public class Price
{
    [JsonProperty("amount")]
    public Double Amount { get; set; }

    [JsonProperty("type")]
    public PriceType Type { get; set; }

    private IDictionary<String, Object> _additionalProperties = new Dictionary<String, Object>();

    public IDictionary<String, Object> AdditionalProperties
    {
        get { return _additionalProperties; }
        set { _additionalProperties = value; }
    }
}
```

## Solution Structure

```
yaml-code-generator/
├── Hci.Gma.CodeGenerators.YamlGenerators/   # Source generator (netstandard2.0)
│   ├── YamlDotNetGenerator.cs                # Main IIncrementalGenerator
│   ├── Constants.cs                          # YAML node name constants
│   ├── IPropertyGeneratorProvider.cs         # Provider interface
│   ├── PropertyGeneratorProvider.cs          # Type-to-generator registry
│   ├── Extensions/
│   │   └── StringExtensions.cs              # Nullable/CamelCase helpers
│   └── PropertyGenerators/
│       ├── IPropertyGenerator.cs            # Generator interface
│       ├── StringPropertyGenerator.cs       # string, uuid, date-time, date
│       ├── IntegerPropertyGenerator.cs      # integer (int32/int64)
│       ├── NumberPropertyGenerator.cs       # number (float/double)
│       ├── BooleanPropertyGenerator.cs      # boolean
│       ├── ObjectPropertyGenerator.cs       # object, additionalProperties dict
│       ├── ArrayPropertyGenerator.cs        # array with $ref or typed items
│       └── EnumPropertyGenerator.cs         # enum (stub)
└── Hci.Gma.CodeGenerators.Cli/              # Demo consumer project (net8.0)
    ├── Program.cs
    ├── common.yaml                           # Shared types (Image, Error, Price, …)
    ├── content.shared.yaml                   # Content schemas + messages
    ├── deals.shared.yaml                     # Coupon/Product schemas
    ├── lookups.yaml                          # Lookup API schemas
    ├── mapp-backend.shared.yaml              # Merchant branch schemas
    └── products.shared.yaml                  # Product message schemas
```

## Supported OpenAPI Features

| OAS Feature | Generated C# |
|---|---|
| `type: string` | `String` |
| `type: string` + `format: uuid` | `Guid` |
| `type: string` + `format: date-time` | `DateTimeOffset` |
| `type: string` + `format: date` | `DateTime` |
| `type: integer` (`int32`/`int64`) | `Int32` / `Int64` |
| `type: number` (`float`/`double`) | `Single` / `Double` |
| `type: boolean` | `Boolean` |
| `type: object` | `Object` |
| `type: object` + `additionalProperties: { type: T }` | `IDictionary<String, T>` |
| `type: array` + `items` | `T[]` or `IEnumerable<T>` |
| `enum` | C# `enum` with PascalCase values |
| `$ref` | Resolved type name from reference path |
| `allOf` (schema level) | Class inheritance |
| `allOf` (property level) | Typed property |
| `nullable: true` | Nullable type (`T?`) |
| `default` | Property initializer (e.g. `= true;`) |
| `description` | XML documentation comment (`/// <summary>`) |
| `additionalProperties: true` | `IDictionary<String, Object>` backing property |
| Schema-level `additionalProperties: { type: T }` | `: Dictionary<String, T>` inheritance |
| `components/messages` | `partial class` generation |

## How It Works

1. **YAML files** are registered as `<AdditionalFiles>` in the consumer `.csproj`.
2. The **Roslyn incremental generator** picks up files ending in `.yaml`.
3. Each YAML file is parsed with **YamlDotNet** into a node tree.
4. The `components/schemas` section produces DTO classes (`Dtos_{filename}.g.cs`).
5. The `components/messages` section produces partial message classes (`Messages_{filename}.g.cs`).
6. **Property generators** (Strategy Pattern) map OAS types to C# types via `IPropertyGenerator` / `IPropertyGeneratorProvider`.

## Usage

### 1. Reference the Generator

Add a project reference to the generator as an analyzer:

```xml
<ItemGroup>
  <ProjectReference Include="..\Hci.Gma.CodeGenerators.YamlGenerators\Hci.Gma.CodeGenerators.YamlGenerators.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="true">
    <PrivateAssets>All</PrivateAssets>
  </ProjectReference>
</ItemGroup>
```

Or, if distributed as a NuGet package, add the package reference instead.

### 2. Register YAML Files

Add your OpenAPI YAML files as additional files:

```xml
<ItemGroup>
  <AdditionalFiles Include="common.yaml" />
  <AdditionalFiles Include="deals.shared.yaml" />
</ItemGroup>
```

### 3. Build

Generated classes appear under `obj/Debug/{tfm}/generated/` and are automatically available in your project under the `{RootNamespace}.Dtos` namespace.

To inspect the generated files, enable `EmitCompilerGeneratedFiles` in your `.csproj`:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

### 4. Use Generated Types

```csharp
using Hci.Gma.CodeGenerators.Cli.Dtos;

var price = new Price
{
    Amount = 19.99,
    Type = PriceType.TotalCost
};
```

## Dependencies

| Package | Version | Purpose |
|---|---|---|
| [YamlDotNet](https://www.nuget.org/packages/YamlDotNet) | 13.7.1 | YAML parsing |
| [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json) | 13.0.4 | `[JsonProperty]` attributes in generated code |
| [Microsoft.CodeAnalysis.CSharp](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp) | 4.13.0 | Roslyn source generator API |
| [Microsoft.CodeAnalysis.Analyzers](https://www.nuget.org/packages/Microsoft.CodeAnalysis.Analyzers) | 5.3.0 | Analyzer best-practice rules |

## Architecture

The generator uses the **Strategy Pattern** for type mapping:

```
IPropertyGeneratorProvider          IPropertyGenerator
  └─ PropertyGeneratorProvider        ├─ StringPropertyGenerator
       ├─ "string"  ──────────────►   ├─ IntegerPropertyGenerator
       ├─ "integer" ──────────────►   ├─ NumberPropertyGenerator
       ├─ "number"  ──────────────►   ├─ BooleanPropertyGenerator
       ├─ "boolean" ──────────────►   ├─ ObjectPropertyGenerator
       ├─ "object"  ──────────────►   └─ ArrayPropertyGenerator
       └─ "array"   ──────────────►
```

Each generator implements `string GetType(YamlMappingNode node)` and resolves the OAS type + format combination to a C# type string.

## Building

```bash
dotnet build
```

The generator targets **.NET Standard 2.0** (required for Roslyn analyzers). The demo CLI project targets **.NET 8**.

## License

See repository for license details.
