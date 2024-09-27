using Hci.Gma.CodeGenerators.YamlGenerators.PropertyGenerators;

namespace Hci.Gma.CodeGenerators.YamlGenerators;

public interface IPropertyGeneratorProvider
{
    IPropertyGenerator GetPropertyGenerator(string type);
}