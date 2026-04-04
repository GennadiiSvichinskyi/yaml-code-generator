using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Hci.Gma.CodeGenerators.YamlGenerators.PropertyGenerators;
internal class ArrayPropertyGenerator : IPropertyGenerator
{
    private readonly IPropertyGeneratorProvider _provider;

    public ArrayPropertyGenerator(IPropertyGeneratorProvider provider)
    {
        _provider = provider;
    }

    public string GetType(YamlMappingNode node)
    {
        if (node.All(x => x.Key.ToString() != Constants.NodeNames.Items))
            return string.Empty;

        var itemsArrayNode = (YamlMappingNode)node[Constants.NodeNames.Items];
        if (itemsArrayNode.Any(x => x.Key.ToString() == Constants.NodeNames.Reference))
        {
            var refNode = (YamlScalarNode)itemsArrayNode[Constants.NodeNames.Reference];
            var typeParam = refNode.Value?.Substring(refNode.Value.LastIndexOf('/') + 1);
            return $"IEnumerable<{typeParam}>";
        }

        if (itemsArrayNode.Any(x => x.Key.ToString() == Constants.NodeNames.Type))
        {
            var typeArrayNode = (YamlScalarNode)itemsArrayNode[Constants.NodeNames.Type];
            var generator = _provider.GetPropertyGenerator(typeArrayNode.Value ?? "object");
            return $"{generator.GetType(itemsArrayNode)}[]";
        }

        return string.Empty;
    }
}
