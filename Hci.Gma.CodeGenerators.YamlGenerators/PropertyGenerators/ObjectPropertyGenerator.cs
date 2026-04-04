using System;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Hci.Gma.CodeGenerators.YamlGenerators.PropertyGenerators;
internal class ObjectPropertyGenerator : IPropertyGenerator
{
    private readonly IPropertyGeneratorProvider _provider;

    public ObjectPropertyGenerator(IPropertyGeneratorProvider provider)
    {
        _provider = provider;
    }

    public string GetType(YamlMappingNode node)
    {
        if (node.Any(x => x.Key.ToString() == Constants.NodeNames.AdditionalProperties)
            && node[Constants.NodeNames.AdditionalProperties] is YamlMappingNode additionalPropsNode)
        {
            var valueType = ResolveValueType(additionalPropsNode);
            return $"IDictionary<{nameof(String)}, {valueType}>";
        }

        return nameof(Object);
    }

    private string ResolveValueType(YamlMappingNode additionalPropsNode)
    {
        if (additionalPropsNode.Any(x => x.Key.ToString() == Constants.NodeNames.Reference))
        {
            var refNode = (YamlScalarNode)additionalPropsNode[Constants.NodeNames.Reference];
            return refNode.Value?.Substring(refNode.Value.LastIndexOf('/') + 1) ?? nameof(Object);
        }

        if (additionalPropsNode.Any(x => x.Key.ToString() == Constants.NodeNames.Type))
        {
            var typeNode = (YamlScalarNode)additionalPropsNode[Constants.NodeNames.Type];
            var generator = _provider.GetPropertyGenerator(typeNode.Value ?? "object");
            return generator.GetType(additionalPropsNode);
        }

        return nameof(Object);
    }
}
