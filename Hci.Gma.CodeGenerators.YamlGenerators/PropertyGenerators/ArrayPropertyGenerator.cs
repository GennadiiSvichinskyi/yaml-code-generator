using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Hci.Gma.CodeGenerators.YamlGenerators.PropertyGenerators;
public class ArrayPropertyGenerator : IPropertyGenerator
{
    public string GetType(YamlMappingNode node)
    {
        string type = string.Empty;
        // items node is mandatory for array definition
        if (node.All(x => x.Key.ToString() != Constants.NodeNames.Items))
        {
            throw new KeyNotFoundException("items node not found. It is mandatory for array specification");
        }
        var itemsArrayNode = (YamlMappingNode)node[Constants.NodeNames.Items];
        if (itemsArrayNode.Any(x => x.Key.ToString() == Constants.NodeNames.Reference))
        {
            var refNode = (YamlScalarNode)itemsArrayNode[Constants.NodeNames.Reference];
            var typeParam = refNode.Value?.Substring(refNode.Value.LastIndexOf('/') + 1);
            type = $"IEnumerable<{typeParam}>";
        }
        else if (itemsArrayNode.Any(x => x.Key.ToString() == Constants.NodeNames.Type))
        {
            var typeArrayNode = (YamlScalarNode)itemsArrayNode[Constants.NodeNames.Type];
            IPropertyGenerator generator = typeArrayNode.Value switch
            {
                "string" => new StringPropertyGenerator(),
                "integer" => new IntegerPropertyGenerator(),
                "number" => new NumberPropertyGenerator(),
                "boolean" => new BooleanPropertyGenerator(),
                "object" => new ObjectPropertyGenerator(),
                "array" => new ArrayPropertyGenerator(),
                _ => new ObjectPropertyGenerator()
            };
            
            type = generator.GetType(itemsArrayNode);

            type = $"{type}[]";
        }

        return type;
    }
}
