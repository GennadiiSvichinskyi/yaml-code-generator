using System.Collections.Generic;
using Hci.Gma.CodeGenerators.YamlGenerators.PropertyGenerators;

namespace Hci.Gma.CodeGenerators.YamlGenerators;
public class PropertyGeneratorProvider : IPropertyGeneratorProvider
{
    private static Dictionary<string, IPropertyGenerator> PropertyGeneratorDictionary { get; } = new()
    {
        {"string", new StringPropertyGenerator()},
        {"integer", new IntegerPropertyGenerator()},
        {"number", new NumberPropertyGenerator()},
        {"boolean", new BooleanPropertyGenerator()},
        {"array", new ArrayPropertyGenerator()},
        {"object", new ObjectPropertyGenerator()}
    };
    public IPropertyGenerator GetPropertyGenerator(string type)
    {
        return PropertyGeneratorDictionary[type];
    }
}