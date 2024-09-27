using System;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Hci.Gma.CodeGenerators.YamlGenerators.PropertyGenerators;
public class NumberPropertyGenerator : IPropertyGenerator
{
    public string GetType(YamlMappingNode node)
    {
        var type = nameof(Single);
        if (node.Any(x => x.Key.ToString() == Constants.NodeNames.Format))
        {
            var typeFormat = (YamlScalarNode)node[Constants.NodeNames.Format];
            type = typeFormat.Value switch
            {
                "float" => nameof(Single),
                "double" => nameof(Double),
                _ => type
            };
        }

        return type;
    }
}
