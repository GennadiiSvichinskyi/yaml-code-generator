using System;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Hci.Gma.CodeGenerators.YamlGenerators.PropertyGenerators;
public class IntegerPropertyGenerator : IPropertyGenerator
{
    public string GetType(YamlMappingNode node)
    {
        var type = nameof(Int32);
        if (node.Any(x => x.Key.ToString() == Constants.NodeNames.Format))
        {
            var typeFormat = (YamlScalarNode)node[Constants.NodeNames.Format];
            type = typeFormat.Value switch
            {
                "int32" => nameof(Int32),
                "int64" => nameof(Int64),
                _ => type
            };
        }

        return type;
    }
}
