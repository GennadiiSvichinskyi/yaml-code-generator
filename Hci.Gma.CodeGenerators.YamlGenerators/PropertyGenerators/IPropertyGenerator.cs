using YamlDotNet.RepresentationModel;

namespace Hci.Gma.CodeGenerators.YamlGenerators.PropertyGenerators;

internal interface IPropertyGenerator
{
    string GetType(YamlMappingNode node);
}
