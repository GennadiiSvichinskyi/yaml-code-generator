using YamlDotNet.RepresentationModel;

namespace Hci.Gma.CodeGenerators.YamlGenerators.PropertyGenerators;
internal class EnumPropertyGenerator : IPropertyGenerator
{
    public string GetType(YamlMappingNode node)
    {
        return node.Anchor.ToString();
    }
}
