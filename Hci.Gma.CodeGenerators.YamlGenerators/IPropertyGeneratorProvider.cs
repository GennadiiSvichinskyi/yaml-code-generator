using Hci.Gma.CodeGenerators.YamlGenerators.PropertyGenerators;

namespace Hci.Gma.CodeGenerators.YamlGenerators;

internal interface IPropertyGeneratorProvider
{
    IPropertyGenerator GetPropertyGenerator(string type);
}