using System.Collections.Generic;
using Hci.Gma.CodeGenerators.YamlGenerators.PropertyGenerators;

namespace Hci.Gma.CodeGenerators.YamlGenerators;
internal class PropertyGeneratorProvider : IPropertyGeneratorProvider
{
    private readonly Dictionary<string, IPropertyGenerator> _generators;

    public PropertyGeneratorProvider()
    {
        _generators = new Dictionary<string, IPropertyGenerator>
        {
            { "string",  new StringPropertyGenerator() },
            { "integer", new IntegerPropertyGenerator() },
            { "number",  new NumberPropertyGenerator() },
            { "boolean", new BooleanPropertyGenerator() },
            { "object",  new ObjectPropertyGenerator(this) },
            { "array",   new ArrayPropertyGenerator(this) }
        };
    }

    public IPropertyGenerator GetPropertyGenerator(string type)
    {
        return _generators.TryGetValue(type, out var generator)
            ? generator
            : _generators["object"];
    }
}