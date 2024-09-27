using YamlDotNet.RepresentationModel;

namespace Hci.Gma.CodeGenerators.YamlGenerators.PropertyGenerators;

public interface IPropertyGenerator
{
    string GetType(YamlMappingNode node);
}
