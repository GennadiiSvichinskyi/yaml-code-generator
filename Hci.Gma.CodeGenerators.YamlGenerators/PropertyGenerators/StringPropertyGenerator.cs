using System;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Hci.Gma.CodeGenerators.YamlGenerators.PropertyGenerators;
internal class StringPropertyGenerator : IPropertyGenerator
{
    public string GetType(YamlMappingNode node)
    {
        var type = nameof(String);
        if (node.Any(x => x.Key.ToString() == Constants.NodeNames.Format))
        {
            var typeFormat = (YamlScalarNode)node[Constants.NodeNames.Format];
            if (typeFormat.Value == "uuid")
            {
                type = nameof(Guid);
            }
            else if (typeFormat.Value == "date-time")
            {
                type = nameof(DateTime);
            }
        }

        return type;
    }
}
